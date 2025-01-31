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
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit;

[ContextAction(
    Name = "Make observable object",
    Description = "Makes the class inherit from ObservableObject",
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
            if (!classLikeDeclaration.IsPartial)
            {
                classLikeDeclaration.SetPartial(true);
            }

            var observableProperty = PluginUtil.GetObservableObject(classLikeDeclaration);

            classLikeDeclaration.SetSuperClass(observableProperty);
            return null;
        }
    }

    public override string Text => "Make ObservableObject";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        FieldDeclaration = null;
        
        if (_provider.GetSelectedTreeNode<IClassDeclaration>() is { } classLikeDeclaration)
        {
            // Check if the containing class implements the ObservableObject in some way or another

            if (TypeFactory.CreateTypeByCLRName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject",
                    classLikeDeclaration.GetPsiModule()) is {IsUnknown: false} observableObject)
            {
                var declaredElement = classLikeDeclaration.DeclaredElement;
                if (declaredElement == null)
                    return false;

                return !declaredElement.IsDescendantOf(observableObject.GetTypeElement());
            }
        }
        

        return false;
    }

    public IFieldDeclaration? FieldDeclaration { get; private set; }
}