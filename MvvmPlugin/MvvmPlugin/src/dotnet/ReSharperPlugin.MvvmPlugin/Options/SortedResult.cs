using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings.Calculated.Extensions;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace ReSharperPlugin.MvvmPlugin.Options;

public class SortedResult
{
    private Dictionary<string, int> _dictionary;
    public SortedResult(IEnumerable<string> values)
    {
        _dictionary = values.Select((x,i) => (x,i)).ToDictionary(x => x.x, x => x.i, StringComparer.OrdinalIgnoreCase);
    }

    public bool Empty => _dictionary.Count <= 0;

    public bool IsMatch(string value)
    {
        return _dictionary.ContainsKey(value);
    }

    public int Sort(string value)
    {
        return _dictionary.TryGetValue(value, out var index) ? index : int.MaxValue;
    }
   
}

// Note. Look under src\rider\main\kotlin\com\jetbrains\rider\plugins\mvvmplugin for necessary kotlin files
// releated to hooking up the options and also the plugin.xml file. 