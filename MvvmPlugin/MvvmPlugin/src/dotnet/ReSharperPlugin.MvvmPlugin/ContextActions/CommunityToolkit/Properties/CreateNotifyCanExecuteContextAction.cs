using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

[ContextAction(
    Name = "Add NotifyCanExecute attribute",
    Description = "Add NotifyCanExecute attribute to the selected property or field.",
    GroupType = typeof(CSharpContextActions))]
public class CreateNotifyCanExecuteContextAction(ICSharpContextActionDataProvider provider) : IContextAction
{
    // The command names to suggest
    private List<string>? myCommandNames; 
    
    public IEnumerable<IntentionAction> CreateBulbItems()
    {
        if (myCommandNames is null)
            yield break;
        
        foreach (var name in myCommandNames)
        {
            yield return new AddNotifyCanExecuteAction(provider, name).ToContextActionIntention(null);
        }
    }

    public bool IsAvailable(IUserDataHolder cache)
    {
        if (provider.GetSelectedTreeNode<IPropertyDeclaration>() is {DeclaredElement: { }} property)
        {
            return CheckCanExecute(property);
        }

        if (provider.GetSelectedTreeNode<IFieldDeclaration>() is {DeclaredElement: { }} field)
        {
            return CheckCanExecute(field);
        }

        return false;
    }

    private bool CheckCanExecute(IClassMemberDeclaration property)
    {
        if (property.GetContainingTypeDeclaration() is IClassLikeDeclaration classLikeDeclaration && PluginUtil.GetObservableObject(classLikeDeclaration).ShouldBeKnown() is {} observableObject)
        {
            if (property.DeclaredElement is null)
                return false;

            if (!property.DeclaredElement.HasAttributeInstance(TypeConstants.ObservableProperty.GetClrName(),
                    AttributesSource.All))
            {
                return false;
            }
            
            
            if (!provider.IsValidObservableObject(classLikeDeclaration, observableObject))
            {
                return true;
            }

            var declaredType = PluginUtil.GetNotifyCanExecuteChangedFor(classLikeDeclaration)!;
               
            var result = property.DeclaredElement.GetAttributeInstances(declaredType.GetClrName(), false);
            var usedCommandsByProperty = result.Select(x => x.PositionParameter(0).ConstantValue.StringValue!).ToJetHashSet();

            var availableCommands = classLikeDeclaration
                .DeclaredElement?.Properties
                .Where(x => x.Type.IsRelayCommand())
                .Select(x => x.ShortName).ToJetHashSet();

            myCommandNames = availableCommands?.Except(usedCommandsByProperty).ToList() ?? [];
            return true;
        }

        return false;
    }

    public class AddNotifyCanExecuteAction(ICSharpContextActionDataProvider provider, string name) : 
        ContextActionBase<IClassMemberDeclaration>
    {

        public override string Text => $"Generate NotifyCanExecuteFor for {name} (CommunityToolkit)";
        protected override IClassMemberDeclaration? TryCreateInfoFromDataProvider(IUserDataHolder cache)
        {
            return provider.GetSelectedElement<IClassMemberDeclaration>();
        }

        protected override bool IsAvailable(IClassMemberDeclaration availabilityInfo)
        {
            return true;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(IClassMemberDeclaration availabilityInfo, ISolution solution,
            IProgressIndicator progress)
        {
            var notifyCanExecuteChangedFor = PluginUtil.GetNotifyCanExecuteChangedFor(availabilityInfo);
            var attribute = provider.ElementFactory.CreateAttribute(notifyCanExecuteChangedFor.GetTypeElement());
            attribute.AddArgumentAfter(provider.ElementFactory.CreateArgument(ParameterKind.VALUE,
                provider.ElementFactory.CreateExpression($"nameof({name})")), null);
            
            availabilityInfo.AddAttributeBefore(attribute, availabilityInfo.Attributes.LastOrDefault());
            return null;
        }
    }
}