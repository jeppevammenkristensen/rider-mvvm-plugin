using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.Progress;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Navigation.NavigationExtensions;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Logging;
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

        MatchedProjectFile.ToSourceFile().Navigate(new TextRange(0), true);
        return null;
    }

    public override string Text { get; } = "Navigate to View";

    public override bool IsAvailable(IUserDataHolder cache)
    {
        this.MatchedProjectFile = null;

        if (_provider.GetSelectedTreeNode<ICSharpTypeDeclaration>() is not { } typeDeclaration)
            return false;

        XamlPlatformWrapper wrapper = XamlPlatformUtil.GetXamlNodePlatform(typeDeclaration);
        if (wrapper.IsUnSupportedPlatform())
            return false;

        bool isWinUI = wrapper.SupportedPlatformEnum == SupportedXamlPlatform.WINUI;
        
        var psiServices = typeDeclaration.GetPsiServices();
        var consumer = new SearchResultsConsumer();

        try
        {
            psiServices.SingleThreadedFinder.FindReferences(
                typeDeclaration.DeclaredElement,
                domain: SearchDomainFactory.Instance.CreateSearchDomain(typeDeclaration.GetProject().GetAllProjectFiles().Select(x => x.ToSourceFile())),
                consumer: consumer,
                NullProgressIndicator.Create());
        }
        catch (Exception ex)
        {
            Logger.LogException("Failed to find references",ex);
            return false;
        }
            
        foreach (var result in consumer.GetOccurrences())
        {
            if (result is ReferenceOccurrence occurrence)
            {
                if (occurrence.SourceFile?.LanguageType.Name == PluginConstants.Xaml)
                {
                    this.MatchedProjectFile = occurrence.SourceFile.ToProjectFile();
                    return true;
                }

                if (isWinUI)
                {
                    if (occurrence.Kinds.Any(x => x.Name == "Property declaration") &&
                        occurrence.SourceFile?.ToProjectFile()?.GetDependsUponFile() is
                            {LanguageType.Name: PluginConstants.Xaml} dependsUponFile)
                    {
                        this.MatchedProjectFile = dependsUponFile;
                        return true;
                    }
                }
            }
            
            
        }

        return false;
    }
    
    private static bool XamlFileMatchesType(IXamlFile xamlFile, string typeNamespace, string modelName)
    {
        
        var (type, _) = xamlFile.GetViewModelType();
        if (type is null)
            return false;

        if (type.GetTypeElement() is { } typeElement &&
            typeElement.GetContainingNamespace().QualifiedName == typeNamespace && typeElement.ShortName == modelName)
        {
            return true;
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