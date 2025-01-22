using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers.impl;
using JetBrains.DocumentManagers.Transactions;
using JetBrains.IDE;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Feature.Services.Xaml.Bulbs;
using JetBrains.ReSharper.Intentions.Xaml.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions;

[ContextAction(
    Name = "Create viewmodel",
    Description = "Creates a viewmodel for the selected XAML file.",
    GroupType = typeof(XamlContextActions))]
public class CreateViewModelAction : ContextActionBase
{
    
    private static Regex _matchViewRegex = new Regex("View$", RegexOptions.IgnoreCase);
    private readonly XamlContextActionDataProvider _provider;
    private readonly Lifetime _lifetime;

    public CreateViewModelAction(XamlContextActionDataProvider provider)
    {
        _provider = provider;
    }
    
    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        // currently only Avalonia is supported
        if (Kind != DesktopKind.Avalonia)
            return null; 
        
        // Get the selected XAML file
        var xamlFile = _provider.GetSelectedTreeNode<IXamlFile>();
        if (xamlFile == null)
            return null;

        // Get the name of the xaml file. For instance SomeView.xaml would become SomeView 
        var name = xamlFile.GetSourceFile().GetLocation().NameWithoutExtension;
       
        // If the view ends with View that part
        var viewName = $"{_matchViewRegex.Replace(name, string.Empty)}ViewModel";

        var project = xamlFile.GetProject();

        // Check or create a 'ViewModels' subdirectory
        //var viewModelsFolder = project.Location.Combine("ViewModels");

        // // Define the ViewModel file path
        // var viewModelFilePath = viewModelsFolder.Combine($"{viewName}.cs");

