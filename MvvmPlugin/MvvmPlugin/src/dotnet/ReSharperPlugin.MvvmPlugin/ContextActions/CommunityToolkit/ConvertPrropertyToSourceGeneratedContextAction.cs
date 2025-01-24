using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit;

[ContextAction(
    Name = "Create viewmodel",
    Description = "Creates a viewmodel for the selected XAML file.",
    GroupType = typeof(CSharpContextActions))]
public class ConvertPropertyToObservableContextAction : ContextActionBase
{
    private readonly ICSharpContextActionDataProvider _provider;

    public ConvertPropertyToObservableContextAction(ICSharpContextActionDataProvider provider)
    {
        _provider = provider;
    }
    
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (_provider.GetSelectedTreeNode<IFieldDeclaration>() is not { } fieldDeclaration)
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

            var factory = CSharpElementFactory.GetInstance(_provider.GetSelectedTreeNode<ICSharpFile>()!);
            var observableProperty = TypeFactory.CreateTypeByCLRName(
                "CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute",
                fieldDeclaration.GetPsiModule());

            var before = fieldDeclaration.AddAttributeBefore(factory.CreateAttribute(observableProperty!.GetTypeElement()!), null);
            fieldDeclaration.AddLineBreakBefore();
            return null;
        }
    }

    public override string Text => "Convert to Observable Property";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        FieldDeclaration = null;
        
        if (_provider.GetSelectedTreeNode<IFieldDeclaration>() is { } fieldDeclaration)
        {
            // Check if the containing class implements the ObservableObject in some way or another

            if (TypeFactory.CreateTypeByCLRName("CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute",
                    fieldDeclaration.GetPsiModule()) is {IsUnknown: false} observableAttribute)
            {
                var containingClass = fieldDeclaration.GetContainingTypeDeclaration();
                if (containingClass == null)
                    return false;
                
                
                var observableObject = TypeFactory.CreateTypeByCLRName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject",
                    containingClass!.GetPsiModule());

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
                
                
                // containing class should inherite from observableobject or an object that inherits from it
                
                return false;
            }
        }
        

        return false;
    }

    public IFieldDeclaration? FieldDeclaration { get; private set; }
}