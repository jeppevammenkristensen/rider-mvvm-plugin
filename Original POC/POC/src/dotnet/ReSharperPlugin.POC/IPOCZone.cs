using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace ReSharperPlugin.POC
{
    [ZoneDefinition]
    // [ZoneDefinitionConfigurableFeature("Title", "Description", IsInProductSection: false)]
    public interface IPOCZone : IZone, IRequire<ILanguageCSharpZone>, IRequire<IDocumentModelZone>
    {
    }
}