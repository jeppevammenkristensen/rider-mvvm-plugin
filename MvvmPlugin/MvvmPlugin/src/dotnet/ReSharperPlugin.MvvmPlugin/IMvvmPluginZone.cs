using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.UI.Options.OptionPages;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp;

namespace ReSharperPlugin.MvvmPlugin
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IToolsOptionsPageImplZone>
    {
    }
    
    [ZoneDefinition]
    // [ZoneDefinitionConfigurableFeature("Title", "Description", IsInProductSection: false)]
    public interface IMvvmPluginZone : IZone, 
        IRequire<ILanguageCSharpZone>, IRequire<IDocumentModelZone>,
        IRequire<IProjectModelZone>
        
    {
    }
}