using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CSharp.PostfixTemplates;
using JetBrains.ReSharper.Feature.Services.CSharp.PostfixTemplates.Behaviors;
using JetBrains.ReSharper.Feature.Services.CSharp.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.PostfixTemplates;
using JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Threading;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.PostFixTemplates;

public abstract class ObservableIntroduceMemberTemplateBase : CSharpPostfixTemplate
{
    public override PostfixTemplateInfo? TryCreateInfo(CSharpPostfixTemplateContext context)
    {
        ICSharpDeclaration ownerDeclaration = ReturnStatementUtil.FindReturnOwnerDeclaration(context.Reference); if (ownerDeclaration == null)
            return null;
      
        if (PluginUtil.GetObservableObject(ownerDeclaration).ShouldBeKnown() is not {}  declaredType)
        {
            return null;
        }
      
        if (!ownerDeclaration.CommunityToolkitCanHandleSourceGenerators(declaredType))
        {
            return null;
        }

      
      
        bool notValid;
        switch (ownerDeclaration.GetContainingNode<IClassLikeDeclaration>())
        {
            case null:
            case IInterfaceDeclaration _:
                notValid = true;
                break;
            default:
                notValid = false;
                break;
        }
        if (notValid)
            return null;
        foreach (CSharpPostfixExpressionContext expressionContext in context.Expressions)
        {
            if (!expressionContext.Type.IsUnknown && expressionContext.CanBecameStatement)
            {
                if (CSharpPostfixUtils.IsAssignmentLike(expressionContext))
                    return null;
                if (expressionContext.Expression is IReferenceExpression expression2 && !expression2.IsQualified)
                {
                    bool isMember;
                    switch (expressionContext.ReferencedElement)
                    {
                        case null:
                        case IField _:
                        case IProperty _:
                            isMember = true;
                            break;
                        default:
                            isMember = false;
                            break;
                    }
                    if (isMember)
                        continue;
                }
                return new IntroduceMemberPostfixTemplateInfo(TemplateName, expressionContext, expressionContext.Type, ownerDeclaration.CommunityToolkitCanHandlePartialProperties(declaredType), ownerDeclaration.DeclaredElement is IConstructor);
            }
        }
        return null;
    }

    [NotNull]
    public abstract string TemplateName { get; }

    public override sealed PostfixTemplateBehavior CreateBehavior(PostfixTemplateInfo info)
    {
        return CreateBehavior((ObservableIntroduceMemberTemplateBase.IntroduceMemberPostfixTemplateInfo) info);
    }

    [NotNull]
    protected abstract PostfixTemplateBehavior CreateBehavior(
        [NotNull] IntroduceMemberPostfixTemplateInfo info);

    protected class IntroduceMemberPostfixTemplateInfo : PostfixTemplateInfo
    {
        [NotNull]
        public IType ExpressionType { get; }

        public bool UsePartial { get; }
        public bool IsStatic { get; }
      
      

        public IntroduceMemberPostfixTemplateInfo(
            [NotNull] string text,
            [NotNull] PostfixExpressionContext expression,
            [NotNull] IType expressionType,
            bool usePartial,
            bool inConstructor)
            : base(text, expression, availableInPreciseMode: inConstructor)
        {
            ExpressionType = expressionType;
            UsePartial = usePartial;
            IsStatic = expression.Expression.IsInStaticContext();
        }
    }

