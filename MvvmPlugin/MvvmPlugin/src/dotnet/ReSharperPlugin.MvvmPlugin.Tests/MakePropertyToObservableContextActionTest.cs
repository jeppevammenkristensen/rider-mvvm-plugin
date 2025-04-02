using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.MvvmPlugin.ContextActions.CommunityToolkit.Properties;

namespace ReSharperPlugin.MvvmPlugin.Tests;

[TestPackages("CommunityToolkit.Mvvm/8.4.0")]
[CSharpLanguageLevelAttribute(CSharpLanguageLevel.Experimental)]
public class MakePropertyToObservableContextActionTest : CSharpContextActionExecuteTestBase<
    MakePropertyToObservableContextAction>
{
    protected override string ExtraPath { get; } = string.Empty;

    protected override string RelativeTestDataPath => nameof(MakePropertyToObservableContextActionTest);

    [Test]
    public void NotifyPropertyChangedTest() => DoNamedTest();

    [Test]
    public void CanExecuteChangedFor() => DoNamedTest();

}



