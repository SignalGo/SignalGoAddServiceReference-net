using EnvDTE;
using SignalGo.CodeGenerator.Helpers;

namespace SignalGoAddReferenceShared.Models
{
    public class ProjectInfo : ProjectInfoBase
    {
        public Project Project { get; set; }
    }
}
