using EnvDTE;
using SignalGo.CodeGenerator.Helpers;

namespace SignalGoAddServiceReference.Models
{
    public class ProjectInfo : ProjectInfoBase
    {
        public Project Project { get; set; }
    }
}
