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
        Name = "Adds NotifyCanExecute attribute ",
    Description = "",
    GroupType = typeof(CSharpContextActions),
    Priority = 100)]
public class CreateNotifyCanExecute(ICSharpContextActionDataProvider provider) : IContextAction
{
    // The command names to suggest
    private List<string>? _commandNames; 
    
    public IEnumerable<IntentionAction> CreateBulbItems()
    {
        if (_commandNames is null)
            yield break;
        
        foreach (var name in _commandNames)
        {
            yield return new AddNotifyCanExecuteAction(provider, name).ToContextActionIntention(null);
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
               var usedCommandsByProperty = result.Select(x => x.PositionParameter(0).ConstantValue.StringValue!).ToJetHashSet();

               var availableCommands = classLikeDeclaration
                   .DeclaredElement?.Properties
                   .Where(x => x.Type.IsRelayCommand())
                   .Select(x => x.ShortName).ToJetHashSet();

               _commandNames = availableCommands?.Except(usedCommandsByProperty).ToList() ?? [];
               return _commandNames is { Count: > 0 };
           }
          
        }

        return false;
    }

    public class AddNotifyCanExecuteAction(ICSharpContextActionDataProvider provider, string name) : 
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