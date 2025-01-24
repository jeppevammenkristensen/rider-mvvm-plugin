using System.Collections.Generic;
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

    // public static bool InheritsFrom(this ITypeElement type, IDeclaredType baseType, HashSet<string>? testedTypes = null)
    // {
    //     testedTypes ??= new HashSet<string>();
    //     var baseTypes = type.GetSuperTypeElements();
    //
    //     while (baseTypes is {Count: > 0})
    //     {
    //         foreach (var baseTypeElement in baseTypes)
    //         {
    //             // if the type has already been tested we continue (just to ensure
    //             // that we don't create a stack overflow
    //             if (!testedTypes.Add(baseTypeElement.GetClrName().FullName))
    //                 continue;
    //
    //             if (baseTypeElement.IsValid() && baseTypeElement.Equals(baseType))
    //                 return true;
    //
    //             if (InheritsFrom(baseTypeElement, baseType, testedTypes))
    //                 return true;
    //         }
    //     }
    //
    //     return false;
    // }
}