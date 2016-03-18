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
            Console.WriteLine(root.Type + " " + root.Begin);
            
            foreach (var child in root.Children)
            {
                Console.WriteLine(" ___ ");
                dfs(child);
                Console.WriteLine(" ___ ");
            }
            Console.WriteLine(root.Type + " " + root.End);
        }

        public static void Main(string[] args)
        {
            var root = Analyze.AnalyzeDom(@"<html>
@if (2 > 1)
{
    <a>123</a>
}
</html>");
            Console.ReadKey();
        }
    }
}
