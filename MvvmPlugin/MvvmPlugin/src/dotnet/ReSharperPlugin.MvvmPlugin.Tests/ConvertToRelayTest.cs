using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

namespace ReSharperPlugin.MvvmPlugin.Tests;

[TestPackages("CommunityToolkit.Mvvm/8.4.0")]
[CSharpLanguageLevelAttribute(CSharpLanguageLevel.CSharp140)]
public class ConvertToRelayTest : CSharpContextActionExecuteTestBase<ConvertToRelayProperty>
{
    protected override string ExtraPath { get; } = string.Empty;

    protected override string RelativeTestDataPath => nameof(ConvertToRelayTest);
    
    
    [Test, ExecuteScopedActionInFile]
    public void FirstTest() =>  DoNamedTest();

}

[TestPackages("CommunityToolkit.Mvvm/8.4.0")]
[CSharpLanguageLevelAttribute(CSharpLanguageLevel.CSharp140)]
public class ConvertToRelayAvailableTest : CSharpContextActionAvailabilityTestBase<ConvertToRelayProperty>
{
    protected override string ExtraPath { get; } = string.Empty;

    protected override string RelativeTestDataPath => nameof(ConvertToRelayTest);


    [Test]
    public void FirstTestAvailable()
    {
        DoNamedTest();
    }

}