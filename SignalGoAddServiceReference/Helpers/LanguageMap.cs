using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using SignalGo.CodeGenerator.Helpers;
using SignalGo.CodeGenerator.LanguageMaps;
using SignalGo.CodeGenerator.LanguageMaps.CsharpWebService;
using SignalGo.CodeGenerator.Models;
using SignalGo.Shared.Models.ServiceReference;
using SignalGoAddServiceReference.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace SignalGoAddServiceReference.Helpers
{
    public class LanguageMap : LanguageMapBase
    {
        private static LanguageMap _Current = null;
        public static LanguageMap Current
        {
            get
            {
                if (_Current == null)
                    GetCurrent = _Current = new LanguageMap();
                return _Current;
            }
        }

        public override string DownloadService(string servicePath, AddReferenceConfigInfo config)
        {
            string fullFilePath = "";
            if (config.ServiceType == 0)
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(config.ServiceUrl);
                webRequest.ContentType = "SignalGo Service Reference";
                webRequest.Headers.Add("servicenamespace", config.ServiceNameSpace);
                webRequest.Headers.Add("selectedLanguage", config.LanguageType.ToString());
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

                    //csharp
                    if (config.LanguageType == 0)
                    {
                        fullFilePath = Path.Combine(servicePath, "Reference.cs");
                        File.WriteAllText(fullFilePath, CsharpLanguageMap.CalculateMapData(namespaceReferenceInfo, config), Encoding.UTF8);
                    }
                    //angular
                    else if (config.LanguageType == 1)
                    {
                        string oldPath = Path.Combine(servicePath, "OldAngular");
                        string newPath = Path.Combine(servicePath, "NewAngular");
                        fullFilePath = Path.Combine(oldPath, "Reference.ts");
                        if (!Directory.Exists(oldPath))
                            Directory.CreateDirectory(oldPath);
                        TypeScriptLanguageMap typeScriptLanguageMap = new TypeScriptLanguageMap();
                        File.WriteAllText(fullFilePath, typeScriptLanguageMap.CalculateMapData(oldPath, namespaceReferenceInfo, config.ServiceNameSpace), Encoding.UTF8);

                        AngularTypeScriptLanguageMap angularTypeScriptLanguageMap = new AngularTypeScriptLanguageMap();
                        angularTypeScriptLanguageMap.CalculateMapData(newPath, namespaceReferenceInfo, config.ServiceNameSpace);

                    }
                    //blazor
                    else if (config.LanguageType == 2)
                    {
                        fullFilePath = Path.Combine(servicePath, "Reference.cs");
                        File.WriteAllText(fullFilePath, BlazorLanguageMap.CalculateMapData(namespaceReferenceInfo, config.ServiceNameSpace), Encoding.UTF8);
                    }
                    //java android
                    else if (config.LanguageType == 3)
                    {
                        JavaAndroidLanguageMap javaAndroidLanguageMap = new JavaAndroidLanguageMap();
                        javaAndroidLanguageMap.CalculateMapData(servicePath, namespaceReferenceInfo, config.ServiceNameSpace);
                    }
                    //swift
                    else if (config.LanguageType == 4)
                    {
                        SwiftLanguageMap swiftLanguageMap = new SwiftLanguageMap();
                        swiftLanguageMap.CalculateMapData(servicePath, namespaceReferenceInfo, config.ServiceNameSpace);
                    }
                    //dart
                    else if (config.LanguageType == 5)
                    {
                        DartFlutterLanguageMap dartFlutterLanguageMap = new DartFlutterLanguageMap();
                        dartFlutterLanguageMap.CalculateMapData(servicePath, namespaceReferenceInfo, config.ServiceNameSpace);
                    }
                }
            }
            else
            {
                if (config.LanguageType > 0)
                    throw new NotSupportedException("this language for this type not supported now!");
                XMLToCsharp2 xmlCsharp = new XMLToCsharp2();
                xmlCsharp.Generate(config.ServiceUrl);
                string csharpCode = xmlCsharp.GeneratesharpCode();
                fullFilePath = Path.Combine(servicePath, "Reference.cs");
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"namespace {config.ServiceNameSpace}");
                builder.AppendLine("{");
                builder.AppendLine(csharpCode);
                builder.AppendLine("}");
                File.WriteAllText(fullFilePath, builder.ToString(), Encoding.UTF8);
            }
            return fullFilePath;
        }

        public override ProjectInfoBase GetActiveProject()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            return GetActiveProject(dte);
        }

        public ProjectInfoBase GetActiveProject(DTE dte)
        {
            Project activeProject = null;

            Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                activeProject = activeSolutionProjects.GetValue(0) as Project;
            }

            return new ProjectInfo() { Project = activeProject, ProjectItemsInfoBase = new ProjectItemsInfo() { ProjectItems = activeProject.ProjectItems } };
        }

        public override List<ProjectItemInfoBase> GetAllProjectItemsWithoutServices(ProjectItemsInfoBase projectBase)
        {
            ProjectItemsInfo project = projectBase as ProjectItemsInfo;
            List<ProjectItemInfoBase> result = new List<ProjectItemInfoBase>();
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                result.Add(new ProjectItemInfo() { ProjectItem = projectItem });
                result.AddRange(GetAllProjectItemsWithoutServices(new ProjectItemsInfo() { ProjectItems = projectItem.ProjectItems }));
            }
            return result;
        }

        public override string GetAutoGeneratedText()
        {
            return $@"// AUTO GENERATED
//     This code was generated by signalgo add refenreces.
//     Runtime Version : {AddSignalGoServicePackage.Version}
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//     to download signalgo vsix for visual studio go https://marketplace.visualstudio.com/items?itemName=AliVisualStudio.SignalGoExtension
//     support and use signalgo go https://github.com/SignalGo/SignalGo-full-net
// AUTO GENERATED";
        }
    }
}
