using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xaml.Tree.MarkupExtensions;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.ContextActions;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;
using IFieldDeclaration = JetBrains.ReSharper.Psi.CSharp.Tree.IFieldDeclaration;


public static class PropertyExtensions
{
    public static void ConvertToAutoProperty(this IPropertyDeclaration propertyDeclaration, CSharpElementFactory factory)
    {
        RemoveField(propertyDeclaration);
        
        var existingGetter = propertyDeclaration.GetAccessorDeclaration(AccessorKind.GETTER);
        var newGetter = factory.CreateAccessorDeclaration("get;");
        if (existingGetter is not null) 
        {
            ModificationUtil.ReplaceChild(existingGetter, newGetter);    
        }
        else
        {
            propertyDeclaration.AddAccessorDeclarationAfter(newGetter, null);
        }
                        
        var existingSetter = propertyDeclaration.GetAccessorDeclaration(AccessorKind.SETTER);
        var newSetter = factory.CreateAccessorDeclaration("set;");
                        
        if (existingSetter is not null)
        {
            ModificationUtil.ReplaceChild(existingSetter, newSetter);
        }
        else
        {
            propertyDeclaration.AddAccessorDeclarationAfter(newSetter, null);
        }
    }

    public static void RemoveField(this IPropertyDeclaration property)
    {
        IPropertyBodyHelper? propertyBodyService =
            LanguageManager.Instance.TryGetService<IPropertyBodyHelper>(property.DeclaredElement!
                .PresentationLanguage);

        if (propertyBodyService?.GetBackingField(property.DeclaredElement!) is {} field)
        {
            if (field.GetSingleDeclaration()?.Parent is { } fieldDeclaration)
            {
                ModificationUtil.DeleteChild(fieldDeclaration);
            }
        }
        else
        {
            var accessor = property.GetAccessorDeclaration(AccessorKind.SETTER);
            if (accessor is null)
            {
                return;
            }
            var fieldSearcher = new FieldSearcher();
            accessor.ProcessDescendants(fieldSearcher);
            if (fieldSearcher.FieldDeclaration is { } fieldDeclaration)
            {
                ModificationUtil.DeleteChild(fieldDeclaration);
            }
            
        }
    }

    private class FieldSearcher : IRecursiveElementProcessor
    {
        public IMultipleFieldDeclaration? FieldDeclaration { get; private set; }
        
        public FieldSearcher()
        {
            
        }

        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            return true;
        }

        public void ProcessBeforeInterior(ITreeNode element)
        {
            if (element is IAssignmentExpression assignment)
            {
                HandleAssignment(assignment);
            }
        }

        private void HandleAssignment(IAssignmentExpression assignment)
        {
            if (assignment.Dest is IReferenceExpression referenceExpression && referenceExpression.TryGetReferencedNode<IFieldDeclaration>() is { } fieldDeclaration)
            {
                if (fieldDeclaration.Parent is IMultipleFieldDeclaration field)
                {
                    FieldDeclaration = field;
                    ProcessingIsFinished = true;
                }
            }
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
            
        }

        public bool ProcessingIsFinished { get; private set; }
    }
}


public static class XamlExtensions
{
    public static ICSharpFile? GetPrimaryNonSourceGeneratedFile(this ITypeElement? typeElement)
    {
        return typeElement?.GetSourceFiles()
            .FirstOrDefault(x => x.IsValid() && !x.IsSourceGeneratedFile())?.GetPrimaryPsiFile() as ICSharpFile;
    }

    public static (IType? type, SupportedXamlPlatform platform) GetViewModelType(this IXamlFile xamlFile)
    {
        var platform = XamlPlatformWrapper.CreateFromTreeNode(xamlFile);
        switch (platform.SupportedPlatformEnum)
        {
            case SupportedXamlPlatform.WPF:
                return (GetViewModelTypeFromDesignDataContext(xamlFile), platform);
            case SupportedXamlPlatform.MAUI:
            case SupportedXamlPlatform.AVALONIA:
                return (GetViewModelTypeFromDataType(xamlFile), platform);
            case SupportedXamlPlatform.WINUI:
                return (GetViewModelTypeFromWinUI(xamlFile), platform);
            
            default:
                return (null, platform);
        }
    }

    private static IType? GetViewModelTypeFromWinUI(IXamlFile xamlFile)
    {
        var match = xamlFile.GetSourceFile().ToProjectFile()?.GetDependentFiles()
            .Where(x => x.LanguageType.Is<CSharpProjectFileType>())
            .Select(x => x.ToSourceFile()?.GetTheOnlyPsiFile<CSharpLanguage>() as ICSharpFile)
            .FirstNotNull();

        if (match is { })
        {
            if (match.Descendants<IPropertyDeclaration>().ToEnumerable()
                    .Where(x => x.NameIdentifier.Name == "ViewModel").FirstNotNull() is { } property)
            {
                return property.Type;
            }
        }

        return null;
    }

    private static IType? GetViewModelTypeFromDesignDataContext(IXamlFile? file)
    {
        if (file?.GetTypeDeclarations().SingleItem is not { } typeDeclaration)
            return null;
        if (typeDeclaration.GetDesignDataContextAttribute() is { Value.MarkupExtension: { } markup })
        {
            return markup.GetDesignDataContextType();
        }
        return null;
    }

    public static IPropertyAttribute? GetDesignDataContextAttribute(this IXamlTypeDeclaration? type)
    {
        return type?.GetAttribute(x => x is IPropertyAttribute p && p.IsDesignTimeDataContextSetter()) as IPropertyAttribute;
    }

    private static IType? GetViewModelTypeFromDataType(IXamlFile xamlFile)
    {
        if (xamlFile.GetDataTypePropertyAttribute() is
            { Value.MarkupAttributeValue: ITypeExpression markupAttributeValue })
        {
            return ReferenceUtil.GetType(markupAttributeValue);
        }
        return null;
    }

    public static IPropertyAttribute? GetDataTypePropertyAttribute(this IXamlFile xamlFile)
    {
        return xamlFile.GetTypeDeclarations().FirstOrDefault().GetDataTypePropertyAttribute();
    }

    public static IPropertyAttribute? GetDataTypePropertyAttribute(this IXamlTypeDeclaration? type)
    {
        if (type?.GetAttribute("x:DataType") is IPropertyAttribute propertyAttribute)
        {
            return propertyAttribute;
        }

        return null;
    }

    public static DesktopKind GetDesktopKind(this IXamlTypeDeclaration? rootType)
    {
        if (rootType?.NamespaceAliases.FirstOrDefault(x => x.XmlName == "xmlns")?.UnquotedValue is { } xmlns)
        {
            if (xmlns == "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            {
                return DesktopKind.Wpf;
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
}