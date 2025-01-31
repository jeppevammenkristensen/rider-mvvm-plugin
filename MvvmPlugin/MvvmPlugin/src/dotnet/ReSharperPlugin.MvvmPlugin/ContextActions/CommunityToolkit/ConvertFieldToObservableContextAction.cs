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

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit;

[ContextAction(
    Name = "Make field observable",
    Description = "Decorates the selected field with the ObservablePropertyAttribute",
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
            if (cSharpTypeDeclaration == null)
                return null;
            
            if (!cSharpTypeDeclaration.IsPartial)
            {
                cSharpTypeDeclaration.SetPartial(true);
            }

            var factory = CSharpElementFactory.GetInstance(provider.GetSelectedTreeNode<ICSharpFile>()!);
            var observableProperty = TypeFactory.CreateTypeByCLRName(
                "CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute",
                fieldDeclaration.GetPsiModule());

            var before = fieldDeclaration.AddAttributeBefore(factory.CreateAttribute(observableProperty!.GetTypeElement()!), null);
            fieldDeclaration.AddLineBreakBefore();
            return null;
        }
    }

    public override string Text => "Decorate with ObservablePropertyAttribute";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        FieldDeclaration = null;
        
        // Check is this is a field declartion
        if (provider.GetSelectedTreeNode<IFieldDeclaration>() is { } fieldDeclaration)
        {
            // Check if the containing class implements the ObservableObject in some way or another
            if (TypeFactory.CreateTypeByCLRName("CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute",
                    fieldDeclaration.GetPsiModule()) is {IsUnknown: false} observableAttribute)
            {
                // Find the containing class and exit if it is not found
                var containingClass = fieldDeclaration.GetContainingTypeDeclaration();
                if (containingClass == null)
                    return false;
                
                
                // Retrieve the ObservableObject type
                var observableObject = TypeFactory.CreateTypeByCLRName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject",
                    containingClass!.GetPsiModule());

                // If the given class does not inherit in some way from ObservableObject return false
                if (!containingClass.DeclaredElement.IsDescendantOf(observableObject.GetTypeElement()))
                {
                    return false;
                }

                if (!fieldDeclaration.Attributes.Any())
                {
                    FieldDeclaration = fieldDeclaration;
                    return true;
                }
                else
                {
                    return !fieldDeclaration.DeclaredElement.HasAttributeInstance(observableAttribute.GetClrName(),
                        false);
                }
                
                
            }
        }
        

        return false;
    }

    public IFieldDeclaration? FieldDeclaration { get; private set; }
}