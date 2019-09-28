using System;
using System.Threading.Tasks;

namespace MyProject.Services
{
    public class HelloService
    {
        private readonly IBigMethod _bigMethod;
        private readonly IAnotherMethod _anotherMethod;

        public HelloService(IBigMethod bigMethod, IAnotherMethod anotherMethod)
        {
            _bigMethod = bigMethod;
            _anotherMethod = anotherMethod;
        }

        public void SendMessage(string message)
        {
            Console.WriteLine(message);
            Boom();
        }

        public string AnotherMethod()
        {
            var result = _bigMethod.BigResult("Hallo");
            _bigMethod.Hallo("No");
            if (_anotherMethod.IsBigResult(result))
            {
                return result;
            }
            Console.WriteLine(result);
            return "Big File";
        }

        public async Task<int> BigIntMethod()
        {
            await _bigMethod.SendAsync("This is a Message");
            var result = await _bigMethod.GetAllAsync();
            return result;
        }

        private void Boom()
        {
            Console.WriteLine("Boom");
        }
    }
}