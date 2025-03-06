using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Settings;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xaml.Tree.MarkupExtensions;
using JetBrains.ReSharper.Psi.Xml.Tree;
using Microsoft.Build.Evaluation;
using ReSharperPlugin.MvvmPlugin.ContextActions;
using ReSharperPlugin.MvvmPlugin.Models;

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

    public static bool MatchesNamespaceAndType(this IFile file, string namespaceName, string typeName)
    {
        if (file is not IXamlFile xamlFile)
            return false;
        
        return false;
        
        
    }
    //
    // public static IType? GetBoundDataContextType(this IXamlTypeDeclaration? rootType, DesktopKind? desktopKind)
    // {
    //     if (rootType == null)
    //         return null;
    //     
    //     if (desktopKind == DesktopKind.Wpf)
    //     {
    //         int i = 0;
    //     }
    //     else if (desktopKind == DesktopKind.Avalonia)
    //     {
    //         if (rootType.GetAttribute(x => x.XmlName == "DataType") is IPropertyAttribute {  Value.MarkupAttributeValue: ITypeExpression markupAttributeValue })
    //         {
    //             if (markupAttributeValue.TypeName?.XmlName == modelName)
    //             {
    //                 var derivedType = ReferenceUtil.GetType(markupAttributeValue);
    //                 if (derivedType.GetTypeElement() is { } typeElement)
    //                 {
    //                     return typeElement.GetContainingNamespace().QualifiedName == typeNamespace;    
    //                 }
    //                     
    //             }
    //              
    //                 
    //             // if (dataType.UnquotedValue.Split(':') is {Length: 2} splitData)
    //             // {
    //             //     var (nameSpace, shortName) = (splitData[0], splitData[1]);
    //             //     if (type.NamespaceAliases.FirstOrDefault(x => x.DeclaredName == nameSpace) is { } nameSpaceAlias)
    //             //     {
    //             //         if (nameSpaceAlias.UnquotedValue.Split(':') is {Length: 2} splitAlias)
    //             //         {
    //             //             return splitAlias[1] == typeNamespace && modelName == shortName;
    //             //         }
    //             //     }
    //             // }
    //         }
    //     }
    //
    //
    //     return ReferenceUtil.GetType(rootType);
    //     
    //     // if (desktopKind == DesktopKind.Wpf)
    //     // {
    //     //     var context = rootType.GetAttributes()
    //     //         .FirstOrDefault(x => x.XmlName == PluginConstants.DataContextName && x.XmlNamespace == "d");
    //     //     
    //     //     if (context?.Value is IPropertyAttributeValue { MarkupExtension: {} markup })
    //     //     {
    //     //         return markup.GetDesignDataContextType();
    //     //          
    //     //     }
    //     // }
    //     // else if (desktopKind == DesktopKind.Avalonia)
    //     // {
    //     //     var context = rootType.GetAttributes()
    //     //         .FirstOrDefault(x => x.XmlName == PluginConstants.DatatypeName);
    //     //     if (AvaloniaCompiledBindingsHelpers.IsAvaloniaCompiledBinding())
    //     // }
    //     
    //     return null;
    // }

    /// <summary>
    /// Determines the type of desktop platform (WPF or Avalonia) based on the XAML namespace in the provided root type declaration.
    /// </summary>
    /// <param name="rootType">The root type declaration of the XAML file, which may be null.</param>
    /// <returns>A value of the <see cref="DesktopKind"/> enum indicating the desktop platform type. Returns <see cref="DesktopKind.None"/> if the namespace does not match known platforms or if <paramref name="rootType"/> is null.</returns>
    public static DesktopKind GetDesktopKind(this IXamlTypeDeclaration? rootType)
    {
        if (rootType == null)
            return DesktopKind.None;
        
        if (rootType.NamespaceAliases.FirstOrDefault(x => x.XmlName == "xmlns")?.UnquotedValue is { } xmlns)
        {
            // if xmlns matches the wpf spec check if the d:DataContext attribute is set
            // if not set the createViewModel action is available
                
            if (xmlns == "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            {
                return DesktopKind.Wpf;
                // return !rootType.GetAttributes()
                //     .Any(x => x.XmlName == PluginConstants.DataContextName && x.XmlNamespace == "d");
            }
            if (xmlns == "https://github.com/avaloniaui")
            {
                return DesktopKind.Avalonia;
            }
        }
        
        return DesktopKind.None;
    }
    
    public static DesktopKind GetDesktopKind(this IXamlFile xamlFile)
    {
        return xamlFile.GetTypeDeclarations().FirstOrDefault().GetDesktopKind();
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

    public static bool IsObservableProperty(this IClassMemberDeclaration declaration)
    {
        if (!declaration.Attributes.Any())
            return false;
        
        if (declaration.DeclaredElement is IAttributesSet attributesSet)
        {
            return attributesSet.HasAttributeInstance(TypeConstants.ObservableProperty.GetClrName(),
                false);    
        }

        return true; 
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