    protected abstract class IntroduceMemberBehaviorBase : 
        CSharpStatementPostfixTemplateBehavior<IExpressionStatement>
    {
        [NotNull]
        protected readonly IType ExpressionType;
        protected readonly bool IsStatic;
        [NotNull]
        private IReadOnlyList<string> myMemberNames = EmptyList<string>.Instance;
        [CanBeNull]
        private ITreeNodePointer<IClassMemberDeclaration> myMemberPointer;

        private string _hello;

        protected IntroduceMemberBehaviorBase(
            [NotNull] ObservableIntroduceMemberTemplateBase.IntroduceMemberPostfixTemplateInfo info)
            : base(info)
        {
            ExpressionType = info.ExpressionType;
            IsStatic = info.IsStatic;
            UsePartial = info.UsePartial;
        }

        /// <summary>
        /// When this is set to true we generate a partial property and decorate it with
        /// the observable property. Otherwise it's a field
        /// </summary>
        public bool UsePartial { get; set; }

        protected override IExpressionStatement CreateStatement(
            CSharpElementFactory factory,
            ICSharpExpression expression)
        {
            IExpressionStatement statement = (IExpressionStatement) factory.CreateStatement("__ = expression;");
            IClassMemberDeclaration memberDeclaration = CreateMemberDeclaration(factory);
            ((IAssignmentExpression) statement.Expression).SetSource(expression);
            NameSuggestionManager suggestion = expression.GetPsiServices().Naming.Suggestion;
            IClassLikeDeclaration classLikeDeclaration = expression.GetContainingNode<IClassLikeDeclaration>().NotNull<IClassLikeDeclaration>("expression.GetContainingNode<IClassLikeDeclaration>()");
            PsiLanguageType language = expression.Language;
            ICSharpExpression node = expression;
            INamesCollection emptyCollection = suggestion.CreateEmptyCollection(PluralityKinds.Unknown, language, true, node);
            emptyCollection.Add(expression, new EntryOptions
            {
                SubrootPolicy = SubrootPolicy.Decompose,
                PredefinedPrefixPolicy = PredefinedPrefixPolicy.Remove
            });
            INamesSuggestion namesSuggestion = emptyCollection.Prepare(memberDeclaration.DeclaredElement, new SuggestionOptions
            {
                UniqueNameContext = (ITreeNode) classLikeDeclaration.Body ?? classLikeDeclaration
            });
        
            // The name is set to the first suggested name
        
            memberDeclaration.SetName(namesSuggestion.FirstName());

            // In this scenario we convert for instance _someField to SomeField and change the
            // Name so it's SomeField = "SomeValue"
            if (!this.UsePartial && statement.Expression is IAssignmentExpression assignmentExpression)
            {
                if (assignmentExpression.Dest is IReferenceExpression referenceExpression)
                {
                    referenceExpression.SetName(memberDeclaration.DeclaredName.ToPropertyName());  
                }
            }
       
        
            myMemberNames = namesSuggestion.AllNames();
        
            // The generated member is inserted after the matched anchor
        
            ICSharpTypeMemberDeclaration anchorMember = GetAnchorMember(classLikeDeclaration.MemberDeclarations.ToList());
            myMemberPointer = classLikeDeclaration.AddClassMemberDeclarationAfter<IClassMemberDeclaration>(memberDeclaration, (IClassMemberDeclaration) anchorMember).CreateTreeElementPointer<IClassMemberDeclaration>();
            return statement;
        }

        [CanBeNull]
        protected abstract ICSharpTypeMemberDeclaration GetAnchorMember(
            [NotNull] IList<ICSharpTypeMemberDeclaration> members);

        [NotNull]
        protected abstract IClassMemberDeclaration CreateMemberDeclaration(
            [NotNull] CSharpElementFactory factory);

        protected override void AfterComplete(
            ITextControl textControl,
            IExpressionStatement statement,
            Suffix suffix)
        {
            IClassMemberDeclaration treeNode = myMemberPointer?.GetTreeNode();
            if (treeNode == null)
                return;

            // If we generate a partial. We will generate a hotspot that will allow the
            // user the change the name of the property (and it will point to both the property and
            // the property name assignment) so both will change when editing
            if (UsePartial)
            {
                HotspotInfo hotspotInfo = new HotspotInfo(templateField: new TemplateField("memberName",
                        new NameSuggestionsExpression(myMemberNames),
                        0), documentRanges:
                    [
                        ((IReferenceExpression) ((IAssignmentExpression) statement.Expression).Dest).NameIdentifier
                        .GetDocumentRange(), // The assignment expression identifier
                        treeNode.GetNameDocumentRange() // The property declaration
                    ]);
                // The editing is performed on the assignment identifier
                DocumentOffset documentEndOffset =  statement.GetDocumentEndOffset();
                Info.ExecutionContext.LiveTemplatesManager.CreateHotspotSessionAtopExistingText(statement.GetSolution(), documentEndOffset, textControl, LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo).ExecuteAndForget();  
            }
            // If a field is generated the hotspot will only be generated for the field and not the assignment
            // NOTE: I haven't found out how to update the assignment identifier to a correct property name
            else
            {
                var treeNodeRange = treeNode.GetNameDocumentRange();
                var dest = (IReferenceExpression) ((IAssignmentExpression) statement.Expression).Dest;

                HotspotInfo hotspotInfo = new HotspotInfo(templateField: new TemplateField("memberName",
                        new NameSuggestionsExpression(myMemberNames),
                        0), documentRanges:
                    [
                        treeNodeRange
                    ]);
                DocumentOffset documentEndOffset =  treeNode.GetDocumentEndOffset();
                var session = Info.ExecutionContext.LiveTemplatesManager.CreateHotspotSessionAtopExistingText(treeNode.GetSolution(), documentEndOffset, textControl, LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo);
                session.ExecuteAsync().NoAwait();

            }
        }
    }
}