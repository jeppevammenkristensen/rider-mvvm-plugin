using System;
using System.Linq;

namespace ReSharperPlugin.MvvmPlugin.Options;

public class ViewModelGenerationOptions
{
    public ViewModelGenerationOptions(string viewModelFolder, string viewFolder, bool useSameFolderForViewModel)
    {
        ViewModelFolder = viewModelFolder;
        ViewFolder = viewFolder;
        UseSameFolderForViewModel = useSameFolderForViewModel;
    }

    public string[] GetViewModelFolders()
    {
        return ViewModelFolder.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).ToArray();
    }

    public string[] GetViewFolders()
    {
        return ViewFolder.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).ToArray();
    }

    public string ViewModelFolder { get; private set; }
    public string ViewFolder { get; private set; }
    public bool UseSameFolderForViewModel { get; private set; }
}