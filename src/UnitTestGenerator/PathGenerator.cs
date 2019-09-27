using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Opiskull.UnitTestGenerator
{
    public class PathGenerator
    {
        private readonly IFileSystem _fileSystem;

        public PathGenerator(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string FindProjectFile(string filePath)
        {
            var parent = _fileSystem.Path.GetDirectoryName(filePath);
            var files = _fileSystem.Directory.GetFiles(parent, "*.csproj");

            if (parent == _fileSystem.Path.GetPathRoot(parent))
            {
                throw new FileNotFoundException();
            }

            if (files.Length == 1)
            {
                return _fileSystem.Path.GetFullPath(files.First());
            }
            else
            {
                return FindProjectFile(parent);
            }
        }

        private string FindStartDirectory(string filePath)
        {
            var parent = _fileSystem.Path.GetDirectoryName(filePath);
            if (parent == _fileSystem.Path.GetPathRoot(parent))
            {
                throw new FileNotFoundException();
            }
            var files = _fileSystem.Directory.GetFiles(parent, "*.sln");
            if (files.Length == 1)
            {
                return files.First();
            }
            var directories = _fileSystem.Directory.GetDirectories(parent, ".git");
            if (directories.Length == 1)
            {
                return directories.First();
            }
            return FindStartDirectory(parent);
        }

        public string FindTestProject(string startDirectory, string projectName)
        {
            var projects = _fileSystem.Directory.GetFiles(startDirectory, "*.csproj", SearchOption.AllDirectories);
            return projects.FirstOrDefault(_ => _.StartsWith(projectName));
        }
    }
}
