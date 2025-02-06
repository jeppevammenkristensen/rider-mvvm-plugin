using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.Extensions;


public static class ContextActionUtil
{
    public static CSharpProjectConfiguration? GetCSharpProjectConfiguration(this ITreeNode treeNode)
    {
        if (treeNode.GetProject() is { } project &&
            project.ProjectProperties.ActiveConfigurations.Configurations.OfType<CSharpProjectConfiguration>()
                .FirstOrDefault() is { } configuration)
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
    
    public static bool CommunityToolkitCanHandlePartialProperties(this ITreeNode treeNode,
        IDeclaredType? communityToolkitType)
    {
        communityToolkitType ??= PluginUtil.GetObservableObject(treeNode).ShouldBeKnown();
        if (communityToolkitType is null or not {Assembly.Version: {}})
        {
            return false;
        }
        
        return communityToolkitType.Assembly?.Version >= new Version(8,4) && treeNode.GetCSharpProjectConfiguration()?.LanguageVersion == CSharpLanguageVersion.Preview;
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
            if (!declaration.SuperTypes.Any(x => x.IsClassType()))
            {
                declaration.SetSuperClass(observableObject);
            }
        }

        return true;

    }
}

public static class Extensions
{
    public static string SafeGetProjectProperty(this IEnumerable<ICSharpProjectConfiguration> configuration, string key, string defaultValue = "")
    {
        foreach (var config in configuration)
        {
            if (config.PropertiesCollection.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        
        return defaultValue;
    }

    public static string ToFieldName(this string propertyName)
    {
        if (propertyName.Length == 0 || propertyName[0] == '_')
        return propertyName;

        return string.Concat($"_{char.ToLower(propertyName[0])}{propertyName.Substring(1)}");
    }

    public static string ToPropertyName(this string fieldName)
    {
        if (fieldName.Length == 0 || char.IsUpper(fieldName[0]))
        return fieldName;

        if (fieldName[0] == '_')
        {
            fieldName = fieldName.Substring(1);
        }
        
        if (fieldName.Length == 0)
        return string.Empty;
        
        if (fieldName.Length == 1)
        return char.ToUpper(fieldName[0]).ToString();
        
        return string.Concat(char.ToUpper(fieldName[0]), fieldName.Substring(1));
        
        
    }
}