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

/// <summary>
/// Analyzer that emits <see cref="Dotnet9RequiredWarning"/> for MVVM classes when the project is configured
/// in a way that suggests usage of features available with newer .NET and C# (e.g., partial/required members).
///
/// Conditions checked:
/// - The current element is (or derives from) CommunityToolkit.Mvvm's ObservableObject (via <see cref="PluginUtil"/>).
/// - The project is using C# Preview language version.
/// - The target framework major version is greater than 9 (e.g., .NET 10+), and it is a .NET (Core/App) target.
/// - The referenced CommunityToolkit.Mvvm assembly version is at least 8.4.
///
/// When all conditions are met, the analyzer produces a highlighting on the class name to inform the user about the
/// .NET 9 related requirement/behavior within this context.
/// </summary>
[ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[] {typeof(Dotnet9RequiredWarning)})]
public class Dotnet9PartialPropertiesAnalyzer : ElementProblemAnalyzer<IClassLikeDeclaration>
{
    /// <summary>
    /// Runs the analyzer over class-like declarations and adds a <see cref="Dotnet9RequiredWarning"/>
    /// when the project and dependencies match the criteria described above.
    /// </summary>
    protected override void Run(IClassLikeDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        // Resolve ObservableObject from CommunityToolkit.Mvvm and ensure the current class derives from it.
        if (PluginUtil.GetObservableObject(element) is { } observable && element.DeclaredElement?.IsDescendantOf(observable.GetTypeElement()) == true)
        {
            // Obtain the C# project configuration (language version, target framework, etc.).
            if (element.GetCSharpProjectConfiguration() is { } configuration)
            {
                // Check: C# Preview language, .NET target with major version > 9 (e.g., .NET 10+), and Toolkit >= 8.4.
                if (configuration is {LanguageVersion: CSharpLanguageVersion.Preview, TargetFrameworkId: { Version.Major: < 9 } target} && (target.IsNetCore || target.IsNetCoreApp) && observable.Assembly?.Version >=  new Version(8,4))
                {
                    // Produce the highlighting on the class name to warn about .NET 9 related requirements.
                    consumer.AddHighlighting(new Dotnet9RequiredWarning(element, element.GetNameDocumentRange()));
                }
            }
        }
    }
}