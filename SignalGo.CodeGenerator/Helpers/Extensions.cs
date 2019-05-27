using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGo.CodeGenerator.Helpers
{
    public static class Extensions
    {
        public static string RemoveEndOfAsync(this string name)
        {
            while (name.EndsWith("Async"))
            {
                name = name.Substring(0,name.LastIndexOf("Async"));
            }
            return name;
        }
    }
}
