using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGo.Shared.Models.ServiceReference
{
    public enum ProtocolType
    {
        None = 0,
        HttpGet = 1,
        HttpPost = 2
    }

    public class MethodReferenceInfo
    {
        public string Name { get; set; }
        public string DuplicateName { get; set; }
        public string ReturnTypeName { get; set; }
        public ProtocolType ProtocolType { get; set; } = ProtocolType.HttpPost;
        public List<ParameterReferenceInfo> Parameters { get; set; } = new List<ParameterReferenceInfo>();
    }
}
