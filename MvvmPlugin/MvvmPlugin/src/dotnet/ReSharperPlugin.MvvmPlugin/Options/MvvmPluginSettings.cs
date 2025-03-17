using JetBrains.Application.Settings;
using JetBrains.Application.Settings.WellKnownRootKeys;

namespace ReSharperPlugin.MvvmPlugin.Options;

[SettingsKey(
    // Discover others through usages of SettingsKeyAttribute
    Parent: typeof(EnvironmentSettings),
    Description: "Mvvm Helper options")]
public class MvvmPluginSettings
{
    // [SettingsEntry(DefaultValue: "CommunityToolkit.Mvvm.ComponentModel.ObservableObject", Description: "Base for ObservableObject")]
    // public string ObservableBaseObject;

    [SettingsEntry(DefaultValue: ObservableObjectBaseType.Object,
        Description: "Base for ObservableProperty")]
    public ObservableObjectBaseType PreferredBaseObservable;

    [SettingsEntry(DefaultValue: "ViewModelBase", Description: "Property names defined as model base")]
    public string OtherValuesString { get; set; }
}

public enum ObservableObjectBaseType
{
    Object,
    Validator,
    Recipient,
    Other
}