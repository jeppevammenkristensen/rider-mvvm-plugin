using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.Settings;
using JetBrains.IDE;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using ReSharperPlugin.MvvmPlugin.Models;
using ReSharperPlugin.MvvmPlugin.Options;

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
    
    public static async Task ShowProjectFile(ISolution solution, IProjectFile file,
        int? caretPosition)
    {
        var editor = solution.GetComponent<IEditorManager>();
        var textControl = await editor.OpenProjectFileAsync(file, OpenFileOptions.DefaultActivate);

        if (caretPosition != null)
        {
            textControl?.Caret.MoveTo(caretPosition.Value, CaretVisualPlacement.DontScrollIfVisible);
        }
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

        return communityToolkitType.Assembly.Version >= new Version(8, 0, 0) 
               && treeNode.GetCSharpProjectConfiguration() is { LanguageVersion:>= CSharpLanguageVersion.CSharp8 };
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
               config.IsPartialPropertyFriendlyTargetFramework();

    }

    public static bool IsPartialPropertyFriendlyTargetFramework(this CSharpProjectConfiguration? configuration)
    {
        if (configuration?.TargetFrameworkId is not {} target)
            return false;

        if (target.IsNetStandard || target.IsNetCoreApp || target.IsNetCore)
        {
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Creates an public partial property that is decorated with the ObservableProperty attribute
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="propertyName"></param>
    /// <param name="propertyType"></param>
    /// <returns></returns>
    public static IPropertyDeclaration CreateObservableProperty(this CSharpElementFactory factory, string? propertyName = null, IType? propertyType = null, bool generateObservableAttribute = true)
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

        // This is here for the scenario where we want to copy an existing range of attributes
        // for instance from a field to a property
        if (generateObservableAttribute)
        {
            propertyDeclaration.DecorateWithObservablePropertyAttribute(factory);    
        }
        
              
        return propertyDeclaration;  
    }

    public static bool IsDotnetMajorOrHigher(this CSharpProjectConfiguration? configuration, int majorVersion)
    {
        if (configuration == null)
            return false;
        
        return configuration.TargetFrameworkId is {} target && target.Version.Major >= majorVersion && (target.IsNetCore || target.IsNetCoreApp);
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
        var observableProperty = PluginUtil.GetObservablePropertyAttribute(typeDeclaration);
        
        var before = typeDeclaration.AddAttributeBefore(factory.CreateAttribute(observableProperty!.GetTypeElement()!), null);
        before.AddLineBreakAfter();
    }

    public static void DecorateWithRelayPropertyAttribute(this ICSharpTypeMemberDeclaration item, string? canExecuteName,
        CSharpElementFactory factory)
    {
       var relayAttribute = TypeConstants.RelayCommandAttribute;
       var attr = factory.CreateAttribute(relayAttribute.GetDeclaredType(item).GetTypeElement()!);
       if (!string.IsNullOrWhiteSpace(canExecuteName))
       {
           attr.AddPropertyAssignmentAfter(factory.CreatePropertyAssignment("CanExecute", factory.CreateExpression($"nameof({canExecuteName})")), null);
       }
       
       item.AddAttributeBefore(attr, null);

    }

    /// <summary>
    ///  Gets the property name for the declaration. If it's a field decorated with the
    /// ObservableProperty attribute the generated name will be returned
    /// </summary>
    /// <param name="declaration"></param>
    /// <returns></returns>
    public static string? GetPropertyName(this IClassMemberDeclaration declaration)
    {
        if (declaration is IPropertyDeclaration propertyDeclaration)
        {
            return propertyDeclaration.NameIdentifier.Name;
        }

        if (declaration is IFieldDeclaration fieldDeclaration)
        {
            if (fieldDeclaration.DeclaredElement is { } declaredElement)
            {
                var observablePropertyTypeName = TypeConstants.ObservableProperty.GetClrName();

                if (declaredElement.HasAttributeInstance(observablePropertyTypeName,
                        false))
                {
                    return fieldDeclaration.NameIdentifier.Name.ToPropertyName();
                }
            }
        }

        return null;
    }

    public static bool IsRelayCommand(this IType type)
    {
        if (type is IDeclaredType declaredType)
        {
            ClrTypeNameWrapper[] types =
            [
                TypeConstants.RelayCommand, TypeConstants.RelayCommand.GenericOneType(),
                TypeConstants.AsyncRelayCommand, TypeConstants.AsyncRelayCommand.GenericOneType()
            ];
            
            if (types.Any(x => x.GetClrName().Equals(declaredType.GetClrName()))) 
            {
                return true;
            }
        }

        return false;

    }

    public static bool IsValidObservableObject(this ICSharpContextActionDataProvider provider, IClassLikeDeclaration classLikeDeclaration, IDeclaredType? observableObject)
    {
        observableObject ??= PluginUtil.GetObservableObject(classLikeDeclaration).ShouldBeKnown();
        if (observableObject is null)
        {
            return false;
        }

        if (classLikeDeclaration.DeclaredElement?.IsDescendantOf(observableObject.GetTypeElement()) == true)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ensures partial and that the declaration inherits from ObservableObject
    /// If the ObservableObject could not be found false will be retured
    /// </summary>
    /// <param name="classLikeDeclaration"></param>
    /// <param name="observableObject">If null the type will be loaded when this method is called</param>
    /// <param name="supressObservableObjectNotFound">If the type is not found and this value is true. The type will still be used</param>
    /// <returns></returns>
    public static bool EnsurePartialAndInheritsObservableObject(this ICSharpTypeDeclaration? classLikeDeclaration, IDeclaredType? observableObject, bool supressObservableObjectNotFound)
    {
        if (classLikeDeclaration is not IClassDeclaration declaration)
            return false;
        
        observableObject ??= PluginUtil.GetObservableObject(declaration); //.ShouldBeKnown();

        var setting = classLikeDeclaration.GetProject()?.GetSolution().GetSettingsStore();
          var observableObjectValue = MvvmPluginSettingsRetriever.GetObservableObjectValue(setting);
         
         var type = observableObjectValue switch
         {
             ObservableObjectBaseType.Object => TypeConstants.ObservableObject,
             ObservableObjectBaseType.Validator => TypeConstants.ObservableValidator,
             ObservableObjectBaseType.Recipient => TypeConstants.ObservableRecipient,
             ObservableObjectBaseType.Other => GetCustomViewModel(classLikeDeclaration, setting),
             _ => throw new ArgumentOutOfRangeException()
         };
        // var type = new ClrTypeNameWrapper(observableObjectValue!).GetDeclaredType(classLikeDeclaration);

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
                declaration.SetSuperClass(type.GetDeclaredType(classLikeDeclaration));
            }
        }

        return true;

    }

    private static ClrTypeNameWrapper GetCustomViewModel(ICSharpTypeDeclaration classLikeDeclaration,
        IContextBoundSettingsStore? setting)
    {
        var fallbackValue = TypeConstants.ObservableObject;
        
        var otherValues = MvvmPluginSettingsRetriever.GetOtherValuesAsHashSet(setting);
        if (otherValues is { Count:<= 0})
            return fallbackValue;

        if (classLikeDeclaration.GetProject()?.GetPsiModules()?.FirstOrDefault() is not { } module)
        {
            return fallbackValue;
        }

        if (classLikeDeclaration.GetPsiServices().Symbols.GetSymbolScope(module, true, false) is not { } scope)
            return fallbackValue;
        
         return scope
            .GetPossibleInheritors("ObservableObject")
            .OfType<Class>()
            .Where(x => otherValues.Contains(x.ShortName))
            .Select(x => new ClrTypeNameWrapper(x.GetClrName().FullName))
            .FirstOrDefault() ?? fallbackValue;
    }

    public static bool ImplementsObservableObject(this IClassLikeDeclaration declaration,
        IDeclaredType? observableObject)
    {
        observableObject ??= TypeConstants.ObservableObject.GetDeclaredTypeOrNull(declaration);
        if (observableObject is null)
        {
            return false;
        }

        if (declaration.DeclaredElement?.IsDescendantOf(observableObject.GetTypeElement()) == true)
        {
            return true;
        }

        return false;
    }
}