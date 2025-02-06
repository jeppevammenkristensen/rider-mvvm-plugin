using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CSharp.PostfixTemplates;
using JetBrains.ReSharper.Feature.Services.CSharp.PostfixTemplates.Behaviors;
using JetBrains.ReSharper.Feature.Services.CSharp.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Feature.Services.CSharp.PostfixTemplates.Templates;
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
      ICSharpDeclaration ownerDeclaration = ReturnStatementUtil.FindReturnOwnerDeclaration(context.Reference);
      
      
      if (ownerDeclaration == null)
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
        memberDeclaration.SetName(namesSuggestion.FirstName());

        if (!this.UsePartial && statement.Expression is IAssignmentExpression assignmentExpression)
        {
          if (assignmentExpression.Dest is IReferenceExpression referenceExpression)
          {
              referenceExpression.SetName(memberDeclaration.DeclaredName.ToPropertyName());  
          }
          
        }
        
        int i = 0;
        
        myMemberNames = namesSuggestion.AllNames();
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

        if (UsePartial)
        {
          HotspotInfo hotspotInfo = new HotspotInfo(templateField: new TemplateField("memberName",
              new NameSuggestionsExpression(myMemberNames),
              0), documentRanges:
            [
              ((IReferenceExpression) ((IAssignmentExpression) statement.Expression).Dest).NameIdentifier
              .GetDocumentRange(),
              treeNode.GetNameDocumentRange()
            ]);
          DocumentOffset documentEndOffset =  statement.GetDocumentEndOffset();
          Info.ExecutionContext.LiveTemplatesManager.CreateHotspotSessionAtopExistingText(statement.GetSolution(), documentEndOffset, textControl, LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo).ExecuteAndForget();  
        }
        else
        {
          var treeNodeRange = treeNode.GetNameDocumentRange();
          var dest = (IReferenceExpression) ((IAssignmentExpression) statement.Expression).Dest;

          HotspotInfo hotspotInfo = new HotspotInfo(templateField: new TemplateField("memberName",
              new NameSuggestionsExpression(myMemberNames),
              0), documentRanges:
            [
              // ((IReferenceExpression) ((IAssignmentExpression) statement.Expression).Dest).NameIdentifier
              // .GetDocumentRange(),
              treeNodeRange
            ]);
          DocumentOffset documentEndOffset =  treeNode.GetDocumentEndOffset();
          var session = Info.ExecutionContext.LiveTemplatesManager.CreateHotspotSessionAtopExistingText(treeNode.GetSolution(), documentEndOffset, textControl, LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfo);
          session.ExecuteAsync().NoAwait();


        }
        

      }

      protected bool PromptForName { get; set; }
    }
  }



[PostfixTemplate("obsprop", "Generates a observable property", "Hello.obsprop")]
public class ToObservablePropertyPostFixTemplate : ObservableIntroduceMemberTemplateBase
{
    public override string TemplateName => "obsprop";
    
    protected override PostfixTemplateBehavior CreateBehavior(IntroduceMemberPostfixTemplateInfo info)
    {
        return new IntroduceObservableProperty(info);
    }

    private sealed class IntroduceObservableProperty(
        [NotNull] IntroduceMemberPostfixTemplateInfo info) : 
        IntroduceMemberBehaviorBase(info)
    {
        protected override IClassMemberDeclaration CreateMemberDeclaration(
            CSharpElementFactory factory)
        {
            // We generate the property. The underlying class will ensure that it is given a good name
            if (info.UsePartial)
            {
              IPropertyDeclaration propertyDeclaration = factory.CreatePropertyDeclaration(ExpressionType, "__");
              propertyDeclaration.SetAccessRights(AccessRights.PUBLIC);
              IAccessorDeclaration accessorDeclaration1 = factory.CreateAccessorDeclaration(AccessorKind.GETTER, false);
              IAccessorDeclaration accessorDeclaration2 = factory.CreateAccessorDeclaration(AccessorKind.SETTER, false);
              propertyDeclaration.AddAccessorDeclarationAfter(accessorDeclaration1, null);
              propertyDeclaration.AddAccessorDeclarationBefore(accessorDeclaration2, null);
              propertyDeclaration.SetStatic(IsStatic);

              propertyDeclaration.DecorateWithObservablePropertyAttribute(factory);
              
              return propertyDeclaration;  
            }
            else
            {
              var fieldDeclaration = factory.CreateFieldDeclaration(ExpressionType, "__");
              fieldDeclaration.DecorateWithObservablePropertyAttribute(factory);
              return fieldDeclaration;
            }
            
            
        }

        protected override ICSharpTypeMemberDeclaration GetAnchorMember(
            IList<ICSharpTypeMemberDeclaration> members)
        {
            // We find where to insert the newly generated property
            
            ICSharpTypeMemberDeclaration memberDeclaration = members.LastOrDefault<ICSharpTypeMemberDeclaration>((Func<ICSharpTypeMemberDeclaration, bool>) (member => member.DeclaredElement is IProperty && member.IsStatic == IsStatic)) ?? members.LastOrDefault<ICSharpTypeMemberDeclaration>((Func<ICSharpTypeMemberDeclaration, bool>) (member => member.DeclaredElement is IField && member.IsStatic == IsStatic));
            return memberDeclaration == null && IsStatic ? members.LastOrDefault<ICSharpTypeMemberDeclaration>((Func<ICSharpTypeMemberDeclaration, bool>) (m => m.DeclaredElement is IProperty)) ?? members.LastOrDefault<ICSharpTypeMemberDeclaration>((Func<ICSharpTypeMemberDeclaration, bool>) (m => m.DeclaredElement is IField)) : memberDeclaration;
        }
    }
}

