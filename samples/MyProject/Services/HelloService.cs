using System;
using System.Threading.Tasks;

namespace MyProject.Services
{
    public class HelloService
    {
        private IBigMethod _bigMethod;
        private IAnotherMethod _anotherMethod;

        public HelloService(IBigMethod bigMethod, IAnotherMethod anotherMethod)
        {
            _bigMethod = bigMethod;
            _anotherMethod = anotherMethod;
        }

        public void BigMethod()
        {
            Console.WriteLine("Hello, World!");
            Boom();
        }

        public string AnotherMethod()
        {
            var result = _bigMethod.BigResult();
            _bigMethod.Hallo();
            if (_anotherMethod.IsBigResult(result))
            {
                return result;
            }
            Console.WriteLine(result);
            return "Big File";
        }

        public async Task<int> BigIntMethod()
        {
            var result = await _bigMethod.GetAllAsync();
            return result;
        }

        private void Boom()
        {
            Console.WriteLine("Boom");
        }
    }
}