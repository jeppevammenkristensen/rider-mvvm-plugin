using System;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.MvvmPlugin.Models;

public static class PluginConstants 
{
    public const string DatatypeName = "DataType";
    public const string DataContextName = "DataContext";

    public const string PlaceHolderName = "__";
    
    
}

public static class TypeConstants
{
    public static readonly ClrTypeNameWrapper IAsyncRelayCommand = "CommunityToolkit.Mvvm.Input.IAsyncRelayCommand";
    public static readonly ClrTypeNameWrapper IRelayCommand = "CommunityToolkit.Mvvm.Input.IRelayCommand";

    public static readonly ClrTypeNameWrapper RelayCommandAttribute =
        "CommunityToolkit.Mvvm.Input.RelayCommandAttribute";
    
    
    public static readonly ClrTypeNameWrapper NotifyCanExecuteChangedForAttributeName =
        "CommunityToolkit.Mvvm.ComponentModel.NotifyCanExecuteChangedForAttribute";


    /// <summary>
    /// Return the the given type as a declared type. If the type is not found in the given
    /// context of the <see cref="node"/> null will be returned
    /// </summary>
    /// <param name="type"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IDeclaredType? GetDeclaredTypeOrNull(this ClrTypeNameWrapper type, ITreeNode node)
    {
        return type.GetDeclaredType(node).ShouldBeKnown();
    }
    
}


public class ClrTypeNameWrapper
{
    public string Name { get; }

    public ClrTypeNameWrapper(string name, bool generic = false)
    {
        Name = name;
        Generic = generic;
    }

    public bool Generic { get;  }

    public ClrTypeName GetClrName() => new ClrTypeName(Name);

    public ClrTypeNameWrapper GenericOneType()
    {
        if (Generic)
            throw new InvalidOperationException("Type is alread generic");
        
        return new($"{Name}`1", true);
    }
    
    public static implicit operator ClrTypeNameWrapper(string name) => new(name);
   
    public static implicit operator ClrTypeName(ClrTypeNameWrapper wrapper) => wrapper.GetClrName();

    public static implicit operator string(ClrTypeNameWrapper wrapper) => wrapper;
   
    
    public IDeclaredType GetDeclaredType(ITreeNode node)
    {
        return TypeFactory.CreateTypeByCLRName(this.GetClrName(), node.GetPsiModule());
    }
}