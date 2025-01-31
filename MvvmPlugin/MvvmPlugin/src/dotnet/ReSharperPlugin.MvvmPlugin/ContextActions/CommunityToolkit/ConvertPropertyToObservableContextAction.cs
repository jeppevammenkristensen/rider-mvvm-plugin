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
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit;

[ContextAction(Name = "Make property observable", Description = "Converts the property to an observable property", GroupType = typeof(CSharpContextActions))]
public class ConvertPropertyToObservableContextAction(ICSharpContextActionDataProvider provider) : ContextActionBase
{
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (provider.GetSelectedTreeNode<IPropertyDeclaration>() is not { } propertyDeclaration)
            return null;
        
        var cSharpTypeDeclaration = propertyDeclaration.GetContainingTypeDeclaration();
        if (cSharpTypeDeclaration is not IClassDeclaration classLikeDeclaration)
            return null;

        if (classLikeDeclaration.DeclaredElement is null)
        {
            return null;
        }

        if (PluginUtil.GetObservableObject(propertyDeclaration) is {IsUnknown: false} observableObject &&
            PluginUtil.GetObservablePropertyAttribute(propertyDeclaration) is {IsUnknown: false} observableProperty)
        {
            using (WriteLockCookie.Create())
            {
                if (!cSharpTypeDeclaration.IsPartial)
                {
                    cSharpTypeDeclaration.SetPartial(true);
                }

                if (!classLikeDeclaration.DeclaredElement.IsDescendantOf(observableObject.GetTypeElement()))
                {
                    if (classLikeDeclaration.SuperTypes.FirstOrDefault() is { } first &&
                        first.GetTypeElement().IsClassLike())
                    {
                        classLikeDeclaration.SetSuperClass(observableObject);
                    }
                }

                var factory = CSharpElementFactory.GetInstance(provider.GetSelectedTreeNode<ICSharpFile>()!);

                var field = factory.CreateTypeMemberDeclaration("[$0]\r\nprivate $1 $2;",
                    observableProperty,
                    propertyDeclaration.Type, Extensions.Extensions.ToSnakeCase(propertyDeclaration.DeclaredName));

                if (field is IFieldDeclaration {Parent: IMultipleFieldDeclaration fieldDeclaration})
                {
                    ModificationUtil.ReplaceChild(propertyDeclaration, fieldDeclaration);    
                }
                

            }
        }

        return null;



    }

    public override string Text => "Make property observable";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        if (provider.GetSelectedTreeNode<IPropertyDeclaration>() is not { } propertyDeclaration)
            return false;

        if (PluginUtil.GetObservableObject(propertyDeclaration) is not {IsUnknown: false})
            return false;

        return true;

    }
}