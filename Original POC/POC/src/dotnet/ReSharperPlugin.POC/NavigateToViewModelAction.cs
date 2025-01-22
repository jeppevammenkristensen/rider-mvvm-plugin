using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.Navigation.NavigationExtensions;
using JetBrains.ReSharper.Feature.Services.Xaml.Bulbs;
using JetBrains.ReSharper.Intentions.Xaml.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace ReSharperPlugin.POC;

[ContextAction(
    Name = "Open Corresponding ViewModel",
    Description = "Navigate to the ViewModel associated with this XAML file.",
    GroupType = typeof(XamlContextActions) )]
public class NavigateToViewModelAction : ContextActionBase
{
    
    private readonly XamlContextActionDataProvider _provider;

    public NavigateToViewModelAction(XamlContextActionDataProvider  provider)
    {
        _provider = provider;
    }

    public override string Text => "Open Corresponding ViewModel";

    public override bool IsAvailable(IUserDataHolder cache)
    {
        if (_provider.GetSelectedTreeNode<IXamlFile>() is { } node)
        {
            return node.GetTypeDeclarations()
                .Any(x => XmlTagExtensions.GetAttributes(x).Any(y => y.XmlName == XamlConstants.DatatypeName));
        }

        return false;
    }

    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        var node = _provider.GetSelectedTreeNode<IXamlFile>();
        if (node == null)
            return null;
        var xamlTypeDeclaration = node.GetTypeDeclarations().FirstOrDefault();
        if (xamlTypeDeclaration?.GetAttributes().ToList() is { } xmlAttributes)
        {
            var dataType = xmlAttributes.FirstOrDefault(x => x.XmlName == XamlConstants.DatatypeName)?.Value?.UnquotedValue;
            var split = dataType.Split(':', StringSplitOptions.None);
            var first = split.FirstOrEmpty.ToString();
            var second = split.LastOrEmpty.ToString();

            var nameSpaceAttribute = xmlAttributes.OfType<NamespaceAliasAttribute>().FirstOrDefault(x => x.DeclaredName == first);
            var nameSpace = nameSpaceAttribute.UnquotedValue.Split(':', StringSplitOptions.None).LastOrEmpty;

            var csharpFileInProject = node.GetProject()
                .GetAllProjectFiles(x => x.LanguageType.Name == CSharpProjectFileType.Name)
                .Select(x => x.GetPrimaryPsiFile())
                .OfType<ICSharpFile>()
                .Select(x => x.GetTypeTreeNodeByNamespaceAndShortName(nameSpace.ToString(), second))
                .WhereNotNull().FirstOrDefault();

            if (csharpFileInProject is not null)
            {
                csharpFileInProject.NavigateToTreeNode(true);
            }
        }



        return null;
    }
    
    private static async Task ShowProjectFile([NotNull] ISolution solution, [NotNull] IProjectFile file, int? caretPosition)
    {
        var editor = solution.GetComponent<IEditorManager>();
        var textControl = await editor.OpenProjectFileAsync(file, OpenFileOptions.DefaultActivate);

        if (caretPosition != null)
        {
            textControl?.Caret.MoveTo(caretPosition.Value, CaretVisualPlacement.DontScrollIfVisible);
        }
    }
}