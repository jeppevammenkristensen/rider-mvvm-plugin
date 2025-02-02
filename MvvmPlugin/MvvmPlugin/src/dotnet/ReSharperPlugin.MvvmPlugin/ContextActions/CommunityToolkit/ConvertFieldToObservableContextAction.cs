using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit;

[ContextAction(
    Name = "Make field observable",
    Description = "Decorates the selected field with the ObservablePropertyAttribute. If required the containing class will be made partial.",
    GroupType = typeof(CSharpContextActions))]
public class ConvertFieldToObservableContextAction(ICSharpContextActionDataProvider provider) : ContextActionBase
{
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (provider.GetSelectedTreeNode<IFieldDeclaration>() is not { } fieldDeclaration)
            return null;
        
        using (WriteLockCookie.Create())
        {
            var cSharpTypeDeclaration = fieldDeclaration.GetContainingTypeDeclaration();

            if (!cSharpTypeDeclaration.EnsurePartialAndInheritsObservableObject(observableObject: null))
                return null;

            var factory = CSharpElementFactory.GetInstance(provider.GetSelectedTreeNode<ICSharpFile>()!);
            var observableProperty = TypeFactory.CreateTypeByCLRName(
                "CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute",
                fieldDeclaration.GetPsiModule());

         
            var before = fieldDeclaration.AddAttributeBefore(factory.CreateAttribute(observableProperty!.GetTypeElement()!), null);
            before.AddLineBreakAfter();
            
            return null;
        }
    }

    public override string Text => "Make Field Observable";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        FieldDeclaration = null;
        
        // Check is this is a field declartion
        if (provider.GetSelectedTreeNode<IFieldDeclaration>() is { DeclaredElement: {} } fieldDeclaration)
        {
            // Check if the containing class implements the ObservableObject in some way or another
            if (PluginUtil.GetObservablePropertyAttribute(fieldDeclaration).ShouldBeKnown() is {} observableAttribute)
            {
                // If the field declaration has no Attributes we return true 
                // as the ObservablePropertyAttribute can safely be added
                if (!fieldDeclaration.Attributes.Any())
                {
                    FieldDeclaration = fieldDeclaration;
                    return true;
                }

                // If the field is not allready decorated with a ObservableProperty attribute we
                // return true
                return !fieldDeclaration.DeclaredElement.HasAttributeInstance(observableAttribute.GetClrName(),
                    false);
            }
        }
        

        return false;
    }

    public IFieldDeclaration? FieldDeclaration { get; private set; }
}