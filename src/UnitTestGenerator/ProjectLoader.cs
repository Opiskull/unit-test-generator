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
        public static async Task<SemanticModel> LoadAsync(string projectPath, string filePath)
        {
            AnalyzerManager manager = new AnalyzerManager();
            ProjectAnalyzer analyzer = manager.GetProject(projectPath);
            AdhocWorkspace workspace = analyzer.GetWorkspace();

            var project = workspace.CurrentSolution.Projects.First();

            var doc = project.Documents.FirstOrDefault(_ => _.FilePath == filePath);

            return await doc.GetSemanticModelAsync();
        }
    }
}
