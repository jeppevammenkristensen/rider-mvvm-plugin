using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Feature.Services.Navigation.NavigationExtensions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Tree;

namespace ReSharperPlugin.POC;

[ContextNavigationProvider]
public class NavigateToViewProvider : INavigateFromHereProvider
{
    public IEnumerable<ContextNavigation> CreateWorkflow(IDataContext dataContext)
    {
        var node = dataContext.GetSelectedTreeNode<ITreeNode>();
        var typeDeclaration = node?.GetParentOfType<IClassDeclaration>();

        if (typeDeclaration?.NameIdentifier.Name.IndexOf("ViewModel", StringComparison.OrdinalIgnoreCase) > -1)
        {
            foreach (var allProjectFile in node.GetProject().GetAllProjectFiles(
                         x => x.LanguageType.Name == "XAML").Distinct())
            {
                if (allProjectFile.GetPrimaryPsiFile() is IXamlFile xamlFile)

                    if (xamlFile.Descendants<IXamlAttribute>().ToEnumerable()
                            .FirstOrDefault(x => x.XmlName == "DataType") is { } dataType)
                    {
                        if (dataType.Value?.UnquotedValue.EndsWith(typeDeclaration.NameIdentifier.Name,
                                StringComparison.OrdinalIgnoreCase) == true)
                        {
                            yield return new ContextNavigation($"ViewModel {allProjectFile.Name}", null,
                                NavigationActionGroup.Other,
                                () =>
                                {
                                    var popupContext = node.GetSolution().GetComponent<IMainWindowPopupWindowContext>()
                                        .Source;
                                    allProjectFile.Navigate(popupContext, true);
                                });
                        }
                    }
            }
        }
    }
}