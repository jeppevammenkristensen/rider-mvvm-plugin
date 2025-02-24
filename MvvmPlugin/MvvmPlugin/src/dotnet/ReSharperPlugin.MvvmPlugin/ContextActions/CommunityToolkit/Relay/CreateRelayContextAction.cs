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
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.VB.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Threading;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;
using IClassLikeDeclaration = JetBrains.ReSharper.Psi.CSharp.Tree.IClassLikeDeclaration;
using IMethodDeclaration = JetBrains.ReSharper.Psi.CSharp.Tree.IMethodDeclaration;

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
        var (valid, methodDeclaration, classLikeDeclaration, relayAttribute) = TryGetItem();

        if (!valid)
            return null;

        var canExecuteName = $"CanExecute{methodDeclaration.NameIdentifier.Name}";
        methodDeclaration.DecorateWithRelayPropertyAttribute(canExecuteName, provider.ElementFactory);

        if (provider.ElementFactory.CreateTypeMemberDeclaration("private bool $0 () { return true; }", canExecuteName)
            is IMethodDeclaration m)
        {
            if (methodDeclaration.ParameterDeclarations.FirstOrDefault() is { } parameter)
            {
                m.AddParameterDeclarationAfter(parameter, null);
            }
            
            classLikeDeclaration.AddClassMemberDeclarationBefore(m, methodDeclaration);
           
            // TODO: At some stage we need to check if the method is public and if not called directly make it private
        }
        
        return null;
    }
    
    public override string Text => "Create Relay (CommunityToolkit)";

    public override bool IsAvailable(IUserDataHolder cache)
    {
        var (valid, methodDeclaration, classLikeDeclaration, relayAttribute) = TryGetItem();

        if (valid == false)
            return false;
        
        if (!classLikeDeclaration.CommunityToolkitCanHandleSourceGenerators(relayAttribute))
            return false;

        // check if method declaration is a Task 
        var isVoidKind = methodDeclaration.Type.IsTask() || methodDeclaration.Type.IsVoid();
        
        // The method must be void and have max 1 parameter (and it should be a value parameter)
        // so no in int firstparam, out string secondParam (etc.)
        if (isVoidKind && methodDeclaration.ParameterDeclarations.Count <= 1 &&
            methodDeclaration.ParameterDeclarations.All(x => x.Kind == ParameterKind.VALUE))
        {
            return methodDeclaration.DoesNotHaveAttribute(relayAttribute);
        }

        return false;

    }

    private (bool, IMethodDeclaration, IClassLikeDeclaration, IDeclaredType) TryGetItem()
    {
        if (provider.GetSelectedTreeNode<IMethodDeclaration>() is
            {
            } methodDeclaration &&
            provider.GetSelectedTreeNode<IClassLikeDeclaration>() is
            {
            } classLikeDeclaration
            && TypeConstants.RelayCommandAttribute.GetDeclaredType(classLikeDeclaration).ShouldBeKnown() is { } relayAttribute)
        {
            return (true, methodDeclaration, classLikeDeclaration, relayAttribute);
        }
        
        return (false, null!, null!, null!);
    }
}