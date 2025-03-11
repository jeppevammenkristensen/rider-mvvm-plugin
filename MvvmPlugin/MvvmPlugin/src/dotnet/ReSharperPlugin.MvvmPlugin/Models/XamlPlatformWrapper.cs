using System.Diagnostics.CodeAnalysis;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;

namespace ReSharperPlugin.MvvmPlugin.Models;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum SupportedXamlPlatform
{
    None,
    AVALONIA = XamlPlatform.AVALONIA,
    WPF = XamlPlatform.WPF,
    MAUI = XamlPlatform.MAUI,
    WINUI = XamlPlatform.WINUI
}

public class XamlPlatformWrapper
{
    private readonly XamlPlatform _platform;

    public bool IsUnSupportedPlatform()
    {
        // the platforms under and including 16 are WinRt, WindowsPhone, Silverlight
        return (int)_platform <= 16 || _platform is XamlPlatform.XAMARIN_FORMS or XamlPlatform.UWP or XamlPlatform.UNO;
    }

    public bool IsSupportedPlatform()
    {
        return !IsUnSupportedPlatform();
    }
    
    public XamlPlatformWrapper(XamlPlatform platform)
    {
        _platform = platform;
    }

    public static implicit operator XamlPlatformWrapper(XamlPlatform wrapper)
    {
        return new(wrapper);
    }

    public static implicit operator SupportedXamlPlatform(XamlPlatformWrapper wrapper)
    {
        return wrapper.SupportedPlatformEnum;
    }

    public static XamlPlatformWrapper CreateFromTreeNode(ITreeNode node)
    {
        return new XamlPlatformWrapper(XamlPlatformUtil.GetXamlNodePlatform(node));
    }

    public SupportedXamlPlatform SupportedPlatformEnum {
        get
        {
            if (IsUnSupportedPlatform())
            {
                return SupportedXamlPlatform.None;
            }
            
            // Check if the value of XamlPlatform is composed of mulitple values
            

            if ((_platform & XamlPlatform.WINUI) == XamlPlatform.WINUI)
            {
                return SupportedXamlPlatform.WINUI;
            }
            
            if ((_platform & (_platform - 1)) != 0)
            {
                return SupportedXamlPlatform.None;
            }

            return (SupportedXamlPlatform) (int) _platform;    
        }
    }
    
}