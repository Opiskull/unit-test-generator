using Microsoft.CodeAnalysis;
using Buildalyzer.Workspaces;
using Buildalyzer;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace Opiskull.UnitTestGenerator
{
    public class ProjectLoader
    {
        private readonly PathGenerator _pathGenerator;

        public ProjectLoader(PathGenerator pathGenerator)
        {
            _pathGenerator = pathGenerator;
        }

        public async Task<SemanticModel> LoadSemanticModelAsync(string filePath)
        {
            AnalyzerManager manager = new AnalyzerManager();
            var projectPath = _pathGenerator.FindProjectFile(filePath);

            ProjectAnalyzer analyzer = manager.GetProject(projectPath);
            AdhocWorkspace workspace = analyzer.GetWorkspace();

            var project = workspace.CurrentSolution.Projects.First();

            var doc = project.Documents.FirstOrDefault(_ => _.FilePath == filePath);

            return await doc.GetSemanticModelAsync();
        }
    }
}
