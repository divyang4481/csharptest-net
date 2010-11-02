using System;
using System.Collections.Generic;

namespace CSharpTest.Net.Library.Test
{
    class AssemblyRunnerSampleApp
    {
        static int Main(string[] arguments)
        {
            Console.WriteLine("WorkingDirectory = {0}", Environment.CurrentDirectory);
            for(int i = 0; i < arguments.Length; i++)
                Console.WriteLine("argument[{0}] = {1}", i, arguments[i]);
            Console.WriteLine("std-input:");
            string line;
            while (null != (line = Console.In.ReadLine()))
                Console.WriteLine(line);
            Console.Error.WriteLine("std-err");

            if (arguments.Length == 1 && arguments[0] == "-wait")
            {
                while (true) System.Threading.Thread.Sleep(100);
            }
            if (arguments.Length == 2 && arguments[0] == "-throw")
            {
                throw (Exception)Activator.CreateInstance(Type.GetType(arguments[1]));
            }

            return arguments.Length;
        }
    }
}
