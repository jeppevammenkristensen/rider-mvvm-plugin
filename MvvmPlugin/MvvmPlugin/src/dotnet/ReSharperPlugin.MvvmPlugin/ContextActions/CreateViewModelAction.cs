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
using JetBrains.ReSharper.Feature.Services.Intentions.Impl.LanguageSpecific.Finders;
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
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
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

    public CreateViewModelAction(XamlContextActionDataProvider provider)
    {
        _provider = provider;
    }
    
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        // // currently only Avalonia is supported
        // if (Kind != DesktopKind.Avalonia)
        //     return null; 
        
        // Get the selected XAML file
        var xamlFile = _provider.GetSelectedTreeNode<IXamlFile>();
        if (xamlFile == null)
            return null;

        // Get the name of the xaml file. For instance SomeView.xaml would become SomeView 
        var name = xamlFile.GetSourceFile().GetLocation().NameWithoutExtension;
       
        // If the view ends with View that part
        var viewName = $"{_matchViewRegex.Replace(name, string.Empty)}ViewModel";

        var project = xamlFile.GetProject();

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
                bool fileExist = filePath.ExistsFile;

                IPsiSourceFile? newFile;
                if (fileExist)
                {
                    //var symbolsService = xamlFile.GetPsiServices().Symbols;
                    
                    // Load the existing file as a CSharp source file
                    var matchedFile = projectFolder.GetSubFiles()
                        .FirstOrDefault(sf => sf.Name.Equals($"{viewName}.cs", StringComparison.OrdinalIgnoreCase));
                    
                    if (matchedFile == null)
                        throw new InvalidOperationException("Failed to retrieve the existing C# file.");

                    newFile = matchedFile.ToSourceFile();
                }
                else
                {
                    // Create the new csharp file
                    newFile = AddNewItemHelper.AddFile(projectFolder, $"{viewName}.cs").ToSourceFile();
                }
                
               
                
                int? caretPosition;
                using (PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(newFile.GetPsiServices(),
                           "CreateViewModelClass"))
                {
                    var csharpFile = newFile.GetDominantPsiFile<CSharpLanguage>() as ICSharpFile;
                    if (csharpFile == null)
                        return null;

                    var firstClassLikeMatch = csharpFile.Descendants<IClassLikeDeclaration>().Collect().FirstOrDefault();

                    string? namespaceName = null;

                    if (firstClassLikeMatch == null)
                    {

                        var elementFactory = CSharpElementFactory.GetInstance(csharpFile);

                        // Check how to add the namespace
                        bool isFileScoped = CSharpNamespaceUtil.CanAddFileScopedNamespaceDeclaration(csharpFile);

                        // Find the namespace to use for the generated file
                        var projectFile = newFile.ToProjectFile();
                        var nspath = projectFile.GetParentFoldersPresentable().Reverse().Select(x => x.Name)
                            .AggregateString(".");

                        var namespaceDeclaration = elementFactory.CreateNamespaceDeclaration(nspath, isFileScoped);
                        var addedNs = csharpFile.AddNamespaceDeclarationAfter(namespaceDeclaration, null);
                        namespaceName = namespaceDeclaration.DeclaredName;

                        // Generate the empty class
                        firstClassLikeMatch =
                            (IClassLikeDeclaration) elementFactory.CreateTypeMemberDeclaration("public class $0 {}",
                                viewName);
                        var addedTypeDeclaration =
                            addedNs.AddTypeDeclarationAfter(firstClassLikeMatch, null) as IClassDeclaration;

                        caretPosition = addedTypeDeclaration?.Body?.GetDocumentRange().TextRange.StartOffset + 1;
                    }
                    else
                    {
                        namespaceName = firstClassLikeMatch.GetContainingNamespaceDeclaration()?.DeclaredName ?? throw new InvalidOperationException("Can't locate namespace for existing item");
                        caretPosition = firstClassLikeMatch.Body?.GetDocumentRange().TextRange.StartOffset + 1;
                    }

                    GenerateXamlViewModelAttributes(xamlFile, namespaceName, viewName);
                }
                // Commit the changes
                cookie.Commit(NullProgressIndicator.Create());
                
                // Go to the newly created file. But only in Rider as this will fail in visual studio
