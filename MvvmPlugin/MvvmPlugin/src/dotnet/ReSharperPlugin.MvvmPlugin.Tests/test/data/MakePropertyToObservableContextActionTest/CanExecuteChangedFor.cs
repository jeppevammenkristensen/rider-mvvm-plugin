using System;
using System.Linq.Expressions;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReSharperPlugin.MvvmPlugin.Tests.test.data.ConvertToRelayTest;

public partial class CanExecuteChangedFor : ObservableObject
{
    private string _someProperty;

    public string Some{caret}Property 
    { 
        get
        {
            return _someProperty;
        } 
        set
        {
            _someProperty = value;
            OnPropertyChanged();
            SomeCommand.NotifyCanExecuteChanged();
            CustomCommand.RaiseCanExecuteChanged();
        }
    }
    
    public IRelayCommand SomeCommand { get; set; }
    public CustomCommand CustomCommand { get; set; }
}

public class CustomCommand : System.Windows.Input.ICommand
{
    void RaiseCanExecuteChanged();
}