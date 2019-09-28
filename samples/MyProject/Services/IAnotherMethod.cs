using System.Collections.Generic;

namespace MyProject.Services
{
    public interface IAnotherMethod
    {
        bool IsBigResult(string result);

        string[] GetAllMethods();

        IEnumerable<string> GetItems(string search);
    }
}