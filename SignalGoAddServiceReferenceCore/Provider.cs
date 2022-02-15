using Microsoft.VisualStudio.ConnectedServices;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SignalGoAddServiceReferenceCore
{
    [ConnectedServiceProviderExport(Constants.ProviderId, SupportsUpdate = true)]
    internal class Provider : ConnectedServiceProvider
    {
        #region Constructors

        public Provider()
        {
            Category = Constants.ExtensionCategory;
            Name = Constants.ExtensionName;
            Description = Constants.ExtensionDescription;
            SupportsUpdate = true;
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SignalGoAddServiceReferenceCore.Resources.icon.png");
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            Icon = bitmapImage;
            CreatedBy = Constants.Author;
            Version = typeof(Provider).Assembly.GetName().Version;
            MoreInfoUri = new Uri(Constants.Website);
        }

        #endregion

        #region Methods

        public override Task<ConnectedServiceConfigurator> CreateConfiguratorAsync(ConnectedServiceProviderContext context)
        {
            return Task.FromResult<ConnectedServiceConfigurator>(new Wizard(context));
        }

        public override IEnumerable<Tuple<string, Uri>> GetSupportedTechnologyLinks()
        {
            yield return Tuple.Create("SignalGo source", new Uri("https://github.com/SignalGo/SignalGo-full-net"));
            yield return Tuple.Create("Ali Yousefi", new Uri("https://github.com/Ali-YousefiTelori"));
        }

        #endregion
    }
}
