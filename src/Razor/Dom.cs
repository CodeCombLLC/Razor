using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Razor
{
    public enum CodeType
    {
        Html,
        Razor,
        Text
    }

    public class Dom
    {
        public CodeType Type { get; set; }

        public string Begin { get; set; } 

        public string End { get; set; }

        public List<Dom> Children { get; set; } = new List<Dom>();

        public void AppendChild(Dom dom)
        {
            Children.Add(dom);
        }
    }
}
