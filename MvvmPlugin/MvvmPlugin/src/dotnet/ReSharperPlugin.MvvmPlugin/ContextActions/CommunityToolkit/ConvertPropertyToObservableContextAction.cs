using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Psi;
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

[ContextAction(Name = "Make property observable", Description = "Converts the property to a field and decorates it with the ObservableProperty", GroupType = typeof(CSharpContextActions))]
public class ConvertPropertyToObservableContextAction(ICSharpContextActionDataProvider provider) : ContextActionBase
{
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
        
        var cSharpTypeDeclaration = propertyDeclaration.GetContainingTypeDeclaration();

        

        if (PluginUtil.GetObservableObject(propertyDeclaration) is {IsUnknown: false} observableObject &&
            PluginUtil.GetObservablePropertyAttribute(propertyDeclaration) is {IsUnknown: false} observableProperty)
        {
            using (WriteLockCookie.Create())
            {
                // This will ensure that the containing class is partial and if possible
                // inherits from ObservableObject
                if (!cSharpTypeDeclaration.EnsurePartialAndInheritsObservableObject(observableObject))
                    return null;

                // Get the factory we will use to generate a field
                var factory = CSharpElementFactory.GetInstance(provider.GetSelectedTreeNode<ICSharpFile>()!);

                // Create a field declaration with the type from the property and a snake cased name
                // The property should have a summart with a cref to the property that is generated behind
                // it will also be decorated with the ObservableProperty attribute
                var field = factory
                    .CreateTypeMemberDeclaration($"/// <summary>\n    /// <see cref=\"{propertyDeclaration.DeclaredName}\"/>\n    /// </summary>\n[$0]\nprivate $1 $2;",
                     observableProperty,
                    propertyDeclaration.Type, propertyDeclaration.DeclaredName.ToSnakeCase());

                if (field is IFieldDeclaration  {Parent: IMultipleFieldDeclaration multiFieldDeclaration} fieldDeclaration)
                {
                    // If there is a initalizer on the property. For instance = "Hello World" 
                    // we apply that to the field
                    if (propertyDeclaration.Initializer is IExpressionInitializer initializer)
                    {
                        fieldDeclaration.SetInitial(initializer);
                    }
                    
                    // Replace the property with the field
                    ModificationUtil.ReplaceChild(propertyDeclaration, multiFieldDeclaration);    
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