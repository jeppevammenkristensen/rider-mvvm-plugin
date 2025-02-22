using System;
using System.Linq;
using System.Text;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Threading;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

[ContextAction(
    Name = "Add observable property (CommunityToolkit)",
    Description =
        "Create a property in the given class declaration and decorate it with the ObservableProperty attribute.",
    GroupType = typeof(CSharpContextActions))]
public class CreateObservablePropertyContextAction(ICSharpContextActionDataProvider provider) : ContextActionBase
{
    /// <summary>
    /// <see cref="ExecutePsiTransaction"/>
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        if (provider.GetSelectedTreeNode<IClassLikeDeclaration>() is not { } classLikeDeclaration)
            return null;

        var psiServices = classLikeDeclaration.GetPsiServices();
        var suggestionManager = psiServices.Naming.Suggestion;

        var project = classLikeDeclaration.GetProject();

        if (PluginUtil.GetObservableObject(classLikeDeclaration).ShouldBeKnown() is { } observableObject &&
            PluginUtil.GetObservablePropertyAttribute(classLikeDeclaration).ShouldBeKnown() is { } observableProperty)
        {
            using (WriteLockCookie.Create())
            {
                // This will ensure that the containing class is partial and if possible
                // inherits from ObservableObject
                if (!classLikeDeclaration.EnsurePartialAndInheritsObservableObject(observableObject,
                        supressObservableObjectNotFound: false))
                    return null;

                // Get the factory we will use to generate a field
                var factory = CSharpElementFactory.GetInstance(provider.GetSelectedTreeNode<ICSharpFile>()!);

                if (CanUsePartialProperties)
                {
                    var propertyDeclaration = factory.CreateObservableProperty();

                    classLikeDeclaration.AddClassMemberDeclarationAfter(propertyDeclaration,
                        classLikeDeclaration.MemberDeclarations.OfType<IPropertyDeclaration>().LastOrDefault());

                    
                    

                    propertyDeclaration = classLikeDeclaration.MemberDeclarations.OfType<IPropertyDeclaration>()
                        .First(x => x.DeclaredName == PluginConstants.PlaceHolderName);

                    var propertyIdentifier = propertyDeclaration.NameIdentifier;

                    var typeExpression = new ConstantMacroDef().ToMacroCall().WithConstant("string");
                    
                    var type = new HotspotInfo(new TemplateField("type", typeExpression, 0),
                        propertyDeclaration.TypeUsage.GetDocumentRange());

                    var propertyName = new SuggestVariableNameMacroDef().ToMacroCall();

                    var propertyNameHotspot = new HotspotInfo(
                        new TemplateField("propertyName", propertyName, 0),
                        propertyIdentifier.GetDocumentRange());
                    
                    var liveTemplatesManager = solution.GetComponent<LiveTemplatesManager>();

                    var endCaret = propertyDeclaration.GetDocumentEndOffset();

                    return control =>
                    {
                        var session = liveTemplatesManager.CreateHotspotSessionAtopExistingText(
                            solution, endCaret, control,
                            LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, type, propertyNameHotspot);
                        session.ExecuteAsync().NoAwait();
                    };
                }
                else
                {
                    var field = BuildField(factory, PluginUtil.GetObservablePropertyAttribute(classLikeDeclaration));
                    classLikeDeclaration.AddClassMemberDeclarationAfter(field,
                        classLikeDeclaration.MemberDeclarations.OfType<IFieldDeclaration>().LastOrDefault());
                    
                    field = classLikeDeclaration.MemberDeclarations.OfType<IFieldDeclaration>().First(x => x.DeclaredName == PluginConstants.PlaceHolderName);

                    // We add to hotspots which are used to present a way for the user to edit the type and the name of the field
                    var type = new HotspotInfo(new TemplateField("type", 0),
                        field.TypeUsage.GetDocumentRange());
                   
                    
                    var suggestNameExpression = new MacroCallExpressionNew(new SuggestVariableNameMacroDef());
                    
                    var propertyNameHotspot = new HotspotInfo(
                        new TemplateField("propertyName", suggestNameExpression, 0),
                        field.NameIdentifier.GetDocumentRange());
                   
                    var liveTemplatesManager = solution.GetComponent<LiveTemplatesManager>();

                    return control =>
                    {
                        var session = liveTemplatesManager.CreateHotspotSessionAtopExistingText(
                            solution,  DocumentRange.InvalidRange, control,
                            LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, type, propertyNameHotspot);

                        session.ExecuteAsync().NoAwait();
                    };
                }

            }
        }

        return null;
    }

    private IFieldDeclaration BuildField(CSharpElementFactory factory, IDeclaredType observableProperty)
    {
        var builder = new StringBuilder();

        // $0 ObservableProperty reference
        // $1 Name

        builder.AppendLine($"[$0]");
        builder.AppendLine($"private TYPE $1;");

        var field = (IFieldDeclaration) factory.CreateTypeMemberDeclaration(builder.ToString(), observableProperty,
            PluginConstants.PlaceHolderName);
        
        return field;
    }

    public override string Text => "Create observable property (CommunityToolkit)";

    public override bool IsAvailable(IUserDataHolder cache)

    {
        if (provider.GetSelectedTreeNode<IFieldDeclaration>() is { } fieldDeclaration)
        {
            // check if the cursor is at the end of the declaration
            var caretOffset = provider.DocumentSelection.TextRange.StartOffset;

            if (caretOffset < fieldDeclaration.GetDocumentEndOffset().Offset)
                return false;
        }

        if (provider.GetSelectedTreeNode<IPropertyDeclaration>() is { } propertyDeclaration)
        {
            // check if the cursor is at the end of the declaration
            var caretOffset = provider.DocumentSelection.TextRange.StartOffset;
            
            if (caretOffset < propertyDeclaration.GetDocumentEndOffset().Offset)
                return false;
        }
        
        
        if (provider.GetSelectedTreeNode<IClassLikeDeclaration>() is not { } classLikeDeclaration)
            return false;

        if (PluginUtil.GetObservablePropertyAttribute(classLikeDeclaration).ShouldBeKnown() is not
            { } observableProperty)
            return false;

        if (!classLikeDeclaration.CommunityToolkitCanHandleSourceGenerators(observableProperty))
            return false;

        CanUsePartialProperties = classLikeDeclaration.CommunityToolkitCanHandlePartialProperties(observableProperty);

        return true;

    }

    private bool CanUsePartialProperties { get; set; }
}