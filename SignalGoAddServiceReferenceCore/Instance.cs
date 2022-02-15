using Microsoft.VisualStudio.ConnectedServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGoAddServiceReferenceCore
{
    internal class Instance : ConnectedServiceInstance
    {
        #region Constructors

        public Instance()
        {
            InstanceId = Constants.ExtensionCategory;
            Name = Constants.ExtensionName;
        }

        #endregion
    }
}
