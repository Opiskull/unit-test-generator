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

        private string FindRootDirectory(string filePath)
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
                return _fileSystem.Path.GetDirectoryName(directories.First());
            }
            return FindRootDirectory(parent);
        }

        public string FindTestProject(string startDirectory, string projectName)
        {
            var projects = _fileSystem.Directory.GetFiles(startDirectory, "*.csproj", SearchOption.AllDirectories);
            return projects.SingleOrDefault(_ => _.EndsWith(projectName + ".Test.csproj"));
        }

        public string CreateTestFilePath(string inputFilePath)
        {
            var inputFileName = _fileSystem.Path.GetFileNameWithoutExtension(inputFilePath);

            var projectFilePath = FindProjectFile(inputFilePath);
            var projectName = _fileSystem.Path.GetFileNameWithoutExtension(projectFilePath);
            var projectFolderPath = _fileSystem.Path.GetDirectoryName(projectFilePath);

            var startDirectory = FindRootDirectory(inputFilePath);
            var testProjectFilePath = FindTestProject(startDirectory, projectName);
            var testProjectFolderPath = _fileSystem.Path.GetDirectoryName(testProjectFilePath);

            var outputFileName = $"{inputFileName}Test.cs";
            var outputPath = inputFilePath.Replace(projectFolderPath, "").Remove(0, 1);
            var outputFolder = _fileSystem.Path.GetDirectoryName(outputPath);

            return Path.Combine(testProjectFolderPath, outputFolder, outputFileName);
        }
    }
}
