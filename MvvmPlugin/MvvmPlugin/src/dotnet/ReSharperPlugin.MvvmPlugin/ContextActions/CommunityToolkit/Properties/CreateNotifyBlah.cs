using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Options.Options.ThemedIcons;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.BulbActions;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Intentions.CSharp.ContextActions;
using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;
using ReSharperPlugin.MvvmPlugin.Models;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

[ContextAction(
    Name = "Add bladiblah",
    Description = "Blahili",
    GroupType = typeof(CSharpContextActions),
    Priority = 100)]
public class CreateNotifyBlah(ICSharpContextActionDataProvider provider) : IContextAction
{
    private List<string>? _names; 
    
    public IEnumerable<IntentionAction> CreateBulbItems()
    {
        if (_names is null)
            yield break;
        
        SubmenuAnchor customAnchor = new SubmenuAnchor((IAnchor) IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);
        
        foreach (var name in _names)
        {
            yield return new AddNotifyAction(provider, name).ToContextActionIntention(null);
        }
    }

    public bool IsAvailable(IUserDataHolder cache)
    {
        if (provider.GetSelectedTreeNode<IPropertyDeclaration>() is { DeclaredElement: {}} property)
        {
           if (property.GetContainingTypeDeclaration() is IClassLikeDeclaration classLikeDeclaration && PluginUtil.GetObservableObject(classLikeDeclaration).ShouldBeKnown() is {} observableObject)
           {
               if (!provider.IsValidObservableObject(classLikeDeclaration, observableObject))
               {
                   return false;
               }

               var declaredType = PluginUtil.GetNotifyCanExecuteChangedFor(classLikeDeclaration)!;
               
               var result = property.DeclaredElement.GetAttributeInstances(declaredType.GetClrName(), false);
               var usedCommands = result.Select(x => x.PositionParameter(0).ConstantValue.StringValue!).ToJetHashSet();

               var properties = classLikeDeclaration
                   .DeclaredElement?.Properties
                   .Where(x => x.Type.IsRelayCommand())
                   .Select(x => x.ShortName).ToJetHashSet();

               _names = properties?.Except(usedCommands).ToList() ?? [];
               return _names is { Count: > 0 };
           }
          
        }

        return false;
    }

    public class AddNotifyAction(ICSharpContextActionDataProvider provider, string name) : 
        ContextActionBase<IClassMemberDeclaration>
    {

        public override string Text => $"Generate Notify for {name}";
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