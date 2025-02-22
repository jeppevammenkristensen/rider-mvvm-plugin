using System.Collections.Generic;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Settings;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using Microsoft.Build.Evaluation;

namespace ReSharperPlugin.MvvmPlugin.Extensions;

public static class Extensions
{
    public static MacroCallExpressionNew ToMacroCall<T>(this T macro) where T : IMacroDefinition
    {
        return new MacroCallExpressionNew(macro);
    }

    public static MacroCallExpressionNew WithParameter(this MacroCallExpressionNew macro, IMacroParameterValue value)
    {
        macro.AddParameter(value);
        return macro;
    }

    public static MacroCallExpressionNew WithConstant(this MacroCallExpressionNew macro, string value)
    {
        return macro.WithParameter(new ConstantMacroParameter(value));
    }
    
    public static string SafeGetProjectProperty(this IEnumerable<ICSharpProjectConfiguration> configuration, string key,
        string defaultValue = "")
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

    public static bool DoesNotHaveAttribute(this IAttributesOwnerDeclaration item, IDeclaredType attribute)
    {
        // If the field declaration has no Attributes we return true
        if (!item.Attributes.Any())
        {
            return true;
        }

        if (item.DeclaredElement is IAttributesSet attributesSet)
        {
            return !attributesSet.HasAttributeInstance(attribute.GetClrName(),
                false);    
        }

        return true;
    }
}