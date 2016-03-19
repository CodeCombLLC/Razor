using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Razor;

namespace Razor.PerformanceTests
{
    public class Program
    {
        private static void dfs(Dom root)
        {
            foreach (var child in root.Children)
            {
                dfs(child);
            }
        }

        public static void Main(string[] args)
        {
            var path = Console.ReadLine();
            var code = System.IO.File.ReadAllText(path);
            var root = Analyze.AnalyzeDom(code);
            Console.ReadKey();
        }
    }
}
