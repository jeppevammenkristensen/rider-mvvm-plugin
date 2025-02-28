using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace ReSharperPlugin.MvvmPlugin.Models;

public static class PluginUtil
{
    public static IDeclaredType GetObservablePropertyAttribute(JetBrains.ReSharper.Psi.Tree.ITreeNode treeNode)
    {
        return TypeFactory.CreateTypeByCLRName(
            "CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute",
            treeNode.GetPsiModule());
    }

   

    /// <summary>
    /// Gets the <see cref="IDeclaredType"/> for a community toolkit ObservableObject
    /// </summary>
    /// <param name="treeNode">This is required to get the PsiModule</param>
    /// <returns></returns>
    public static IDeclaredType GetObservableObject(JetBrains.ReSharper.Psi.Tree.ITreeNode treeNode)
    {
        return TypeFactory.CreateTypeByCLRName(
            TypeConstants.ObservableObject.GetClrName(),
            treeNode.GetPsiModule());
    }

    /// <summary>
    /// Returns the given type as null if it is unknown
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IDeclaredType? ShouldBeKnown(this IDeclaredType type)
    {
        return type.Classify switch
        {
            TypeClassification.UNKNOWN => null,
            _ => type
        };
    }

    public static IDeclaredType? GetNotifyCanExecuteChangedFor(JetBrains.ReSharper.Psi.Tree.ITreeNode treeNode)
    {
        return TypeFactory.CreateTypeByCLRName(
            "CommunityToolkit.Mvvm.ComponentModel.NotifyCanExecuteChangedForAttribute",
            treeNode.GetPsiModule());
    }

    public static IDeclaredType GetNotifyPropertyChangedFor(JetBrains.ReSharper.Psi.Tree.ITreeNode treeNode)
    {
        return TypeFactory.CreateTypeByCLRName("CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedForAttribute",
            treeNode.GetPsiModule());
    }

    public static string? GetReference(this IProperty property)
    {
        if (property.GetAttributeInstances(TypeConstants.RelayCommandAttribute.GetClrName(), AttributesSource.All)
                .SingleItem(null) is { } single)
        {
            return single.NamedParameter("CanExecute").ConstantValue.StringValue;
        }

        return null;
    }
}
