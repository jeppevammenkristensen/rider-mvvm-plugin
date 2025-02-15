using System.Globalization;
using System.Runtime.InteropServices;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.Util;

namespace ReSharperPlugin.MvvmPlugin.PostFixTemplates;

[MacroImplementation(Definition = typeof (MvvmCapitalizeMacroDef))]
public class MvvmCapitalizeMacroImpl : SimpleMacroImplementation
{
    private readonly IMacroParameterValueNew myArgument;

    public MvvmCapitalizeMacroImpl([Optional] MacroParameterValueCollection arguments)
    {
        this.myArgument = arguments.OptionalFirstOrDefault();
    }

    public override string EvaluateQuickResult(IHotspotContext context)
    {
        return this.myArgument != null ? this.Execute(this.myArgument.GetValue()) : (string) null;
    }

    private static string CapitalizeAlphanum(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        int startIndex = 0;
        
        int num = -1;
        for (int index = 0; index < s.Length; ++index)
        {
            if (s[index] == '_')
            {
                startIndex = index + 1;
            }
            
            if (s[index].IsLetterOrDigitFast())
            {
                num = index;
                break;
            }
        }
        if (num < 0)
            return s;
        char upper = char.ToUpper(s[num], CultureInfo.InvariantCulture);
        return (int) s[num] == (int) upper ? s : s.Substring(startIndex, num-startIndex) + upper.ToString() + s.Substring(num + 1);
    }

    private string Execute(string text)
    {
        if (text == null)
            return string.Empty;
        if (text.Length > 0)
            text = MvvmCapitalizeMacroImpl.CapitalizeAlphanum(text);
        return text;
    }
}