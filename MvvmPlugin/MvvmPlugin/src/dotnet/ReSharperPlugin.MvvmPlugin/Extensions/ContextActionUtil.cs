using System;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.Extensions;

public static class ContextActionUtil
{
    public static CSharpProjectConfiguration? GetCSharpProjectConfiguration(this ITreeNode treeNode)
    {
        
        if (treeNode.GetProject() is { } project &&
            project.ProjectProperties.TryGetConfiguration<CSharpProjectConfiguration>(project.GetCurrentTargetFrameworkId()) is {} configuration)
        {
            return configuration;
        }

        return null;
    }

    /// <summary>
    /// It's required that the csharp language version is at least 8 to support CommunityToolkit.Mvvm Source generators
    /// </summary>
    /// <param name="treeNode"></param>
    /// <returns></returns>
    public static bool LanguageVersionSupportCommunityToolkitSourceGenerators(this JetBrains.ReSharper.Psi.Tree.ITreeNode treeNode)
    {
        if (treeNode.GetCSharpProjectConfiguration() is {} configuration)
        {
            return configuration.LanguageVersion >= CSharpLanguageVersion.CSharp8;
        }

        return false;
    }

    /// <summary>
    /// Checks if the CommunityTookit is referenced in the project and that
    /// it is at least version 8 (which supports source generators)
    /// </summary>
    /// <param name="treeNode"></param>
    /// <param name="communityToolkitType"></param>
    /// <returns></returns>
    public static bool CommunityToolkitCanHandleSourceGenerators(this ITreeNode treeNode,
        IDeclaredType? communityToolkitType)
    {
        communityToolkitType ??= PluginUtil.GetObservableObject(treeNode).ShouldBeKnown();
        if (communityToolkitType is null or not {Assembly.Version: {}})
        {
            return false;
        }

        return communityToolkitType.Assembly.Version?.Major >= 8;
    }
    
    /// <summary>
    /// If the community toolkit reference is 8.4 or larger and language type is preview
    /// then we can use Partial properties instead of fields for generation of properties
    /// </summary>
    /// <param name="treeNode"></param>
    /// <param name="communityToolkitType"></param>
    /// <returns></returns>
    public static bool CommunityToolkitCanHandlePartialProperties(this ITreeNode treeNode,
        IDeclaredType? communityToolkitType)
    {
        communityToolkitType ??= PluginUtil.GetObservableObject(treeNode).ShouldBeKnown();
        if (communityToolkitType is null or not {Assembly.Version: {}})
        {
            return false;
        }

        return communityToolkitType.Assembly?.Version >=
               new Version(8,
                   4) &&
               treeNode.GetCSharpProjectConfiguration() is {LanguageVersion: CSharpLanguageVersion.Preview} config &&
               config.IsDotnet90OrHigher();

    }
    
    /// <summary>
    /// Creates an public partial property that is decorated with the ObservableProperty attribute
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="propertyName"></param>
    /// <param name="propertyType"></param>
    /// <returns></returns>
    public static IPropertyDeclaration CreateObservableProperty(this CSharpElementFactory factory, string? propertyName = null, IType? propertyType = null)
    {
        IPropertyDeclaration propertyDeclaration;
        if (propertyType is null)
        {
            propertyDeclaration = (IPropertyDeclaration)factory.CreateTypeMemberDeclaration("public TYPE $0 {get;set;}",
                propertyName ?? PluginConstants.PlaceHolderName);
        }
        else
        {
            propertyDeclaration = factory.CreatePropertyDeclaration(propertyType, propertyName ?? PluginConstants.PlaceHolderName);
            propertyDeclaration.SetAccessRights(AccessRights.PUBLIC);
            IAccessorDeclaration accessorDeclaration1 = factory.CreateAccessorDeclaration(AccessorKind.GETTER, false);
            IAccessorDeclaration accessorDeclaration2 = factory.CreateAccessorDeclaration(AccessorKind.SETTER, false);
            propertyDeclaration.AddAccessorDeclarationAfter(accessorDeclaration1, null);
            propertyDeclaration.AddAccessorDeclarationBefore(accessorDeclaration2, null);
        }
        
        
        propertyDeclaration.SetPartial(true);

        propertyDeclaration.DecorateWithObservablePropertyAttribute(factory);
              
        return propertyDeclaration;  
    }

    /// <summary>
    /// Evaluates if the given configuration is dotnet 9 or higher. If the passed in configuration
    /// is null false is returned
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static bool IsDotnet90OrHigher(this CSharpProjectConfiguration? configuration)
    {
        if (configuration == null)
            return false;
        
        return configuration.TargetFrameworkId is {Version.Major: >= 9} target &&
               (target.IsNetCore || target.IsNetCoreApp);
    }
    
    public static void DecorateWithObservablePropertyAttribute(this ICSharpTypeMemberDeclaration typeDeclaration, CSharpElementFactory factory)
    {
        var observableProperty = TypeFactory.CreateTypeByCLRName(
            "CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute",
            typeDeclaration.GetPsiModule());
        
        var before = typeDeclaration.AddAttributeBefore(factory.CreateAttribute(observableProperty!.GetTypeElement()!), null);
        before.AddLineBreakAfter();
    }
    
    /// <summary>
    /// Ensures partial and that the declaration inherits from ObservableObject
    /// If the ObservableObject could not be found false will be retured
    /// </summary>
    /// <param name="declaration"></param>
    /// <param name="observableObject">If null the type will be loaded when this method is called</param>
    /// <param name="supressObservableObjectNotFound">If the type is not found and this value is true. The type will still be used</param>
    /// <returns></returns>
    public static bool EnsurePartialAndInheritsObservableObject(this ICSharpTypeDeclaration? classLikeDeclaration, IDeclaredType? observableObject, bool supressObservableObjectNotFound)
    {
        if (classLikeDeclaration is not IClassDeclaration declaration)
            return false;
        
        observableObject ??= PluginUtil.GetObservableObject(declaration); //.ShouldBeKnown();
        
        if (observableObject.ShouldBeKnown() is null && !supressObservableObjectNotFound)
        {
            return false;
        }
        
        if (declaration.DeclaredElement is null)
        {
            return false;
        }
        
        if (!declaration.IsPartial)
        {
            declaration.SetPartial(true);
        }

        if (!declaration.DeclaredElement.IsDescendantOf(observableObject.GetTypeElement()))
        {
            if (!declaration.SuperTypes.Any(x => TypesUtil.IsClassType(x)))
            {
                declaration.SetSuperClass(observableObject);
            }
        }

        return true;

    }
}