using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.Input;

namespace ReSharperPlugin.MvvmPlugin.Tests.test.data.ConvertToRelayTest;

public partial class FirstTest : ObservableObject
{
    public IRelayCommand SomeComm{caret}and { get; }
    
}