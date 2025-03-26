using System;
using System.Linq;
using System.Text;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.Navigation.ReferencedCode;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Intentions.JavaScript.QuickFixes.TypeScript.ChangeAll;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.Logging;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;
using IClassLikeDeclaration = JetBrains.ReSharper.Psi.CSharp.Tree.IClassLikeDeclaration;
using IMethodDeclaration = JetBrains.ReSharper.Psi.CSharp.Tree.IMethodDeclaration;
using IPropertyDeclaration = JetBrains.ReSharper.Psi.VB.Tree.IPropertyDeclaration;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

[ContextAction(
    Name = "Make relay with can execute (CommunityToolkit)",
    Description =
        "Makes the given type a relay",
    GroupType = typeof(CSharpContextActions))]
public class CreateRelayContextAction(ICSharpContextActionDataProvider provider) : ContextActionBase
{
    /// <summary>
    /// <see cref="ExecutePsiTransaction"/>
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        var (valid, methodDeclaration, classLikeDeclaration, _) = TryGetItem();

        if (!valid)
            return null;

        var canExecuteName = $"CanExecute{methodDeclaration.NameIdentifier.Name}";

        if (provider.ElementFactory.CreateTypeMemberDeclaration("private bool $0 () { return true; }", canExecuteName)
            is not IMethodDeclaration m)
        {
            return null;
        }

        if (CanHandleSourceGenerators)
        {
            methodDeclaration.DecorateWithRelayPropertyAttribute(canExecuteName, provider.ElementFactory);
            
            if (methodDeclaration.ParameterDeclarations.FirstOrDefault() is { } parameter)
            {
                m.AddParameterDeclarationAfter(parameter, null);
            }
        
            classLikeDeclaration.AddClassMemberDeclarationBefore(m, methodDeclaration);
           
                // TODO: At some stage we need to check if the method is public and if not called directly make it private
                
        }
        else
        {
            var factory = provider.ElementFactory;
            var fieldName = methodDeclaration.NameIdentifier.Name.ToFieldName();

            var (relayInterface, relay) = GetRelayCommandTypes(methodDeclaration, classLikeDeclaration);
            
            classLikeDeclaration.AddClassMemberDeclarationBefore(m, methodDeclaration);

            var field = (IFieldDeclaration) factory.CreateTypeMemberDeclaration("private $0? $1;", relayInterface,
                fieldName);
            classLikeDeclaration.AddClassMemberDeclarationBefore(field, methodDeclaration);

            if (factory.CreateTypeMemberDeclaration("public $0 $1Command => $2 ??= new $3($1, $4);", relayInterface,
                    methodDeclaration.NameIdentifier.Name, fieldName, relay, canExecuteName) is IClassMemberDeclaration property)
            {
                classLikeDeclaration.AddClassMemberDeclarationAfter(property, methodDeclaration);
            }
            
        }
        
        
        
        return null;
    }

    private static (IDeclaredType relayInterface, IDeclaredType relay)  GetRelayCommandTypes(IMethodDeclaration methodDeclaration,
        IClassLikeDeclaration classLikeDeclaration)
    {
        bool isTask = methodDeclaration.Type.IsTask();

        var hasParameter = methodDeclaration.ParameterDeclarations.Count == 1;
        IDeclaredType? parameter = hasParameter ? methodDeclaration.ParameterDeclarations[0].Type as IDeclaredType : null;

        if (isTask)
        {
            if (hasParameter)
            {
                return (TypeConstants.IAsyncRelayCommand.GenericOneType().GetSingleClosedType(methodDeclaration, parameter), TypeConstants.AsyncRelayCommand.GenericOneType().GetSingleClosedType(methodDeclaration, parameter));

            }
            else
            {
                return (TypeConstants.IAsyncRelayCommand.GetDeclaredType(methodDeclaration), TypeConstants.AsyncRelayCommand.GetDeclaredType(methodDeclaration));
            }
        }
        else
        {
            if (hasParameter)
            {
                return (TypeConstants.IRelayCommand.GenericOneType().GetSingleClosedType(methodDeclaration,parameter), TypeConstants.RelayCommand.GenericOneType().GetSingleClosedType(methodDeclaration, parameter));
            }
            else
            {
                return (TypeConstants.IRelayCommand.GetDeclaredType(methodDeclaration), TypeConstants.RelayCommand.GetDeclaredType(methodDeclaration));   
            }
            
        }
       
    }

    public override string Text => "Create Relay (CommunityToolkit)";

    public override bool IsAvailable(IUserDataHolder cache)
    {
        var (valid, methodDeclaration, classLikeDeclaration, relayAttribute) = TryGetItem();

        if (valid == false)
            return false;
        
        CanHandleSourceGenerators = classLikeDeclaration.CommunityToolkitCanHandleSourceGenerators(relayAttribute);

        // check if method declaration is a Task or void 
        var isVoidKind = methodDeclaration.Type.IsTask() || methodDeclaration.Type.IsVoid();
        
        // The method must be void and have max 1 parameter (and it should be a value parameter)
        // so no in int firstparam, out string secondParam (etc.)
        if (isVoidKind && methodDeclaration.ParameterDeclarations.Count <= 1 &&
            methodDeclaration.ParameterDeclarations.All(x => x.Kind == ParameterKind.VALUE))
        {
            if (CanHandleSourceGenerators)
            {
                return methodDeclaration.DoesNotHaveAttribute(relayAttribute!);    
            }
            else
            {
                // Find usages of the given method
                var psiServices = methodDeclaration.GetPsiServices();
                var consumer = new SearchResultsConsumer();

                try
                {
                    psiServices.SingleThreadedFinder.FindReferences(
                        methodDeclaration.DeclaredElement,
                        domain: SearchDomainFactory.Instance.CreateSearchDomain(methodDeclaration.GetSourceFile()),
                        consumer: consumer,
                        NullProgressIndicator.Create());
                }
                catch (Exception ex)
                {
                    Logger.LogException("Failed to find references",ex);
                    return false;
                }

                foreach (var occurrence in consumer.GetOccurrences()
                             .OfType<ReferenceOccurrence>())
                {
                    if (occurrence.GetTypeMember()?.GetValidDeclaredElement() is ICSharpProperty property)
                    {

                        if (property.ReturnType.IsRelayCommand())
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            
        }

        return false;

    }

    public bool CanHandleSourceGenerators { get; private set; }

    private (bool valid, IMethodDeclaration method, IClassLikeDeclaration containingClass, IDeclaredType? relayAttribute) TryGetItem()
    {
        if (provider.GetSelectedTreeNode<IMethodDeclaration>() is
            {
            } methodDeclaration &&
            provider.GetSelectedTreeNode<IClassLikeDeclaration>() is
            {
            } classLikeDeclaration)
        {
            if (TypeConstants.RelayCommandAttribute.GetDeclaredType(classLikeDeclaration).ShouldBeKnown() is
                { } relayAttribute)
            {
                return (true, methodDeclaration, classLikeDeclaration, relayAttribute);    
            }
            else if (TypeConstants.ObservableObject.GetDeclaredType(classLikeDeclaration).ShouldBeKnown() is { })
            {
                return (true, methodDeclaration, classLikeDeclaration, null);
            }
        }
        
        return (false, null!, null!, null!);
    }
}