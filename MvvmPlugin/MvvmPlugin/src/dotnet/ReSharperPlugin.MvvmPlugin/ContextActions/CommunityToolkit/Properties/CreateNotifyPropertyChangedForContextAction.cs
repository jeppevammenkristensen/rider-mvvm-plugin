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
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

[ContextAction(
    Name = "Add NotifyPropertyChangedFor attribute",
    Description = "Some description",
    GroupType = typeof(CSharpContextActions))]
public class CreateNotifyPropertyChangedForContextAction(ICSharpContextActionDataProvider provider) : IContextAction
{
    private List<string>? myPropertyNames = null;
    
    public IEnumerable<IntentionAction> CreateBulbItems()
    {
        if (myPropertyNames is null)
            yield break;
        
        foreach (var name in myPropertyNames)
        {
            yield return new AddNotifyPropertyChangedForAction(provider, name).ToContextActionIntention(null);
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
    
    private bool CheckCanExecute(IClassMemberDeclaration member)
    {
        if (member.GetContainingTypeDeclaration() is IClassLikeDeclaration classLikeDeclaration && PluginUtil.GetObservableObject(classLikeDeclaration).ShouldBeKnown() is {} observableObject)
        {
            if (!provider.IsValidObservableObject(classLikeDeclaration, observableObject))
            {
                return true;
            }
            
            if (member.DeclaredElement is null)
                return false;
            
            if (!member.DeclaredElement.HasAttributeInstance(TypeConstants.ObservableProperty.GetClrName(),
                    AttributesSource.All))
            {
                return false;
            }

            var declaredType = PluginUtil.GetNotifyPropertyChangedFor(classLikeDeclaration)!;
               
            var result = member.DeclaredElement!.GetAttributeInstances(declaredType.GetClrName(), false);
            var usedNotifyProperties = result.Select(x => x.PositionParameter(0).ConstantValue.StringValue!).ToJetHashSet();

            if (member.GetPropertyName() is not { } propertyName)
            {
                return false;
            }

            usedNotifyProperties.Add(propertyName);
            
            var allProperties = classLikeDeclaration
                .DeclaredElement?.Properties
                .Where(x => x.Type.IsRelayCommand() == false)
                .Select(x => x.ShortName).ToJetHashSet();

            myPropertyNames = allProperties?.Except(usedNotifyProperties).ToList() ?? [];
            return true;
        }

        return false;
    }
    
    public class AddNotifyPropertyChangedForAction(ICSharpContextActionDataProvider provider, string name) : 
        ContextActionBase<IClassMemberDeclaration>
    {

        public override string Text => $"Generate NotifyPropertyChangedFor for {name} (CommunityToolkit)";
        protected override IClassMemberDeclaration? TryCreateInfoFromDataProvider(IUserDataHolder cache)
        {
            return provider.GetSelectedElement<IClassMemberDeclaration>();
        }

        protected override bool IsAvailable(IClassMemberDeclaration availabilityInfo)
        {
            return true;
        }

        protected override Action<ITextControl>? ExecutePsiTransaction(IClassMemberDeclaration availabilityInfo, ISolution solution,
            IProgressIndicator progress)
        {
            availabilityInfo.AddNotifyPropertyChangedAttribute(name,provider.ElementFactory);
            return null;
        }
    }
}