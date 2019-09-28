using System.Threading.Tasks;

namespace MyProject.Services
{
    public interface IBigMethod
    {
        string BigResult();
        void Hallo();
        Task<int> GetAllAsync();
        Task SendAsync();
    }
}