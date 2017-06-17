using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var rm = new ResourceManager();
            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}
