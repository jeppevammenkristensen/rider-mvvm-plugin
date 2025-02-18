using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.PostfixTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.PostfixTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using ReSharperPlugin.MvvmPlugin.Extensions;

namespace ReSharperPlugin.MvvmPlugin.PostFixTemplates;


  [PostfixTemplate("obsprop", "Generates a observable property (CommunityToolkit).","")]
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
            CSharpElementFactory factory, IPsiModule module)
        {
            
            // We generate the property. The underlying class will ensure that it is given a good name
            if (info.UsePartial)
            {
              var propertyDeclaration = factory.CreateObservableProperty(null, info.ExpressionType);
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

