using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.BulbActions;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Html;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

[ContextAction(Name = "Convert to Relay (CommunityToolkit SourceGenerator)",
    Description = "Description convert a command to a method decorated with RelayAttribute",
    GroupType = typeof(CSharpContextActions))]
public class ConvertToRelayProperty : ModernScopedContextActionBase<IPropertyDeclaration>
{
    private readonly ICSharpContextActionDataProvider _provider;

    public ConvertToRelayProperty(
        ICSharpContextActionDataProvider provider)
    {
        _provider = provider;
    }

    protected override IBulbActionCommand? ExecutePsiTransaction(IPropertyDeclaration property, ISolution solution,
        IProgressIndicator progress)
    {

        if (property.GetContainingTypeDeclaration() is IClassLikeDeclaration parent)
        {
            IPropertyBodyHelper? propertyBodyService =
                LanguageManager.Instance.TryGetService<IPropertyBodyHelper>(property.DeclaredElement!
                    .PresentationLanguage);

            if (propertyBodyService == null)
                return null;

            ConvertToRelayContext context = new();

            // Check to see if the property has a backing field
            if (propertyBodyService.GetBackingField(property.DeclaredElement!) is { } field)
            {
                if (field.GetSingleDeclaration()?.Parent is { } fieldDeclaration)
                {
                    // Mark the field to be deleted
                    context.ItemsToRemove.Add(fieldDeclaration);
                }


                // Loop through all the usages of the property and field to find
                // where the given item is written to
                foreach (var referenceOccurrence in _provider
                             .FindUsagesInFile(property, [property.DeclaredElement, field])
                             .Where(x => x.AccessType == ReferenceAccessType.WRITE))
                {

                    if (referenceOccurrence.TryGetReferencedNode<ITreeNode>()?.Parent is IAssignmentExpression
                        assignment)
                    {
                        if (assignment.Source is IObjectCreationExpression creation)
                        {
                            if (HandleObjectCreation(creation, context))
                            {
                                context.ItemsToRemove.Add(assignment);
                                break;
                            }
                        }
                    }
                }
            }

            // catch SomeCommand => 
            else if (property.ArrowClause is { } arrowClause)
            {
                if (arrowClause.Expression is IAssignmentExpression assignment)
                {
                    // This will catch the field for scenarios like => _someField ??= new AsyncRelay(...)
                    // and return the the field
                    if (assignment.Dest.TryGetReferencedNode<IFieldDeclaration>() is
                        {Parent: IMultipleFieldDeclaration f})
                    {
                        // Mark the field for deletion
                        context.ItemsToRemove.Add(f);
                    }

                    // Check to get data from the object creation
                    if (assignment.Source is IObjectCreationExpression creation)
                    {
                        HandleObjectCreation(creation, context);
                    }
                }
            }

            // Any other property setup
            else
            {
                // Find all write usage of the property
                var occurrences = _provider.FindUsagesInFile(property, p => p.DeclaredElement)
                    .Where(x => x.AccessType == ReferenceAccessType.WRITE);

                foreach (var occurrence in occurrences)
                {

                    if (TryExtractInformationFromOccurrence(occurrence, context))
                    {
                        break;
                    }
                }


                //var symbolScope = _provider.PsiServices.Symbols.GetSymbolScope(_provider.PsiModule, false,false);
            }

            // If we have not been able to determine a command method we will generate it
            if (context.CommandMethod == null)
            {
                HandleNoMatchedCommandMethod(property, parent, context);
                // For now we do nothing
            }


            // Remove the property (It will be autogenerted by the source generator)
            parent.RemoveClassMemberDeclaration(property);

            // Run through items to remove (typically how the command was initialized. SomeCommand = new RelayCommand(...)
            foreach (var itemsToRemove in context.ItemsToRemove)
            {
                ModificationUtil.DeleteChild(itemsToRemove);
            }

            // Decorate the CommandMethod will the Relay Property
            context.CommandMethod!.DecorateWithRelayPropertyAttribute(context.CanExecuteMethod?.NameIdentifier.Name,
                _provider.ElementFactory);

        }

        return null;
    }

