using System;
using System.Linq;

namespace ReSharperPlugin.MvvmPlugin.Options;

public record ViewModelGenerationOptions(string ViewModelFolder, string ViewFolder, bool UseSameFolderForViewModel)
{
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
}