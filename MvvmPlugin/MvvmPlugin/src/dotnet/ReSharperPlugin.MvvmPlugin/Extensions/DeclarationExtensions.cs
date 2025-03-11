using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using ReSharperPlugin.MvvmPlugin.Models;

public static class DeclarationExtensions
{
    public static bool IsObservableProperty(this IClassMemberDeclaration declaration)
    {
        if (!declaration.Attributes.Any())
            return false;

        if (declaration.DeclaredElement is IAttributesSet attributesSet)
        {
            return attributesSet.HasAttributeInstance(TypeConstants.ObservableProperty.GetClrName(), false);
        }
        return true;
    }

    public static bool DoesNotHaveAttribute(this IAttributesOwnerDeclaration item, IDeclaredType attribute)
    {
        if (!item.Attributes.Any())
        {
            return true;
        }
        if (item.DeclaredElement is IAttributesSet attributesSet)
        {
            return !attributesSet.HasAttributeInstance(attribute.GetClrName(), false);
        }
        return true;
    }
}