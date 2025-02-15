using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;

namespace ReSharperPlugin.MvvmPlugin.PostFixTemplates;

[MacroDefinition("mvvmCapitalize")]
public class MvvmCapitalizeMacroDef : SimpleMacroDefinition
{
    public override ParameterInfo[] Parameters
    {
        get
        {
            return new ParameterInfo[1]
            {
                new ParameterInfo(ParameterType.VariableReference)
            };
        }
    }
}