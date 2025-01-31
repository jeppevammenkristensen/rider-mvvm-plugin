using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.MvvmPlugin.Extensions;

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