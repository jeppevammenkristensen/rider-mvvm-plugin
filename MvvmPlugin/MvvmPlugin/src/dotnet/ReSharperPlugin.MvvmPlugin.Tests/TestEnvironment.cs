using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.MvvmPlugin.Tests
{
    [ZoneDefinition]
    public class MvvmPluginTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>, IRequire<IMvvmPluginZone>
    {
    }

    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>,
        IRequire<MvvmPluginTestEnvironmentZone>
    {
    }

    [SetUpFixture]
    public class MvvmPluginTestsAssembly : ExtensionTestEnvironmentAssembly<MvvmPluginTestEnvironmentZone>
    {
    }
}