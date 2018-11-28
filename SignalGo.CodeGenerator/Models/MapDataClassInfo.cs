using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGo.CodeGenerator.Models
{
    public class MapDataClassInfo
    {
        public string Body { get; set; }
        public string Name { get; set; }
        public string ServiceName { get; set; }
        public bool IsIncludeInheritances { get; set; } = true;
        public bool IsEnabledNotifyPropertyChangedBaseClass { get; set; } = true;
        public List<string> Inheritances { get; set; } = new List<string>();
        public List<string> Usings { get; set; } = new List<string>();
        public List<string> IgnoreProperties { get; set; } = new List<string>();
    }
}
