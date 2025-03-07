using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Navigation.NavigationExtensions;
using JetBrains.ReSharper.Feature.Services.Xaml.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.Navigation;

[ContextAction(Name = nameof(NavigateToViewModelFromXamlView), Description = "Navigate to the ViewModel")]
public class NavigateToViewModelFromXamlView(XamlContextActionDataProvider provider) : ContextActionBase

{
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (ViewModelType is { } viewModelType)
        {
            // ISymbolScope symbolScope = provider.PsiServices.Symbols.GetSymbolScope(module: provider.PsiModule, withReferences: true, caseSensitive: true);
            // var type = symbolScope.GetTypeElementByCLRName(viewModelType.GetTypeElement()!.GetClrName())var singleOrDefaultSourceFile = viewModelType.

            var typeElement = viewModelType.GetTypeElement();
            if (typeElement is null)
                return null;

            if (typeElement.GetPrimaryNonSourceGeneratedFile() is not { } csharpfile)
            {
                return null;
            }
            var clrTypeName = typeElement.GetClrName();

            var classLikeDeclaration = csharpfile.Descendants<IClassLikeDeclaration>().Collect().FirstOrDefault(x => x.DeclaredElement?.GetClrName().Equals(clrTypeName) == true);
            csharpfile.GetSourceFile().Navigate(classLikeDeclaration?.Body.GetDocumentRange().TextRange ?? new TextRange(0), true);
        }
    
        return null;
    }

    public override string Text => "Navigate to ViewModel";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        if (provider.GetSelectedTreeNode<IXamlFile>() is not { } xamlFile)
            return false;
        
        if (xamlFile.GetViewModelType() is { type: { } viewModelType })
        {
            ViewModelType = viewModelType;
            return true;
        }
        
        return false;

    }

    public IType? ViewModelType { get; set; }
}