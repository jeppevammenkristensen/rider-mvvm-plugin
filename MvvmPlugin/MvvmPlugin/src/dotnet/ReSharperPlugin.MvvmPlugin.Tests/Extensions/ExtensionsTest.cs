using JetBrains.Annotations;
using NUnit.Framework;
using ReSharperPlugin.MvvmPlugin.Extensions;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace ReSharperPlugin.MvvmPlugin.Tests.Extensions;

[TestSubject(typeof(MvvmPlugin.Extensions.ContextActionUtil))]
public class ExtensionsTest
{

    [Theory]
    [TestCase("Test", "_test")]
    [TestCase("_Test", "_Test")] // Allready snake case / special scenario
    [TestCase("test", "_test")]
    [TestCase(" ", "_ ")]
    [TestCase("A","_a")]
    public void ToSnakeCase_Generates_Expected_Result(string input, string expected)
    {
        Assert.AreEqual(input.ToFieldName(),expected);
    }
}
