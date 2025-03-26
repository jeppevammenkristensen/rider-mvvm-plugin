using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.BulbActions;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using ReSharperPlugin.MvvmPlugin.Extensions;

namespace ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

[ContextAction(Name = "Convert to partial property (CommunityToolkit)",
    Description =
        "If the current nuget version of community toolkit supports partial properties this will convert a field decorated with the ObservableProperty attribute a partial property ",
    GroupType = typeof(CSharpContextActions)
)]
public class ConvertToPartialPropertyContextAction(ICSharpContextActionDataProvider provider) : ModernScopedContextActionBase<IFieldDeclaration>
{
    public override string Text => "Convert to partial property (CommunityToolkit)";
    protected override IFieldDeclaration? TryCreateInfoFromDataProvider(IUserDataHolder cache)
    {
        if (provider.GetSelectedElement<IFieldDeclaration>() is { } fieldDeclaration)
        {
            return fieldDeclaration;
        }

        return null;
    }

    protected override bool IsAvailable(IFieldDeclaration availabilityInfo)
    {
        if (availabilityInfo.GetContainingTypeDeclaration() is IClassLikeDeclaration cls)
        {
            if (!cls.ImplementsObservableObject(observableObject: null) && !cls.CommunityToolkitCanHandlePartialProperties(null))
            {
                return false;
            }

            return availabilityInfo.IsObservableProperty();
        }
        
        return false;
    }

    protected override IBulbActionCommand? ExecutePsiTransaction(IFieldDeclaration availabilityInfo, ISolution solution,
        IProgressIndicator progress)
    {
        var cls = (IClassLikeDeclaration) availabilityInfo.GetContainingTypeDeclaration()!;
        
        // Ensure that the containing class is partial and inherits ObservableObject
        cls.SetPartial(true);

        
        var propertyName = availabilityInfo.NameIdentifier.Name.ToPropertyName();
        
        // HACK: 

        var referenceExpressions = cls.Descendants<IReferenceExpression>()
            .ToEnumerable()
            .Where(x => x.NameIdentifier.Name == availabilityInfo.NameIdentifier.Name)
            .ToList();
        
        var propertyDeclaration = provider.ElementFactory.CreateObservableProperty(propertyName, availabilityInfo.Type, generateObservableAttribute: false);

        foreach (var referenceExpression in referenceExpressions)
        {
            referenceExpression.SetName(propertyName);
        }
        
        // If there are attributes on the property add them to the field
        // The property is filtered away if it is decorated with ObservableProperty
        // So we don't check for that
        if (availabilityInfo.Attributes is {Count: > 0} attributes)
        {
            foreach (var attribute in attributes)
            {
                propertyDeclaration.AddAttributeAfter(attribute, propertyDeclaration.Attributes.LastOrDefault());
            }
        }
        
        // If there is an initializer on the field. For instance = "Hello World" 
        // we apply that to the field
        if (availabilityInfo.Initializer is IExpressionInitializer initializer)
        {
            propertyDeclaration.SetInitial(initializer);
        }

        
        cls.ReplaceClassMemberDeclaration(availabilityInfo, propertyDeclaration);
        return null;
    }
    
    
}