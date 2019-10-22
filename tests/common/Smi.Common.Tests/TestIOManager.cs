
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace Smi.Common.Tests
{
    public class TestIOManager : Dicom.IO.IOManager
    {
        private readonly IFileSystem _fileSystem;

        static TestIOManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public TestIOManager(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        protected override Encoding BaseEncodingImpl => Encoding.ASCII;

        protected override Dicom.IO.IPath PathImpl => new TestPath(_fileSystem);

        protected override Dicom.IO.IFileReference CreateFileReferenceImpl(string fileName) => new TestFileReference(fileName, _fileSystem);

        protected override Dicom.IO.IDirectoryReference CreateDirectoryReferenceImpl(string directoryName) => new TestDirectoryReference(directoryName, _fileSystem);
    }

    public class TestFileReference : Dicom.IO.IFileReference
    {
        private bool _isTempFile;
        private readonly IFileSystem _fileSystem;

        public TestFileReference(string fileName, IFileSystem fileSystem)
        {
            Name = fileName;
            IsTempFile = false;
            _fileSystem = fileSystem;
        }

        ~TestFileReference()
        {
            // Can use the standard TemporaryFileRemover since it doesn't touch the filesystem directly
            if (IsTempFile)
                Dicom.IO.TemporaryFileRemover.Delete(this);
        }

        public string Name { get; private set; }

        public bool Exists => _fileSystem.File.Exists(Name);

        public bool IsTempFile
        {
            get => _isTempFile;
            set
            {
                if (value && Exists)
                    try
                    {
                        _fileSystem.File.SetAttributes(Name, _fileSystem.File.GetAttributes(Name) | System.IO.FileAttributes.Temporary);
                    }
                    catch { }

                _isTempFile = value;
            }
        }

        public Dicom.IO.IDirectoryReference Directory => new TestDirectoryReference(_fileSystem.Path.GetDirectoryName(Name), _fileSystem);

        public System.IO.Stream Create()
        {
            return _fileSystem.File.Create(Name);
        }

        public System.IO.Stream Open()
        {
            return _fileSystem.File.Open(Name, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
        }

        public System.IO.Stream OpenRead()
        {
            return _fileSystem.File.OpenRead(Name);
        }

        public System.IO.Stream OpenWrite()
        {
            return _fileSystem.File.OpenWrite(Name);
        }

        public void Delete()
        {
            _fileSystem.File.Delete(Name);
        }

        public void Move(string dstFileName, bool overwrite = false)
        {
            if (_fileSystem.File.Exists(dstFileName) && overwrite)
                _fileSystem.File.Delete(dstFileName);

            _fileSystem.File.Move(Name, dstFileName);
            Name = _fileSystem.Path.GetFullPath(dstFileName);
            IsTempFile = false;
        }

        public byte[] GetByteRange(long offset, int count)
        {
            var buffer = new byte[count];

            using (System.IO.Stream fs = OpenRead())
            {
                fs.Seek(offset, System.IO.SeekOrigin.Begin);
                fs.Read(buffer, 0, count);
            }

            return buffer;
        }

        public override string ToString() => IsTempFile ? $"{Name} [TEMP]" : Name;
    }

    public class TestDirectoryReference : Dicom.IO.IDirectoryReference
    {
        private readonly IDirectoryInfo _directoryInfo;

        public TestDirectoryReference(string directoryName, IFileSystem fileSystem)
        {
            _directoryInfo = fileSystem.DirectoryInfo.FromDirectoryName(directoryName);
        }

        public string Name => _directoryInfo.FullName;

        public bool Exists => _directoryInfo.Exists;

        public void Create() => _directoryInfo.Create();

        public IEnumerable<string> EnumerateFileNames(string searchPattern = null)
        {
            return string.IsNullOrEmpty(searchPattern?.Trim())
                       ? _directoryInfo.GetFiles().Select(fi => fi.FullName)
                       : _directoryInfo.GetFiles(searchPattern).Select(fi => fi.FullName);
        }

        public IEnumerable<string> EnumerateDirectoryNames() => _directoryInfo.EnumerateDirectories().Select(di => di.FullName);
    }

    public class TestPath : Dicom.IO.IPath
    {
        private readonly IFileSystem _fileSystem;

        public TestPath(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string GetDirectoryName(string path) => _fileSystem.Path.GetDirectoryName(path);

        public string GetTempDirectory() => _fileSystem.Path.GetTempPath();

        public string GetTempFileName() => _fileSystem.Path.GetTempFileName();

        public string Combine(params string[] paths)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));

            return paths.Aggregate(string.Empty, (current, path) => _fileSystem.Path.Combine(current, path));
        }
    }
}
