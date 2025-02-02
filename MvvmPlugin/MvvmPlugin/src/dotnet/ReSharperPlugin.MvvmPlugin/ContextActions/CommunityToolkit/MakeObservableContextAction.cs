using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit;

[ContextAction(
    Name = "Make Class Observable",
    Description = "Lets the class inherit from ObservableObject. If required the containing class will be made partial.",
    GroupType = typeof(CSharpContextActions))]
public class MakeObservableContextAction : ContextActionBase
{
    private readonly ICSharpContextActionDataProvider _provider;

    public MakeObservableContextAction(ICSharpContextActionDataProvider provider)
    {
        _provider = provider;
    }
    
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (_provider.GetSelectedTreeNode<IClassDeclaration>() is not { } classLikeDeclaration)
            return null;
        
        using (WriteLockCookie.Create())
        {
            classLikeDeclaration.EnsurePartialAndInheritsObservableObject(observableObject: null);
            return null;
        }
    }

    public override string Text => "Make Class ObservableObject";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        FieldDeclaration = null;
        
        if (_provider.GetSelectedTreeNode<IClassDeclaration>() is { } classLikeDeclaration)
        {
            // Check if the containing class implements the ObservableObject in some way or another

            if (PluginUtil.GetObservableObject(classLikeDeclaration).ShouldBeKnown() is { } observableObject)
            {
                var declaredElement = classLikeDeclaration.DeclaredElement;
                if (declaredElement == null)
                    return false;

                if (!classLikeDeclaration.IsPartial)
                    return true;

                return !declaredElement.IsDescendantOf(observableObject.GetTypeElement()) && !classLikeDeclaration.SuperTypes.Any(x => x.IsClassType());
            }
        }
        

        return false;
    }

    public IFieldDeclaration? FieldDeclaration { get; private set; }
}