#if RIDER
                ShowProjectFile(solution, newFile.ToProjectFile().NotNull(),caretPosition).GetAwaiter().GetResult();
#endif
                
            }
            
           
        }
        
       
        
        return null;
    }

    private void GenerateXamlViewModelAttributes(IXamlFile xamlFile, string nspath, string viewName)
    {
        var xamlFactory = XamlElementFactory.GetInstance(xamlFile, true);
        // Get the type declaration (will be for instance the root <Window> or <UserControl>
        var xamlTypeDeclaration = xamlFile.GetTypeDeclarations().First();

        INamespaceAlias? namespaceAlias = null;
        
        if (xamlTypeDeclaration.NamespaceAliases
            .FirstOrDefault(x => x.UnquotedValue == $"clr-namespace:{nspath}") is {} aliasMatch)
        {
            namespaceAlias = aliasMatch;
        }
        else
        {
            // Create a namespace ViewModel that point to the namespace
            // Note: Things will die if viewModel is already present. Should be refined later on
            namespaceAlias = xamlFactory.CreateNamespaceAlias("viewModel", $"clr-namespace:{nspath}");
            
             // Add the newly created alias after the last namespacealias attribute
                    xamlTypeDeclaration.AddAttributeAfter(namespaceAlias,
                        xamlTypeDeclaration.GetAttributes()
                            .OfType<NamespaceAliasAttribute>()
                            .LastOrDefault());
        }
              
        // Based on the technology we set the datacontext differently
        // For avalonia it is set through the DataType attribute and for wpf we set
        // the design context
        
        if (Kind == DesktopKind.Avalonia)
        {
            // Set the DataType to point to the newly created viewModel
                        
            // Create the DataType attribute and add it
            var dataTypeAttribute = xamlFactory.CreateRootAttribute($"x:DataType=\"{namespaceAlias.XmlName}:{viewName}\"");
            xamlTypeDeclaration.AddAttributeAfter(dataTypeAttribute, xamlTypeDeclaration.GetAttributes()
                .OfType<NamespaceAliasAttribute>()
                .LastOrDefault() );

            // Try to format it (not working as I would want right now)
            var codeFormatter = ModificationUtil.GetCodeFormatter(xamlFile.Language.LanguageService());
            codeFormatter.Format(xamlFile, CodeFormatProfile.STRICT);
        }
        else if (Kind == DesktopKind.Wpf)
        {
            // d:DataContext="{d:DesignInstance Type=Something, IsDesignTimeCreatable=False}"
            var dataTypeAttribute = xamlFactory.CreateRootAttribute(
                $$"""d:DataContext="{d:DesignInstance Type=viewModel:{{viewName}}, IsDesignTimeCreatable=False}" """);
            xamlTypeDeclaration.AddAttributeAfter(dataTypeAttribute, xamlTypeDeclaration.GetAttributes()
                .OfType<NamespaceAliasAttribute>()
                .LastOrDefault() );
        }
    }

    public override string Text => "Create viewmodel";
    public override bool IsAvailable(IUserDataHolder cache)
    {
        if (_provider.GetSelectedTreeNode<IXamlFile>() is { } node)
        {
            var rootType = node.GetTypeDeclarations().FirstOrDefault();
            if (rootType == null)
                return false;

            if (rootType.Type.GetTypeElement()?.ShortName == "Application")
                return false;

            if (rootType.NamespaceAliases.FirstOrDefault(x => x.XmlName == "xmlns")?.UnquotedValue is { } xmlns)
            {
                // if xmlns matches the wpf spec check if the d:DataContext attribute is set
                // if not set the createViewModel action is available
                
                if (xmlns == "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
                {
                    Kind = DesktopKind.Wpf;
                    return !rootType.GetAttributes()
                        .Any(x => x.XmlName == XamlConstants.DataContextName && x.XmlNamespace == "d");
                }
                if (xmlns == "https://github.com/avaloniaui")
                {
                    Kind = DesktopKind.Avalonia;
                    return rootType.GetAttributes().All(x => x.XmlName != XamlConstants.DatatypeName);
                }
                
            }
            
            
            
            
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