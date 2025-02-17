using System;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.Analysis;

[ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[] {typeof(Dotnet9RequiredWarning)})]
public class Dotnet9PartialPropertiesAnalyzer : ElementProblemAnalyzer<IClassLikeDeclaration>
{
    protected override void Run(IClassLikeDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (PluginUtil.GetObservableObject(element) is { } observable && element.DeclaredElement?.IsDescendantOf(observable.GetTypeElement()) == true)
        {
            if (element.GetCSharpProjectConfiguration() is { } configuration)
            {
                if (configuration is {LanguageVersion: CSharpLanguageVersion.Preview, TargetFrameworkId: { Version.Major: > 9 } target} && (target.IsNetCore || target.IsNetCoreApp) && observable.Assembly?.Version >=  new Version(8,4))
                {
                    consumer.AddHighlighting(new Dotnet9RequiredWarning(element, element.GetNameDocumentRange()));
                }
            }
        }
    }
}