using EnvDTE;
using SignalGo.CodeGenerator.Helpers;

namespace SignalGoAddReferenceShared.Models
{
    public class ProjectItemInfo : ProjectItemInfoBase
    {
        public ProjectItem ProjectItem { get; set; }
        public override int GetFileCount()
        {
            return ProjectItem.FileCount;
        }

        public override string GetFileName(short index)
        {
            return ProjectItem.FileNames[index];
        }
    }
}
