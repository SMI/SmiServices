using System.IO;

namespace SmiServices.UnitTests.TestCommon;

public static class TestDirectoryHelpers
{
    private static DirectoryInfo? _slnDirInfo;

    public static DirectoryInfo SlnDirectoryInfo()
    {
        if (_slnDirInfo != null)
            return _slnDirInfo;

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory != null && directory.GetFiles("*.sln").Length == 0)
            directory = directory.Parent;

        if (directory == null)
            throw new FileNotFoundException("Could not find sln file");

        _slnDirInfo = directory;
        return directory;
    }
}