    private void HandleNoMatchedCommandMethod(IPropertyDeclaration property, IClassLikeDeclaration classLikeDeclaration,
        ConvertToRelayContext context)
    {
        Regex regex = new("Command$");
        var methodName = regex.Replace(property.NameIdentifier.Name, string.Empty);
        if (string.IsNullOrWhiteSpace(methodName))
        {
            methodName = "Execute";
        }

        var methodDeclaration = CreateMethod(property, methodName);
        context.CommandMethod = classLikeDeclaration.AddClassMemberDeclarationAfter(methodDeclaration,
            classLikeDeclaration.MethodDeclarations.LastOrDefault());

        var canExecuteName = $"CanExecute{methodDeclaration.NameIdentifier.Name}";

        if (_provider.ElementFactory.CreateTypeMemberDeclaration("private bool $0 () { return true; }", canExecuteName)
            is not IMethodDeclaration m)
        {
            return;
        }
        
        if (methodDeclaration.ParameterDeclarations.FirstOrDefault() is { } parameter)
        {
            m.AddParameterDeclarationAfter(parameter, null);
        }

        context.CanExecuteMethod = classLikeDeclaration.AddClassMemberDeclarationBefore(m, null);

        // TODO: At some stage we need to check if the method is public and if not called directly make it private


    }

    private IMethodDeclaration CreateMethod(IPropertyDeclaration property, string methodName)
    {
        if (property.Type.GetRelayInformation(property) is { } information)
        {
            if (information.Async)
            {
                return (IMethodDeclaration)(information.HasParameters ? _provider.ElementFactory.CreateTypeMemberDeclaration("private async Task $0 ($1 first) { }", methodName, information.ParameterType) : _provider.ElementFactory.CreateTypeMemberDeclaration("private async Task $0 () {}", methodName, information.ParameterType));
            }
            else
            {
                return (IMethodDeclaration)(information.HasParameters ? _provider.ElementFactory.CreateTypeMemberDeclaration("private void $0($1 first) { }", methodName, information.ParameterType) : _provider.ElementFactory.CreateTypeMemberDeclaration("private void $0 () {}", methodName, information.ParameterType));
            }
        }
        else
        {
            throw new InvalidOperationException($"Could not retrieve relay information for type {property.GetText()}");
        }
    }


    /// <summary>
    /// Tries to extract information from call to a constructor on AsyncRelayCommand or RelayCommand.
    /// Like the method referenced and if present the CanExecute method
    /// </summary>
    /// <param name="creation"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private bool HandleObjectCreation(IObjectCreationExpression creation, ConvertToRelayContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        if (!creation.Type().IsAnyKindOfRelayCommand(creation))
        {
            return false;
        }

        var parametersCount = creation.ArgumentList.Arguments.Count;

        if (parametersCount is 0 or > 2)
        {
            return true;
        }
        
        if (creation.ArgumentList.Arguments[0].Expression.TryGetReferencedNode<IMethodDeclaration>() is
            { } methodDeclaration)
        {
            context.CommandMethod = methodDeclaration;
        }

        if (parametersCount == 2 &&
            creation.ArgumentList.Arguments[1].Expression.TryGetReferencedNode<IMethodDeclaration>() is {} canExecuteDeclaration)
        {
            context.CanExecuteMethod = canExecuteDeclaration;
        }

        return true;

    }


    /// <summary>
    /// Contains data relevant when converting to a relay property
    /// </summary>
    private class ConvertToRelayContext
    {
        public IMethodDeclaration? CommandMethod { get; set; }
        public IMethodDeclaration? CanExecuteMethod { get; set; }

        public ICollection<ITreeNode> ItemsToRemove { get; } = new List<ITreeNode>();
    }

    
    
    /// <summary>
    /// Checks if the given occurence is used inside an Assignment expression and
    /// enriches 
    /// </summary>
    /// <param name="occurrence"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private bool TryExtractInformationFromOccurrence(ReferenceOccurrence occurrence, ConvertToRelayContext context)
    {
        if (occurrence.PrimaryReference?.GetTreeNode().Parent is IAssignmentExpression { Source: IObjectCreationExpression creation} assignment )
        {
            if (HandleObjectCreation(creation, context))
            {
                context.ItemsToRemove.Add(assignment);
                return true;
            }
        }

        return false;
    }

    public override string Text => "Convert to Relay Command (CommunityToolkit SourceGenerator)";

    protected override IPropertyDeclaration? TryCreateInfoFromDataProvider(IUserDataHolder cache)
    {
        if (_provider.GetSelectedTreeNode<IPropertyDeclaration>() is { } property)
        {
            return property;
        }

        return null;

    }

    protected override bool IsAvailable(IPropertyDeclaration property)
    {
        if (!property.CommunityToolkitCanHandleSourceGenerators(null))
        {
            return false;
        }

        if (!property.Type.IsAnyKindOfRelayCommand(property))
        {
            return false;
        }

        return true;
    }
}