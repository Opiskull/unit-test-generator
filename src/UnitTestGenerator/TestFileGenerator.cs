using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Opiskull.UnitTestGenerator
{
    public class TestFileGenerator
    {
        public string CreateTestFile(SemanticModel semanticModel)
        {
            var compilationUnitRoot = semanticModel.SyntaxTree.GetCompilationUnitRoot();
            return compilationUnitRoot.CreateTestFile().ToString();
        }
    }
}
