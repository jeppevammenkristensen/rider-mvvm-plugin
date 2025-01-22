using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace ReSharperPlugin.POC;

[ContextAction(
    Name = nameof(NavigateToViewContextAction),
    Description = "Navigate to ViewModel", Priority = -10)]
public class NavigateToViewContextAction : ContextActionBase
{
    private readonly ICSharpContextActionDataProvider _provider;

    public NavigateToViewContextAction(ICSharpContextActionDataProvider provider)
    {
        _provider = provider;
    }

    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (MatchedProjectFile is null)
            return null;

        ShowProjectFile(solution, MatchedProjectFile, null).GetAwaiter().GetResult();
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
                             x => x.LanguageType.Name == "XAML")
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
                    // yield return new ContextNavigation($"ViewModel {allProjectFile.Name}", null,
                    //     NavigationActionGroup.Other,
                    //     () =>
                    //     {
                    //         var popupContext = node.GetSolution().GetComponent<IMainWindowPopupWindowContext>()
                    //             .Source;
                    //         allProjectFile.Navigate(popupContext, true);}//     });
                }
            }
        }

        return false;
    }

    
    
    private bool XamlFileMatchesType(IXamlFile xamlFile, string typeNamespace, string modelName)
    {
        if (xamlFile.GetTypeDeclarations().FirstOrDefault() is { } type)
        {
            if (type.GetAttribute(x => x.XmlName == "DataType") is { } dataType)
            {
                if (dataType.UnquotedValue.Split(':') is {Length: 2} splitData)
                {
                    (var nameSpace, var shortName) = (splitData[0], splitData[1]);
                    if (type.NamespaceAliases.FirstOrDefault(x => x.DeclaredName == nameSpace) is { } nameSpaceAlias)
                    {
                        if (nameSpaceAlias.DeclaredName == typeNamespace)
                        {
                            return modelName == shortName;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static async Task ShowProjectFile([NotNull] ISolution solution, [NotNull] IProjectFile file,
        int? caretPosition)
    {
        var editor = solution.GetComponent<IEditorManager>();
        var textControl = await editor.OpenProjectFileAsync(file, OpenFileOptions.DefaultActivate);

        if (caretPosition != null)
        {
            textControl?.Caret.MoveTo(caretPosition.Value, CaretVisualPlacement.DontScrollIfVisible);
        }
    }

    [CanBeNull] public IProjectFile MatchedProjectFile { get; set; }
}