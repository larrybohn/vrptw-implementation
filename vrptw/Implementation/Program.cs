using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Implementation.Solver;
using System.IO;

namespace Implementation
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: implementation.exe <input-filename>");
                return;
            }

            TaskInstance taskInstance = TaskInstance.FromFile(args[0]);

            RoutingTaskSolver solver = new RoutingTaskSolver(taskInstance);
            
            string answer = solver.Solve();

            File.WriteAllText(args[0] + ".ans", answer);
        }
    }
}
