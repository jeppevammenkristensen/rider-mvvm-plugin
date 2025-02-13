using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.PostfixTemplates.Templates;
using JetBrains.ReSharper.Feature.Services.PostfixTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using ReSharperPlugin.MvvmPlugin.Extensions;

namespace ReSharperPlugin.MvvmPlugin.PostFixTemplates;




  // NOTE: This is a modified version of the IntroduceMemberTemplateBase

  [PostfixTemplate("obsprop", "Generates a observable property","")]
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
              propertyDeclaration.SetPartial(true);

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

