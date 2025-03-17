using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Calculated.Extensions;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.Util;

namespace ReSharperPlugin.MvvmPlugin.Options;

public static class MvvmPluginSettingsRetriever
{
    public static ObservableObjectBaseType GetObservableObjectValue(IContextBoundSettingsStore? settingsStore)
    {
        return settingsStore?.GetValue((MvvmPluginSettings s) => s.PreferredBaseObservable) ?? ObservableObjectBaseType.Object;
    }

    public static JetHashSet<string> GetOtherValuesAsHashSet(IContextBoundSettingsStore? setting)
    {
        var value = setting?.GetValue((MvvmPluginSettings s) => s.OtherValuesString) ?? "ViewModelBase";
        return value.Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToJetHashSet(StringComparer.OrdinalIgnoreCase);

    }
}

// Note. Look under src\rider\main\kotlin\com\jetbrains\rider\plugins\mvvmplugin for necessary kotlin files
// releated to hooking up the options and also the plugin.xml file. 