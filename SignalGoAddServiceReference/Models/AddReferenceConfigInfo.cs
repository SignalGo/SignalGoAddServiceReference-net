using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGoAddServiceReference.Models
{
    public class AddReferenceConfigInfo
    {
        public string ServiceUrl { get; set; }
        public string ServiceNameSpace { get; set; }
        public int LanguageType { get; set; }
        public int ServiceType { get; set; }
    }
}
