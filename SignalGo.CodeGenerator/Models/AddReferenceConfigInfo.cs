using SignalGo.Shared.Models.ServiceReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGo.CodeGenerator.Models
{
    public class AddReferenceConfigInfo
    {
        public string ServiceUrl { get; set; }
        public string ServiceNameSpace { get; set; }
        public int LanguageType { get; set; }
        public int ServiceType { get; set; }
        public bool IsJustGenerateServices { get; set; }
        public bool IsGenerateAsyncMethods { get; set; } = true;
        public bool IsAutomaticSyncAndAsyncDetection { get; set; } = true;

        public string CustomNameSpaces { get; set; }
        public List<ReplaceNameSpaceInfo> ReplaceNameSpaces { get; set; }
        public List<string> SkipAssemblies { get; set; }
    }

    public class PostToServerInfo
    { 
        public List<string> SkipAssemblies { get; set; }
    }
}
