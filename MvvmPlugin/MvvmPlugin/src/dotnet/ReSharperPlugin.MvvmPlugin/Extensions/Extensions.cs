using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.Extensions;


public static class ContextActionUtil
{
    /// <summary>
    /// Ensures partial and that the declaration inherits from ObservableObject
    /// If the ObservableObject could not be found false will be retured
    /// </summary>
    /// <param name="declaration"></param>
    /// <param name="observableObject">If null the type will be loaded when this method is called</param>
    /// <returns></returns>
    public static bool EnsurePartialAndInheritsObservableObject(this ICSharpTypeDeclaration? classLikeDeclaration, IDeclaredType? observableObject)
    {
        if (classLikeDeclaration is not IClassDeclaration declaration)
            return false;
        
        observableObject ??= PluginUtil.GetObservableObject(declaration).ShouldBeKnown();
        if (observableObject is null)
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

    public static string ToSnakeCase(this string propertyName)
    {
        if (propertyName.Length == 0 || propertyName[0] == '_')
        return propertyName;

        return string.Concat($"_{char.ToLower(propertyName[0])}{propertyName.Substring(1)}");
    }
}