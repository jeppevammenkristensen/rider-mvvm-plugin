using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Logging;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

[ContextAction(
    Name = "Make property observable (CommunityToolkit)",
    Description =
        "Converts the property to a field and decorates it with the ObservableProperty if partial properties are not supported. Otherwise it will make the property partial and decoreate with the ObservableProperty attribute",
    GroupType = typeof(CSharpContextActions))]
public class MakePropertyToObservableContextAction(ICSharpContextActionDataProvider provider) : ContextActionBase
{
    private string _property;

    /// <summary>
    /// <see cref="ExecutePsiTransaction"/>
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (provider.GetSelectedTreeNode<IPropertyDeclaration>() is not { } propertyDeclaration)
            return null;

        var classLike = ClassLikeDeclarationNavigator.GetByPropertyDeclaration(propertyDeclaration);
        if (classLike is null)
            return null;

        MakePropertyToObservableDataContext dataContext = new();

        if (classLike.DeclaredElement is not IClass classElement)
        {
            Logger.LogError("Could not convert element to IClass");
            return null;
        }
        
        //var cSharpTypeDeclaration = propertyDeclaration.GetContainingTypeDeclaration();
        var project = propertyDeclaration.GetProject();

        bool usePartialMethodAproach = propertyDeclaration.CommunityToolkitCanHandlePartialProperties(null);

        if (!propertyDeclaration.IsAuto)
        {
            dataContext.ShouldConvertToAuto = true;

            // IReadOnlyList<IMethod> notifyMethods =
            //     NotifyPropertyChangedUtil.GetNotifyMethods(classElement, provider.PsiModule);

            NotifyCollector collector = new();
            var accessorDeclaration = propertyDeclaration.GetAccessorDeclaration(AccessorKind.SETTER);
            if (accessorDeclaration is not null)
            {
                accessorDeclaration.ProcessDescendants(collector);
                foreach (var (invocationExpression, method) in collector.NotifyInvocations)
                {
                    if (NotifyPropertyChangedUtil.ClassifyNotifierMethodSignature(method) != NotifyPropertyChangedUtil.NotifyMethodType.NotNotifier &&
                        invocationExpression.Arguments.Count > 0)
                    {
                        var property = invocationExpression.GetPropertyNameFromPropertyChangedInvocation(method);
                        if (property == null || property.Equals(propertyDeclaration.NameIdentifier.Name))
                            continue;
                        
                        dataContext.AddNotifyProperty(property);
                    }
                }
                
                foreach (var canExecuteCommandName in collector.CanExecuteCommands)
                {
                    propertyDeclaration.AddCanExecuteChangedForAttribute(canExecuteCommandName, provider.ElementFactory);
                }
            }
        }

        if (PluginUtil.GetObservableObject(propertyDeclaration).ShouldBeKnown() is { } observableObject &&
            PluginUtil.GetObservablePropertyAttribute(propertyDeclaration).ShouldBeKnown() is { } observableProperty)
        {
            using (WriteLockCookie.Create())
            {
                // This will ensure that the containing class is partial and if possible
                // inherits from ObservableObject
                if (!classLike.EnsurePartialAndInheritsObservableObject(observableObject,
                        supressObservableObjectNotFound: false))
                    return null;

                // Get the factory we will use to generate a field
                var factory = provider.ElementFactory;
                
                if (usePartialMethodAproach)
                {
                    propertyDeclaration.SetPartial(true);
                    propertyDeclaration.AddAttributeBefore(
                        factory.CreateAttribute(observableProperty.GetTypeElement()!), null);
                    if (dataContext.ShouldConvertToAuto)
                    {
                        propertyDeclaration.ConvertToAutoProperty(factory);
                    }

                    foreach (var property in dataContext.NotifyProperties)
                    {
                        propertyDeclaration.AddNotifyPropertyChangedAttribute(property,factory);
                    }
                    
                    
                    return null;
                }

                // Create a field declaration with the type from the property and a snake cased name
                // The property should have a summary with a cref to the property that is generated behind
                // it will also be decorated with the ObservableProperty attribute

                var field = BuildField(factory, propertyDeclaration, observableProperty, dataContext);

                // We retrieve the parent. this will ensure that the output will be 
                // with attributes, documents etc. If we only use the field declaration it will something like
                // _name; or _name = "Hello" while IMultipleFieldDeclaration will generate something akin to
                // private string _name = "Hello"; (also with summary and attributes)
                if (field is {Parent: IMultipleFieldDeclaration multiFieldDeclaration})
                {
                    // Replace the property with the field
                    ModificationUtil.ReplaceChild(propertyDeclaration, multiFieldDeclaration);
                }
            }
        }

