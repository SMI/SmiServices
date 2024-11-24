using NUnit.Framework;
using SmiServices.Common.Options;
using SmiServices.UnitTests.Common;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

namespace SmiServices.UnitTests.Applications.DynamicRulesTester;

public class ProgramTests
{
    private MockFileSystem _fileSystem = null!;
    private string _dynamicRulesFileName = null!;
    private string _testRowFileName = null!;

    private IEnumerable<string> _args = null!;

    #region Fixture Methods

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        SmiCliInit.InitSmiLogging = false;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }

    private void WriteDynamicRulesFile(IEnumerable<string> lines) => _fileSystem.File.WriteAllLines(_dynamicRulesFileName, lines);

    private void WriteTestRowFile(IEnumerable<string> lines) => _fileSystem.File.WriteAllLines(_testRowFileName, lines);

    #endregion

    #region Test Methods

    [SetUp]
    public void SetUp()
    {
        _fileSystem = new MockFileSystem();
        _dynamicRulesFileName = _fileSystem.Path.GetFullPath("DynamicRules.txt");
        _testRowFileName = _fileSystem.Path.GetFullPath("TestRow.json");

        _args =
        [
            "-d", _dynamicRulesFileName,
            "-r", _testRowFileName,
        ];
    }

    [TearDown]
    public void TearDown() { }

    #endregion

    #region Tests

    [Test]
    public void Main_AllowedFile_ReturnsZero()
    {
        // Arrange

        WriteDynamicRulesFile(
        [
            "if (!(row[\"ImageType\"].ToString().Contains(\"ORIGINAL\")))",
            "{",
            "    return \"ImageType is not ORIGINAL\";\n",
            "}",
        ]);

        WriteTestRowFile(
        [
            "{",
            "    \"ImageType\": \"ORIGINAL\"",
            "}",
        ]);

        // Act
        var rc = SmiServices.Applications.DynamicRulesTester.DynamicRulesTester.Main(_args, _fileSystem);

        // Assert
        Assert.That(rc, Is.EqualTo(0));
    }

    [Test]
    public void Main_DisallowedFile_ReturnsNonZero()
    {
        // Arrange

        WriteDynamicRulesFile(
        [
            "if (!(row[\"ImageType\"].ToString().Contains(\"ORIGINAL\")))",
            "{",
            "    return \"ImageType is not ORIGINAL\";\n",
            "}",
        ]);

        WriteTestRowFile(
        [
            "{",
            "    \"ImageType\": \"SECONDARY\"",
            "}",
        ]);

        // Act
        var rc = SmiServices.Applications.DynamicRulesTester.DynamicRulesTester.Main(_args, _fileSystem);

        // Assert
        Assert.That(rc, Is.EqualTo(1));
    }

    [Test]
    public void Main_EmptyRulesFile_ReturnsNonZero()
    {
        // Arrange

        WriteDynamicRulesFile([]);

        WriteTestRowFile(
        [
            "{",
            "    \"ImageType\": \"ORIGINAL\"",
            "}",
        ]);

        // Act
        var rc = SmiServices.Applications.DynamicRulesTester.DynamicRulesTester.Main(_args, _fileSystem);

        // Assert
        Assert.That(rc, Is.EqualTo(2));
    }

    [Test]
    public void Main_EmptyTestRowFile_ReturnsNonZero()
    {
        // Arrange

        WriteDynamicRulesFile(
        [
            "if (!(row[\"ImageType\"].ToString().Contains(\"ORIGINAL\")))",
            "{",
            "    return \"ImageType is not ORIGINAL\";\n",
            "}",
        ]);

        WriteTestRowFile([]);

        // Act
        var rc = SmiServices.Applications.DynamicRulesTester.DynamicRulesTester.Main(_args, _fileSystem);

        // Assert
        Assert.That(rc, Is.EqualTo(1));
    }

    #endregion
}
