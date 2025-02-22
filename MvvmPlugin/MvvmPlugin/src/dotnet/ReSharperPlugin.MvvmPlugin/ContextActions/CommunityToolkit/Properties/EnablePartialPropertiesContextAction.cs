using System;
using JetBrains.Application.Progress;
using JetBrains.DocumentManagers.PropertyModifiers;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

[ContextAction(
    Name = "Enable partial properties (CommunityToolkit)",
    Description =
        "If the nuget version is 8.4 or higher of the CommunityToolkit.Mvvm package. This will ensure that that language version is preview. NOTE. This will not change the language version of the project, as it is required that it is at least dotnet 9.0",
    GroupType = typeof(CSharpContextActions))]
public class EnablePartialPropertiesContextAction(ICSharpContextActionDataProvider provider) : ContextActionBase
{
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        var project = provider.SourceFile.GetProject();

        if (project?.ProjectProperties?.TryGetConfiguration<CSharpProjectConfiguration>(
                project.GetCurrentTargetFrameworkId()) is { } configuration)
        {
            return _ =>
            {
                // This seems a bit hacky (But hey it works. :) )
                
                // Retrieve the version modifer
                var item = solution.GetComponent<CSharpLanguageVersionModifier>();
                // This will update the csproj file and set the language version to Preview
                item.Modify(CSharpLanguageVersion.Preview, project);
                // This will update the PSI cache version
                configuration.LanguageVersion = CSharpLanguageVersion.Preview;
                
                // This will invalidate the PSI cache for all files in the project
                PsiFileCachedDataUtil.InvalidateInAllProjectFiles(project,CSharpLanguage.Instance, CSharpPsiFileCachedDataKeys.LANGUAGE_LEVEL);

            };  
        }
        

        return null;

    }

    public override string Text => "Enable partial properties (CommunityToolkit)";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        if (provider.Project?.ProjectProperties.TryGetConfiguration<CSharpProjectConfiguration>(
              provider.Project.GetCurrentTargetFrameworkId()) is { } configuration && provider.GetSelectedTreeNode<ITreeNode>() is {} treeNode && PluginUtil.GetObservableObject(treeNode) is {} observable)
        {
            // If a version of the CommunityToolkit is 8.4 or larger and the language version is not Preview we return true
            if (observable is { Assembly.Version: {} version} && observable.Assembly.Version >= new Version(8, 4) && configuration.LanguageVersion < CSharpLanguageVersion.Preview)
            {
                return true;
            }
        }

        return false;
    }
}