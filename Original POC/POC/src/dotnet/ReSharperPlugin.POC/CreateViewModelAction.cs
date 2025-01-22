using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.DocumentManagers.impl;
using JetBrains.DocumentManagers.Transactions;
using JetBrains.IDE;
using JetBrains.ProjectModel;
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
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.POC;

[ContextAction(
    Name = "Create viewmodel",
    Description = "Creates a viewmodel for the selected XAML file.",
    GroupType = typeof(XamlContextActions))]
public class CreateViewModelAction : ContextActionBase
{
    
    private static Regex _regex = new Regex("View", RegexOptions.IgnoreCase);
    private readonly XamlContextActionDataProvider _provider;

    public CreateViewModelAction(XamlContextActionDataProvider provider)
    {
        _provider = provider;
    }
    
    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        var xamlFile = _provider.GetSelectedTreeNode<IXamlFile>();
        if (xamlFile == null)
            return null;

        var name = xamlFile.GetSourceFile().GetLocation().NameWithoutExtension;
       
        var viewName = $"{_regex.Replace(name, "")}ViewModel";

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
                var viewModelsFolder = project.GetSubFolders()
                    .FirstOrDefault(x => x.Location.Name == "ViewModels");
                IProjectFolder projectFolder;


                if (viewModelsFolder == null)
                {
                    var newFolder = project.Location.Combine("ViewModels");
                    projectFolder = project.GetOrCreateProjectFolder(newFolder);
                }
                else
                {
                     projectFolder = project.GetOrCreateProjectFolder(viewModelsFolder.Location);
                }

                var filePath = projectFolder.Location.Combine($"{viewName}.cs");
                if (filePath.ExistsFile)
                {
                    // Ensure that xaml is updated later
                    return null;
                }
                
                // Create the new csharp file
                var newFile = AddNewItemHelper.AddFile(projectFolder, $"{viewName}.cs").ToSourceFile();
                
                int? caretPosition;
                using (PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(newFile.GetPsiServices(),
                           "CreateTestClass"))
                {
                    var csharpFile = newFile.GetDominantPsiFile<CSharpLanguage>() as ICSharpFile;
                    if (csharpFile == null)
                        return null;

                    var elementFactory = CSharpElementFactory.GetInstance(csharpFile);
                    
                    // Add the namespace
                    bool isFileScoped = CSharpNamespaceUtil.CanAddFileScopedNamespaceDeclaration(csharpFile);
                    
                    // Needs to be adjusted so the correct namespace is used
                    
                    var projectFile = newFile.ToProjectFile();
                    var nspath = projectFile.GetParentFoldersPresentable().Reverse().Select(x => x.Name).AggregateString(".");
                    
                    var namespaceDeclaration = elementFactory.CreateNamespaceDeclaration(nspath,isFileScoped);
                    var addedNs = csharpFile.AddNamespaceDeclarationAfter(namespaceDeclaration, null);

                 

                    var classLikeDeclaration =
                        (IClassLikeDeclaration) elementFactory.CreateTypeMemberDeclaration("public class $0 {}",
                            viewName);
                    var addedTypeDeclaration =
                        addedNs.AddTypeDeclarationAfter(classLikeDeclaration, null) as IClassDeclaration;

                    caretPosition = addedTypeDeclaration?.Body?.GetDocumentRange().TextRange.StartOffset + 1;
                    var xamlFactory = XamlElementFactory.GetInstance(xamlFile, true);
                    var xamlTypeDeclaration = xamlFile.GetTypeDeclarations().First();

                    var namespaceAlias = xamlFactory.CreateNamespaceAlias("viewModel", $"clr-namespace:{nspath}");
                    
                    //xamlFactory.GetOrCreateNamespaceAlias(xamlTypeDeclaration,classLikeDeclaration.DeclaredElement, out var namespaceAlias);
                    
                    xamlTypeDeclaration.AddAttributeAfter(namespaceAlias,
                        xamlTypeDeclaration.GetAttributes()
                            .OfType<NamespaceAliasAttribute>()
                            .LastOrDefault());
                    var rootFactory = xamlFactory.CreateRootAttribute($"x:DataType=\"viewModel:{viewName}\"");
                    xamlTypeDeclaration.AddAttributeAfter(rootFactory, xamlTypeDeclaration.GetAttributes()
                            .OfType<NamespaceAliasAttribute>()
                            .LastOrDefault() );

                    var codeFormatter = ModificationUtil.GetCodeFormatter(xamlFile.Language.LanguageService());
                    codeFormatter.Format(xamlFile, CodeFormatProfile.SOFT);


                    
                }
                
                cookie.Commit(NullProgressIndicator.Create());
                
                
                
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
            return !node.GetTypeDeclarations()
                .Any(x => x.GetAttributes().Any(y => y.XmlName == XamlConstants.DatatypeName));
        }

        return false;
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