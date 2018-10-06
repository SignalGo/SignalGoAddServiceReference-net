using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using SignalGo.Shared.Models.ServiceReference;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace SignalGoAddServiceReference.LanguageMaps
{
    public static class BaseLanguageMap
    {
        public static string DownloadService(string uri, string servicePath, string serviceNameSpace, int selectedLanguage, int serviceType)
        {
            string fullFilePath = "";
            if (serviceType == 0)
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.ContentType = "SignalGo Service Reference";
                webRequest.Headers.Add("servicenamespace", serviceNameSpace);
                webRequest.Headers.Add("selectedLanguage", selectedLanguage.ToString());
                WebResponse response = webRequest.GetResponse();
                if (response.ContentLength <= 0)
                    throw new Exception("Url ContentLength is not set!");
                else if (response.Headers["Service-Type"] == null || response.Headers["Service-Type"] != "SignalGoServiceType")
                    throw new Exception("Url file type is not support!");
                Stream stream = response.GetResponseStream();

                using (MemoryStream streamWriter = new MemoryStream())
                {
                    streamWriter.SetLength(0);
                    byte[] bytes = new byte[1024 * 10];
                    while (streamWriter.Length != response.ContentLength)
                    {
                        int readCount = stream.Read(bytes, 0, bytes.Length);
                        if (readCount <= 0)
                            break;
                        streamWriter.Write(bytes, 0, readCount);
                    }
                    string json = Encoding.UTF8.GetString(streamWriter.ToArray());
                    //var namespaceReferenceInfo = (NamespaceReferenceInfo)JsonConvert.DeserializeObject(json, typeof(NamespaceReferenceInfo), new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Converters = new List<JsonConverter>() { new DataExchangeConverter(LimitExchangeType.IncomingCall) { Server = null, Client = null, IsEnabledReferenceResolver = true, IsEnabledReferenceResolverForArray = true } }, Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
                    NamespaceReferenceInfo namespaceReferenceInfo = (NamespaceReferenceInfo)JsonConvert.DeserializeObject(json, typeof(NamespaceReferenceInfo), new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });

                    if (selectedLanguage == 0)
                    {
                        fullFilePath = Path.Combine(servicePath, "Reference.cs");
                        File.WriteAllText(fullFilePath, CsharpLanguageMap.CalculateMapData(namespaceReferenceInfo, serviceNameSpace), Encoding.UTF8);
                    }
                    else if (selectedLanguage == 1)
                    {
                        fullFilePath = Path.Combine(servicePath, "Reference.ts");
                        File.WriteAllText(fullFilePath, TypeScriptLanguageMap.CalculateMapData(servicePath, namespaceReferenceInfo, serviceNameSpace), Encoding.UTF8);
                    }
                }
            }
            else
            {
                if (selectedLanguage > 0)
                    throw new NotSupportedException("this language for this type not supported now!");
                CsharpWebService.XMLToCsharp xmlCsharp = new CsharpWebService.XMLToCsharp();
                xmlCsharp.Generate(uri);
                string csharpCode = xmlCsharp.GeneratesharpCode();
                fullFilePath = Path.Combine(servicePath, "Reference.cs");
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"namespace {serviceNameSpace}");
                builder.AppendLine("{");
                builder.AppendLine(csharpCode);
                builder.AppendLine("}");
                File.WriteAllText(fullFilePath, builder.ToString(), Encoding.UTF8);
            }
            return fullFilePath;
        }

        public static Project GetActiveProject()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            return GetActiveProject(dte);
        }

        public static Project GetActiveProject(DTE dte)
        {
            Project activeProject = null;

            Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                activeProject = activeSolutionProjects.GetValue(0) as Project;
            }

            return activeProject;
        }

        public static List<ProjectItem> GetAllProjectItemsWithoutServices(ProjectItems items)
        {
            List<ProjectItem> result = new List<ProjectItem>();
            foreach (ProjectItem projectItem in items)
            {
                result.Add(projectItem);
                result.AddRange(GetAllProjectItemsWithoutServices(projectItem.ProjectItems));
            }
            return result;
        }
    }
}
