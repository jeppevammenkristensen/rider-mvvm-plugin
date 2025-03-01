using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;

namespace ReSharperPlugin.MvvmPlugin.PostFixTemplates;

[MacroDefinition("mvvmCapitalize",
    DescriptionResourceName = nameof(Resources.Strings.MvvmCapitalizeMacroDescription), 
    LongDescriptionResourceName = nameof(Resources.Strings.MvvmCapitalizeMacroLongDescription),
    ResourceType = typeof(Resources.Strings))]
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