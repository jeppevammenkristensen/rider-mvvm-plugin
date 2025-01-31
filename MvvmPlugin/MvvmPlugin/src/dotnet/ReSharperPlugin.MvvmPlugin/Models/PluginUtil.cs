using JetBrains.ReSharper.Psi;

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
            "CommunityToolkit.Mvvm.ComponentModel.ObservableObject",
            treeNode.GetPsiModule());
    }
}