        using (ReadLockCookie.Create())
        {
            using (var cookie =
                   solution.CreateTransactionCookie(DefaultAction.Rollback, this.Text, NullProgressIndicator.Create()))
            {
                // Try to locate a ViewModelsFolder
                var viewModelsFolder = project.GetSubFolders()
                    .FirstOrDefault(x => x.Location.Name == "ViewModels");
                IProjectFolder projectFolder;

                // Note: This logic needs to be refined so in the case where a view is nested
                // For instance Views\SomeFolder\Someview.xaml the SomeFolder will be added
                // and in cases where the view is not in a view folder it will either find the toplevel folder
                // for instance UserControls\SomeView. But that will be along the way. It's to easy to over complicate
                // things so early. Especially with the current experience level with Rider development
                
                // If none is matched we set the projectFolder to that one
                if (viewModelsFolder == null)
                {
                    var newFolder = project.Location.Combine("ViewModels");
                    projectFolder = project.GetOrCreateProjectFolder(newFolder);
                }
                // Otherwise set the project folder to the matched value
                else
                {
                     projectFolder = project.GetOrCreateProjectFolder(viewModelsFolder.Location);
                }

                var filePath = projectFolder.NotNull().Location.Combine($"{viewName}.cs");
                
                if (filePath.ExistsFile)
                {
                    // Currently we exit. But we should look into updating the xaml
                    return null;
                }
                
                // Create the new csharp file
                var newFile = AddNewItemHelper.AddFile(projectFolder, $"{viewName}.cs").ToSourceFile();
                
                int? caretPosition;
                using (PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(newFile.GetPsiServices(),
                           "CreateViewModelClass"))
                {
                    var csharpFile = newFile.GetDominantPsiFile<CSharpLanguage>() as ICSharpFile;
                    if (csharpFile == null)
                        return null;

                    var elementFactory = CSharpElementFactory.GetInstance(csharpFile);
                    
                    // Check how to add the namespace
                    bool isFileScoped = CSharpNamespaceUtil.CanAddFileScopedNamespaceDeclaration(csharpFile);
                    
                    // Find the namespace to use for the generated file
                    var projectFile = newFile.ToProjectFile();
                    var nspath = projectFile.GetParentFoldersPresentable().Reverse().Select(x => x.Name).AggregateString(".");
                    
                    var namespaceDeclaration = elementFactory.CreateNamespaceDeclaration(nspath,isFileScoped);
                    var addedNs = csharpFile.AddNamespaceDeclarationAfter(namespaceDeclaration, null);

                    // Generate the empty class
                    var classLikeDeclaration =
                        (IClassLikeDeclaration) elementFactory.CreateTypeMemberDeclaration("public class $0 {}",
                            viewName);
                    var addedTypeDeclaration =
                        addedNs.AddTypeDeclarationAfter(classLikeDeclaration, null) as IClassDeclaration;

                    // Get the caret position inside the body of the class
                    caretPosition = addedTypeDeclaration?.Body?.GetDocumentRange().TextRange.StartOffset + 1;

                    if (Kind == DesktopKind.Avalonia)
                    {
                        // Set the DataType to point to the newly created viewModel
                        
                        var xamlFactory = XamlElementFactory.GetInstance(xamlFile, true);
                        // Get the type declaration (will be for instance the root <Window> or <UserControl>
                        var xamlTypeDeclaration = xamlFile.GetTypeDeclarations().First();

                        // Create a namespace ViewModel that point to the namespace
                        // Note: Things will die if viewModel is already present. Should be refined later on
                        var namespaceAlias = xamlFactory.CreateNamespaceAlias("viewModel", $"clr-namespace:{nspath}");
                    
                        // Add the newly created alias after the last namespacealias attribute
                        xamlTypeDeclaration.AddAttributeAfter(namespaceAlias,
                            xamlTypeDeclaration.GetAttributes()
                                .OfType<NamespaceAliasAttribute>()
                                .LastOrDefault());
                        
                        // Create the DataType attribute and add it
                        var dataTypeAttribute = xamlFactory.CreateRootAttribute($"x:DataType=\"viewModel:{viewName}\"");
                        xamlTypeDeclaration.AddAttributeAfter(dataTypeAttribute, xamlTypeDeclaration.GetAttributes()
                            .OfType<NamespaceAliasAttribute>()
                            .LastOrDefault() );

                        // Try to format it (not working as I would want right now)
                        var codeFormatter = ModificationUtil.GetCodeFormatter(xamlFile.Language.LanguageService());
                        codeFormatter.Format(xamlFile, CodeFormatProfile.SOFT);
                    }
                }
                // Commit the changes
                cookie.Commit(NullProgressIndicator.Create());
                
                // Go to the newly created file
                ShowProjectFile(solution, newFile.ToProjectFile().NotNull(),caretPosition).GetAwaiter().GetResult();
            }
            
           
        }
        
       
        
        return null;
    }

    public override string Text => "Create viewmodel";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        if (_provider.GetSelectedTreeNode<IXamlFile>() is { } node)
        {
            var project = node.GetProject().ProjectProperties.GetActiveConfigurations<CSharpProjectConfiguration>().ToList();
            
            if (project.SafeGetProjectProperty("UseWPF").Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                Kind = DesktopKind.Wpf;
            }
            else
            {
                Kind = DesktopKind.Avalonia;
            }
            
            return !node.GetTypeDeclarations()
                .Any(x => x.GetAttributes().Any(y => y.XmlName == XamlConstants.DatatypeName));
        }

        return false;
    }
    
    private DesktopKind Kind { get; set; } = DesktopKind.None;
    
    private async Task  ShowProjectFile([NotNull] ISolution solution, [NotNull] IProjectFile file, int? caretPosition)
    {
        var editor = solution.GetComponent<IEditorManager>();
        
        var textControl = await editor.OpenProjectFileAsync(file, OpenFileOptions.DefaultActivate);
        
        if (caretPosition != null)
        {
            textControl?.Caret.MoveTo(caretPosition.Value, CaretVisualPlacement.DontScrollIfVisible);
        }    
        

        
    }
}

public enum DesktopKind
{
    None,
    Wpf,
    Avalonia
}