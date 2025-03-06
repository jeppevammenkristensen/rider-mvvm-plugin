using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.Progress;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xaml.Tree.MarkupExtensions;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.TextControl;
using JetBrains.Threading;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.Navigation;

[ContextAction(
    Name = nameof(NavigateToViewContextAction),
    Description = "Navigate to View", Priority = -10)]
public class NavigateToViewContextAction : ContextActionBase
{
    private readonly ICSharpContextActionDataProvider _provider;

    public NavigateToViewContextAction(ICSharpContextActionDataProvider provider)
    {
        _provider = provider;
    }

    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (MatchedProjectFile is null)
            return null;

        ShowProjectFile(solution, MatchedProjectFile, null).NoAwait();
        return null;
    }

    public override string Text { get; } = "Go to View";

    public override bool IsAvailable(IUserDataHolder cache)
    {
        this.MatchedProjectFile = null;

        if (_provider.GetSelectedTreeNode<ICSharpTypeDeclaration>() is not { } typeDeclaration)
            return false;

        if (typeDeclaration.NameIdentifier.Name.IndexOf("ViewModel", StringComparison.OrdinalIgnoreCase) > -1)
        {
            var modelName = typeDeclaration.DeclaredName;
            var typeNamespace = typeDeclaration.GetContainingNamespaceDeclaration()?.DeclaredName;
            
            foreach ((IProjectFile projectFile, IFile file) in typeDeclaration.GetProject().GetAllProjectFiles(
                             x => x.LanguageType.Name == PluginConstants.Xaml)
                         .Distinct()
                         .Select(x => (x, x.GetPrimaryPsiFile()))
                         .Where(x => x.Item2 is IXamlFile))
            {
                if (file is not IXamlFile xamlFile)
                {
                    continue;
                }

                if (XamlFileMatchesType(xamlFile, typeNamespace, modelName))
                {
                    MatchedProjectFile = projectFile;
                    return true;
                }
            }
        }

        return false;
    }
    
    private static bool XamlFileMatchesType(IXamlFile xamlFile, string typeNamespace, string modelName)
    {
        if (xamlFile.GetTypeDeclarations().FirstOrDefault() is { } type)
        {
            var desktopKind = xamlFile.GetDesktopKind();

            if (desktopKind == DesktopKind.Wpf)
            {
                if (type.GetAttribute(x => x is IPropertyAttribute p && p.IsDesignTimeDataContextSetter()) is
                    IPropertyAttribute {Value.MarkupExtension : { } markup})
                {
                    var matchedType = markup.GetDesignDataContextType();
                    if (matchedType.GetTypeElement() is { } typeElement)
                    {
                        return typeElement.GetContainingNamespace().QualifiedName == typeNamespace && typeElement.ShortName == modelName;
                    }
                }
            }
            else if (desktopKind == DesktopKind.Avalonia)
            {
                if (type.GetAttribute(x => x.XmlName == "DataType") is IPropertyAttribute {  Value.MarkupAttributeValue: ITypeExpression markupAttributeValue })
                {
                    if (markupAttributeValue.TypeName?.XmlName == modelName)
                    {
                        var derivedType = ReferenceUtil.GetType(markupAttributeValue);
                        if (derivedType.GetTypeElement() is { } typeElement)
                        {
                            return typeElement.GetContainingNamespace().QualifiedName == typeNamespace;    
                        }
                        
                    }
                 
                    
                    // if (dataType.UnquotedValue.Split(':') is {Length: 2} splitData)
                    // {
                    //     var (nameSpace, shortName) = (splitData[0], splitData[1]);
                    //     if (type.NamespaceAliases.FirstOrDefault(x => x.DeclaredName == nameSpace) is { } nameSpaceAlias)
                    //     {
                    //         if (nameSpaceAlias.UnquotedValue.Split(':') is {Length: 2} splitAlias)
                    //         {
                    //             return splitAlias[1] == typeNamespace && modelName == shortName;
                    //         }
                    //     }
                    // }
                }
            }

            
        }

        return false;
    }

    private static async Task ShowProjectFile(ISolution solution, IProjectFile file,
        int? caretPosition)
    {
        var editor = solution.GetComponent<IEditorManager>();
        var textControl = await editor.OpenProjectFileAsync(file, OpenFileOptions.DefaultActivate);

        
        if (caretPosition != null)
        {
            textControl?.Caret.MoveTo(caretPosition.Value, CaretVisualPlacement.DontScrollIfVisible);
        }
    }

    protected IProjectFile? MatchedProjectFile { get; set; }
}