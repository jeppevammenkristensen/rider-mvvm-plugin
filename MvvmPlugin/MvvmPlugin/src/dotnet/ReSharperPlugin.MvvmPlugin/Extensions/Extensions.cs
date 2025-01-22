using System.Collections.Generic;
using JetBrains.ProjectModel.Properties.CSharp;

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
}