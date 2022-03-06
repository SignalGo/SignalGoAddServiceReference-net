using System.Collections.Generic;

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
        /// <summary>
        /// real name in server
        /// </summary>
        public string RealName { get; set; }
        /// <summary>
        /// customized name in client side
        /// </summary>
        public string Name { get; set; }
        public string DuplicateName { get; set; }
        public string ReturnTypeName { get; set; }
        public ProtocolType ProtocolType { get; set; } = ProtocolType.HttpPost;
        public List<ParameterReferenceInfo> Parameters { get; set; } = new List<ParameterReferenceInfo>();

        public string GetMethodName(bool onlyReal = false)
        {
            if (onlyReal)
            {
                if (string.IsNullOrEmpty(RealName))
                    return Name;
                return RealName;
            }
            return Name;
        }
    }
}
