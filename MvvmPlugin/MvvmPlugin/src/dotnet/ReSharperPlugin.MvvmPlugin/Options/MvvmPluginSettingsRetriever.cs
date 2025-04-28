using System;
using System.Linq;
using JetBrains.Application.Settings;

namespace ReSharperPlugin.MvvmPlugin.Options;

public static class MvvmPluginSettingsRetriever
{
    public static ObservableObjectBaseType GetObservableObjectValue(IContextBoundSettingsStore? settingsStore)
    {
        return settingsStore?.GetValue((MvvmPluginSettings s) => s.PreferredBaseObservable) ?? ObservableObjectBaseType.Object;
    }

    public static SortedResult GetOtherValuesAsHashSet(IContextBoundSettingsStore? setting)
    {
        var value = setting?.GetValue((MvvmPluginSettings s) => s.OtherValuesString) ?? "ViewModelBase";
        return new SortedResult(value.Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()));
    }

    public static ViewModelGenerationOptions GetViewModelGenerationOptions(IContextBoundSettingsStore? settingsStore)
    {
        var viewModelFolder = settingsStore?.GetValue((MvvmPluginSettings s) => s.ViewModelsFolder) ?? "ViewModels";
        var viewFolder = settingsStore?.GetValue((MvvmPluginSettings s) => s.ViewsFolder) ?? "Views";
        var useSameFolderForViewModel = settingsStore?.GetValue((MvvmPluginSettings s) => s.UseSameFolderForViewModel) ?? false;
        return new ViewModelGenerationOptions(viewModelFolder, viewFolder, useSameFolderForViewModel);
    }
}