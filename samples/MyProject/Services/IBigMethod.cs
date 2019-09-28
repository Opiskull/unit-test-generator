using System.Threading.Tasks;

namespace MyProject.Services
{
    public interface IBigMethod
    {
        string BigResult(string input);
        void Hallo(string input);
        Task<int> GetAllAsync();
        Task SendAsync(string message);
    }
}