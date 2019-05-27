using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGo.CodeGenerator.Helpers
{
    public static class Extensions
    {
        public static string RemoveEndOfAsync(this string name, bool isAutoDetection)
        {
            if (!isAutoDetection)
                return name;
            while (name.EndsWith("Async"))
            {
                name = name.Substring(0, name.LastIndexOf("Async"));
            }
            return name;
        }

        public static bool HasEndOfAsync(this string name)
        {
            return name.EndsWith("Async");
        }
    }
}
