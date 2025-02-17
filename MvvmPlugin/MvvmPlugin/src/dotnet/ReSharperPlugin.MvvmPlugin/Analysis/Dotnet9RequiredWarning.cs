using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace ReSharperPlugin.MvvmPlugin.Analysis;

[StaticSeverityHighlighting(Severity.ERROR, typeof(HighlightingGroupIds.GutterMarks))]
public class Dotnet9RequiredWarning : IHighlighting
{
    private readonly IClassLikeDeclaration myClassDeclaration;
    private readonly DocumentRange myRange;

    public Dotnet9RequiredWarning(IClassLikeDeclaration classDeclaration, DocumentRange range)
    {
        myClassDeclaration = classDeclaration;
        myRange = range;
    }
    
    public bool IsValid()
    {
        return myClassDeclaration.IsValid();
    }

    public DocumentRange CalculateRange()
    {
        return myRange;
    }

    public string? ToolTip => "To support partial properties you must be using .NET 9 or higher";
    public string? ErrorStripeToolTip { get; }
}