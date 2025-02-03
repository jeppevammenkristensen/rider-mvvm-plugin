using System;
using System.Linq;
using System.Text;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit;

[ContextAction(
    Name = "Make property observable",
    Description = "Converts the property to a field and decorates it with the ObservableProperty", 
    GroupType = typeof(CSharpContextActions))]
public class MakePropertyToObservableContextAction(ICSharpContextActionDataProvider provider) : ContextActionBase
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
        var project = propertyDeclaration.GetProject();

        bool usePartialMethodAproach = false;
        
        if (this.LargerThanOrEqualToVersion84 && project?.ProjectProperties.ActiveConfigurations.Configurations.FirstOrDefault() is CSharpProjectConfiguration  configuration)
        {
            // If LanguageVersion is preview and targetframeworkId is 9 or more (might change)
            // we use the approach with declaring a partial property instead of a field
            // Right now it requires this. It might change in the future

            if (configuration.LanguageVersion == CSharpLanguageVersion.Preview && configuration.TargetFrameworkId is
                {
                   Version: {Major: >= 9}
                })
            {
                usePartialMethodAproach = true;
            } 
        }

        if (PluginUtil.GetObservableObject(propertyDeclaration).ShouldBeKnown() is {} observableObject &&
            PluginUtil.GetObservablePropertyAttribute(propertyDeclaration).ShouldBeKnown() is {} observableProperty)
        {
            using (WriteLockCookie.Create())
            {
                // This will ensure that the containing class is partial and if possible
                // inherits from ObservableObject
                if (!cSharpTypeDeclaration.EnsurePartialAndInheritsObservableObject(observableObject, supressObservableObjectNotFound: false))
                    return null;
                
                // Get the factory we will use to generate a field
                var factory = CSharpElementFactory.GetInstance(provider.GetSelectedTreeNode<ICSharpFile>()!);

                if (usePartialMethodAproach)
                {
                    propertyDeclaration.SetPartial(true);
                    propertyDeclaration.AddAttributeBefore(
                        factory.CreateAttribute(observableProperty.GetTypeElement()!), null);
                    return null;
                }
                
                // Create a field declaration with the type from the property and a snake cased name
                // The property should have a summary with a cref to the property that is generated behind
                // it will also be decorated with the ObservableProperty attribute
                
                 var field = BuildField(factory, propertyDeclaration, observableProperty);
                
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

    private IFieldDeclaration BuildField(CSharpElementFactory factory, IPropertyDeclaration propertyDeclaration, IDeclaredType observableProperty)
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
        builder.AppendLine($"private $1 {propertyDeclaration.DeclaredName.ToSnakeCase()};");
        
        var field = (IFieldDeclaration)factory.CreateTypeMemberDeclaration(builder.ToString(), observableProperty, propertyDeclaration.Type);
        
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

        return field;
    }

    public override string Text => "Make property observable";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        if (provider.GetSelectedTreeNode<IPropertyDeclaration>() is not { } propertyDeclaration)
            return false;

        if (PluginUtil.GetObservablePropertyAttribute(propertyDeclaration).ShouldBeKnown() is not {} observableProperty)
            return false;

        if (!propertyDeclaration.CommunityToolkitCanHandleSourceGenerators(observableProperty))
            return false;

        LargerThanOrEqualToVersion84 = propertyDeclaration.CommunityToolkitCanHandlePartialProperties(observableProperty);

        if (propertyDeclaration.DeclaredElement?.HasAttributeInstance(observableProperty.GetClrName(), false) == true)
            return false;

        return true;

    }

    private bool LargerThanOrEqualToVersion84 { get; set; }
}