        return null;
    }

    private class NotifyCollector : IRecursiveElementProcessor
    {
        public List<(IInvocationExpression, IMethod)> NotifyInvocations { get; private set; } = new();
        public HashSet<string> CanExecuteCommands { get; private set; } = new();
        
        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            return true;
        }

        public void ProcessBeforeInterior(ITreeNode element)
        {
            switch (element)
            {
                case IInvocationExpression invocationExpression:
                    ProcessInvocation(invocationExpression);
                    break;
            }
        }

        private static string[] CanExecuteNames = ["NotifyCanExecuteChanged", "RaiseCanExecuteChanged"];
 
        private void ProcessInvocation(IInvocationExpression invocationExpression)
        {
            if (invocationExpression.Reference.Resolve().DeclaredElement is IMethod method)
            {
                var result = NotifyPropertyChangedUtil.ClassifyNotifierMethodSignature(method);
                if (result == NotifyPropertyChangedUtil.NotifyMethodType.NotNotifier)
                {
                    // Note. This can result in false positives on rare occasion as we basically "only" check
                    // if the containing type of the method is an ICommand (for instance NotityCanExecuteChanged is only
                    // available on IRelayCommand. 
                    if (method.ContainingType?.IsDescendantOf(TypeConstants.ICommand.GetDeclaredType(invocationExpression).GetTypeElement()) == true)
                    {
                        // Check if the method name is in the list of CanExecuteNames 
                        if (CanExecuteNames.Contains(method.ShortName))
                        {
                            // Traverse up the invocation expression to find the top most reference. 
                            // For SomeCommand.NotifyCanExecuteChanged() that would be SomeCommand
                            if (invocationExpression.ReferencePath().LastOrDefault() is { } res)
                            {
                                CanExecuteCommands.Add(res.NameIdentifier.Name); }
                            
                        }
                    }
                }
                else
                {
                    NotifyInvocations.Add((invocationExpression,method));        
                }
            }
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
        }

        public bool ProcessingIsFinished { get; private set; }
    }

    private IFieldDeclaration BuildField(CSharpElementFactory factory, IPropertyDeclaration propertyDeclaration,
        IDeclaredType observableProperty, MakePropertyToObservableDataContext dataContext)
    {
        var builder = new StringBuilder();

        // Reuse a comment block if it is already set
        if (propertyDeclaration.DocCommentBlock is { } docCommentBlock)
        {
            builder.AppendLine(docCommentBlock.GetText());
        }
        else
        {
            builder.AppendLine("/// <summary>");
            builder.AppendLine($"/// Generated property <see cref=\"{propertyDeclaration.DeclaredName}\"/> ");
            builder.AppendLine("/// </summary>");
        }

        // $0 ObservableProperty reference
        // $1 Property type

        builder.AppendLine($"[$0]");
        builder.AppendLine($"private $1 {propertyDeclaration.DeclaredName.ToFieldName()};");

        var field = (IFieldDeclaration) factory.CreateTypeMemberDeclaration(builder.ToString(), observableProperty,
            propertyDeclaration.Type);

        // Here we check to see if we need to transfer attributes and/or initialization to the field

        // If there are attributes on the property add them to the field
        // The property is filtered away if it is decorated with ObservableProperty
        // So we don't check for that
        if (propertyDeclaration.Attributes is {Count: > 0} attributes)
        {
            foreach (var attribute in attributes)
            {
                field.AddAttributeAfter(attribute, field.Attributes.Last());
            }
        }

        // If there is an initializer on the property. For instance = "Hello World" 
        // we apply that to the field
        if (propertyDeclaration.Initializer is IExpressionInitializer initializer)
        {
            field.SetInitial(initializer);
        }
        
        foreach (var property in dataContext.NotifyProperties)
        {
            field.AddNotifyPropertyChangedAttribute(property,factory);
        }

        return field;
    }

    public override string Text => "Make property observable (CommunityToolkit)";

    public override bool IsAvailable(IUserDataHolder cache)
    {
        if (provider.GetSelectedTreeNode<IPropertyDeclaration>() is not { } propertyDeclaration)
            return false;

        if (PluginUtil.GetObservablePropertyAttribute(propertyDeclaration).ShouldBeKnown() is not
            { } observableProperty)
            return false;

        // Do not suggest this action if the type of the property is an AsyncRelayCommand
        if (propertyDeclaration.Type.IsAnyKindOfRelayCommand(propertyDeclaration))
        {
            return false;
        }

        if (!propertyDeclaration.CommunityToolkitCanHandleSourceGenerators(observableProperty))
            return false;

        LargerThanOrEqualToVersion84 =
            propertyDeclaration.CommunityToolkitCanHandlePartialProperties(observableProperty);

        if (propertyDeclaration.DeclaredElement?.HasAttributeInstance(observableProperty.GetClrName(), false) == true)
            return false;

        return true;
    }

    private class MakePropertyToObservableDataContext
    {
        public ImmutableArray<string> NotifyProperties { get; private set; } = ImmutableArray<string>.Empty;
        public bool ShouldConvertToAuto { get; set; }

        public void AddNotifyProperty(string propertyName)
        {
            NotifyProperties = NotifyProperties.Add(propertyName);
        }

    }
    
    private bool LargerThanOrEqualToVersion84 { get; set; }
}