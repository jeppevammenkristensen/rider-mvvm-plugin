using System;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;

public static class MacroExtensions
{
    public static MacroCallExpressionNew ToMacroCall<T>(this T macro) where T : IMacroDefinition
    {
        return new MacroCallExpressionNew(macro);
    }

    public static MacroCallExpressionNew WithParameter(this MacroCallExpressionNew macro, IMacroParameterValue value)
    {
        macro.AddParameter(value);
        return macro;
    }

    public static MacroCallExpressionNew WithConstant(this MacroCallExpressionNew macro, string value)
    {
        return macro.WithParameter(new ConstantMacroParameter(value));
    }
}