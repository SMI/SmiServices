using NUnit.Framework;
using System;
using System.IO;

namespace SmiServices.UnitTests.TestCommon;

public sealed class DisposableTempDir : IDisposable
{
    private const string PREFIX = "smiservices-nunit-";
    public readonly DirectoryInfo DirectoryInfo;

    public DisposableTempDir()
    {
        DirectoryInfo = Directory.CreateTempSubdirectory(PREFIX);
        TestContext.Out.WriteLine($"[{GetType().Name}] Created {DirectoryInfo}");
    }

    public void Dispose()
    {
        try
        {
            DirectoryInfo.Delete(recursive: true);
            TestContext.Out.WriteLine($"[{GetType().Name}] Deleted {DirectoryInfo}");
        }
        catch (UnauthorizedAccessException)
        {
            TestContext.Error.WriteLine($"[{GetType().Name}] Could not delete {DirectoryInfo}");
        }
    }

    public static implicit operator string(DisposableTempDir d) => d.DirectoryInfo.FullName;
}
