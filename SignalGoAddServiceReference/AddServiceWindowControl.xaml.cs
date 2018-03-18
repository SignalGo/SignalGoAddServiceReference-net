namespace SignalGoAddServiceReference
{
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Linq;
    using Newtonsoft.Json;
    using SignalGo.Shared.Models.ServiceReference;
    using System.Reflection;
    using SignalGoAddServiceReference.Models;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Interaction logic for AddServiceWindowControl.
    /// </summary>
    public partial class AddServiceWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddServiceWindowControl"/> class.
        /// </summary>
        public AddServiceWindowControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        //[SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        //[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        //private void button1_Click(object sender, RoutedEventArgs e)
        //{
        //    MessageBox.Show(
        //        string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
        //        "AddServiceWindow");
        //}
        public static object GetSelectedItem()
        {
            IntPtr hierarchyPointer, selectionContainerPointer;
            object selectedObject = null;
            IVsMultiItemSelect multiItemSelect;
            uint itemId;

            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));

            try
            {
                monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                                 out itemId,
                                                 out multiItemSelect,
                                                 out selectionContainerPointer);

                IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                                                     hierarchyPointer,
                                                     typeof(IVsHierarchy)) as IVsHierarchy;

                if (selectedHierarchy != null)
                {
                    ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out selectedObject));
                }

                Marshal.Release(hierarchyPointer);
                Marshal.Release(selectionContainerPointer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }

            return selectedObject;
        }

        internal static Project GetActiveProject()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            return GetActiveProject(dte);
        }

        internal static Project GetActiveProject(DTE dte)
        {
            Project activeProject = null;

            Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                activeProject = activeSolutionProjects.GetValue(0) as Project;
            }

            return activeProject;
        }

        public Action FinishedAction { get; set; }
        private void btnAddService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var project = GetActiveProject();
                var projectPath = project.FullName;
                string servicesFolder = Path.Combine(Path.GetDirectoryName(projectPath), "Connected Services");
                if (!Directory.Exists(servicesFolder))
                    project.ProjectItems.AddFolder("Connected Services");
                Uri uri = null;
                var serviceNameSpace = txtServiceName.Text.Trim();
                var serviceURI = txtServiceAddress.Text.Trim();
                if (string.IsNullOrEmpty(serviceNameSpace))
                {
                    MessageBox.Show("Please fill your service name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else if (!Uri.TryCreate(serviceURI, UriKind.Absolute, out uri))
                {
                    MessageBox.Show("Service address is not true", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string servicePath = Path.Combine(servicesFolder, Path.GetFileNameWithoutExtension(serviceNameSpace));
                if (!Directory.Exists(servicePath))
                    Directory.CreateDirectory(servicePath);
                var fullFilePath = DownloadService(uri, servicePath, serviceNameSpace);

                StringBuilder text = new StringBuilder();
                text.AppendLine(serviceURI);
                text.AppendLine(serviceNameSpace);
                var signalGoSettingPath = Path.Combine(servicePath, "setting.signalgo");
                File.WriteAllText(signalGoSettingPath, text.ToString(), Encoding.UTF8);

                project.ProjectItems.AddFromFile(fullFilePath);
                FinishedAction?.Invoke();
                MessageBox.Show($"Service {serviceNameSpace} created", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string DownloadService(Uri uri, string servicePath, string serviceNameSpace)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.ContentType = "SignalGo Service Reference";
            webRequest.Headers.Add("servicenamespace", serviceNameSpace);
            var response = webRequest.GetResponse();
            if (response.ContentLength <= 0)
                throw new Exception("Url ContentLength is not set!");
            else if (response.ContentType != "SignalGoServiceType")
                throw new Exception("Url file type is not support!");
            var stream = response.GetResponseStream();

            var fullFilePath = Path.Combine(servicePath, "Reference.cs");
            using (var streamWriter = new MemoryStream())
            {
                streamWriter.SetLength(0);
                var bytes = new byte[1024 * 10];
                while (streamWriter.Length != response.ContentLength)
                {
                    var readCount = stream.Read(bytes, 0, bytes.Length);
                    if (readCount <= 0)
                        break;
                    streamWriter.Write(bytes, 0, readCount);
                }
                var json = Encoding.UTF8.GetString(streamWriter.ToArray());
                //var namespaceReferenceInfo = (NamespaceReferenceInfo)JsonConvert.DeserializeObject(json, typeof(NamespaceReferenceInfo), new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Converters = new List<JsonConverter>() { new DataExchangeConverter(LimitExchangeType.IncomingCall) { Server = null, Client = null, IsEnabledReferenceResolver = true, IsEnabledReferenceResolverForArray = true } }, Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
                var namespaceReferenceInfo = (NamespaceReferenceInfo)JsonConvert.DeserializeObject(json, typeof(NamespaceReferenceInfo), new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });

                File.WriteAllText(fullFilePath, CalculateMapData(namespaceReferenceInfo, serviceNameSpace), Encoding.UTF8);
            }

            return fullFilePath;
        }

        public static string CalculateMapData(NamespaceReferenceInfo namespaceReferenceInfo, string serviceName)
        {
            var project = GetActiveProject();
            List<MapDataClassInfo> MapDataClassInfoes = new List<MapDataClassInfo>();
            List<string> usingsOfClass = new List<string>();
            foreach (ProjectItem projectItem in GetAllProjectItemsWithoutServices(project.ProjectItems))
            {
                if (projectItem.FileCount == 0)
                    continue;
                string fileName = projectItem.FileNames[0];
                if (Path.GetExtension(fileName).ToLower() == ".cs")
                {
                    var dir = Path.GetDirectoryName(fileName);
                    if (File.Exists(Path.Combine(dir, "setting.signalgo")))
                        continue;
                    var fileText = File.ReadAllText(fileName, Encoding.UTF8);
                    if (fileText.Contains("ModelMappAttribute(") || fileText.Contains("ModelMapp("))
                    {
                        using (var streamReader = new StringReader(fileText))
                        {
                            string line = "";
                            bool lineReadClassStarted = false;
                            bool findStartBlock = false;
                            bool canSetBody = false;
                            int findEndBlock = int.MaxValue;

                            MapDataClassInfo mapDataClassInfo = new MapDataClassInfo();
                            StringBuilder builder = new StringBuilder();
                            while ((line = streamReader.ReadLine()) != null)
                            {
                                string lineResult = line;
                                if (lineResult.Trim().StartsWith("using "))
                                {
                                    var uses = GetListOfUsing(lineResult);
                                    mapDataClassInfo.Usings.AddRange(uses);
                                    usingsOfClass.AddRange(uses);
                                }

                                if (findStartBlock && (line.Contains("{") || line.Contains("}")))
                                {
                                    var countPlus = line.Count(x => x == '{') - line.Count(x => x == '}');

                                    if (findEndBlock == int.MaxValue)
                                        findEndBlock = countPlus;
                                    else
                                        findEndBlock += countPlus;

                                    if (findEndBlock <= 0)
                                    {
                                        mapDataClassInfo.Body = builder.ToString();
                                        builder.Clear();
                                        var find = MapDataClassInfoes.FirstOrDefault(x => x.Name == mapDataClassInfo.Name && (usingsOfClass.Contains(serviceName) || x.ServiceName == serviceName));
                                        if (find != null)
                                        {
                                            find.Body += Environment.NewLine + mapDataClassInfo.Body;
                                        }
                                        else
                                            MapDataClassInfoes.Add(mapDataClassInfo);

                                        lineReadClassStarted = false;
                                        findStartBlock = false;
                                        canSetBody = false;
                                        findEndBlock = int.MaxValue;
                                        mapDataClassInfo = new MapDataClassInfo();
                                    }
                                    else
                                    {
                                        if (canSetBody)
                                            builder.AppendLine(lineResult);
                                        canSetBody = true;
                                    }
                                }
                                else if (lineReadClassStarted && line.Contains(" class "))
                                {
                                    var splitInheritance = line.Split(':', ',');
                                    //multiple inheritance
                                    if (splitInheritance.Length > 1)
                                    {
                                        foreach (var item in splitInheritance.Skip(1))
                                        {
                                            var nameSpaceAndName = GetNameSpaceAndName(item);
                                            if (!string.IsNullOrEmpty(nameSpaceAndName.Item1))
                                                usingsOfClass.Add(nameSpaceAndName.Item1);

                                            mapDataClassInfo.Inheritances.Add(nameSpaceAndName.Item2);

                                        }
                                    }
                                    findStartBlock = true;
                                    //builder.AppendLine(lineResult);
                                }
                                else if (!lineResult.TrimStart().StartsWith("//") && (lineResult.Contains("ModelMappAttribute(") || lineResult.Contains("ModelMapp(")))
                                {
                                    int length = "ModelMappAttribute(".Length;
                                    int index = lineResult.IndexOf("ModelMappAttribute(");
                                    if (index == -1)
                                    {
                                        index = lineResult.IndexOf("ModelMapp(");
                                        length = "ModelMapp(".Length;
                                    }


                                    var split = SplitWithIgnoreQuotes(lineResult.Substring(index + length), ",");
                                    foreach (var item in split)
                                    {
                                        if (item.ToLower().Contains("maptotype") || item.Contains("typeof"))
                                        {
                                            var nameSpaceAndName = GetNameSpaceAndName(item.Split('=').LastOrDefault());
                                            if (!string.IsNullOrEmpty(nameSpaceAndName.Item1))
                                            {
                                                usingsOfClass.Add(nameSpaceAndName.Item1);
                                                mapDataClassInfo.ServiceName = nameSpaceAndName.Item1;
                                            }

                                            mapDataClassInfo.Name = nameSpaceAndName.Item2.Replace("typeof", "").Replace("(", "").Replace(")", "")
                                                .Replace("[", "").Replace("]", "").Trim();
                                        }
                                        else if (item.Contains("IsEnabledNotifyPropertyChangedBaseClass"))
                                        {
                                            if (item.Contains("false"))
                                                mapDataClassInfo.IsEnabledNotifyPropertyChangedBaseClass = false;
                                        }
                                        else if (item.Contains("IsIncludeInheritances"))
                                        {
                                            if (item.Contains("false"))
                                                mapDataClassInfo.IsIncludeInheritances = false;
                                        }
                                        else if (item.Contains("IgnoreProperties"))
                                        {
                                            var nameSpaceAndName = GetNameSpaceAndName(item.Split('=').LastOrDefault());
                                            var reg = new Regex("\".*?\"");
                                            var matches = reg.Matches(nameSpaceAndName.Item2);
                                            foreach (var str in matches)
                                            {
                                                mapDataClassInfo.IgnoreProperties.Add(str.ToString().Replace("\"", ""));
                                            }
                                        }
                                    }
                                    lineReadClassStarted = true;
                                }
                                else if (canSetBody)
                                    builder.AppendLine(lineResult);
                            }
                        }
                    }
                }
            }


            StringBuilder builderResult = new StringBuilder();

            foreach (var item in namespaceReferenceInfo.Usings)
            {
                builderResult.AppendLine("using " + item + ";");
            }
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".Services");
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".HttpServices");
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".Models");
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".Callbacks");
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".Enums");

            foreach (var item in usingsOfClass.Where(x => !namespaceReferenceInfo.Usings.Contains(x)).Distinct())
            {
                builderResult.AppendLine("using " + item + ";");
            }
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".Services");
            builderResult.AppendLine("{");
            foreach (var classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ServiceLevel))
            {
                GenerateServiceClass(classInfo, "    ", builderResult, true, "ServiceType.ServerService");
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".HttpServices");
            builderResult.AppendLine("{");
            foreach (var httpClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.HttpServiceLevel))
            {
                GenerateHttpServiceClass(httpClassInfo, "    ", builderResult);
            }

            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".Models");
            builderResult.AppendLine("{");
            foreach (var modelInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ModelLevel))
            {
                GenerateModelClass(modelInfo, "    ", builderResult, MapDataClassInfoes.Where(x => x.Name == modelInfo.Name).FirstOrDefault());

            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");



            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".Callbacks");
            builderResult.AppendLine("{");
            foreach (var callbackInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.CallbackLevel))
            {
                GenerateServiceClass(callbackInfo, "    ", builderResult, false, "ServiceType.ClientService");
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".Enums");
            builderResult.AppendLine("{");
            foreach (var enumInfo in namespaceReferenceInfo.Enums)
            {
                GenerateModelEnum(enumInfo, "    ", builderResult);
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            return builderResult.ToString();
        }

        static void GenerateServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, bool generateAyncMethods,string serviceType)
        {
            string serviceAttribute = $@"[ServiceContract(""{classReferenceInfo.ServiceName}"",{serviceType}, InstanceType.SingleInstance)]";
            builder.AppendLine(prefix + serviceAttribute);
            builder.AppendLine(prefix + "public interface I" + classReferenceInfo.Name);
            builder.AppendLine(prefix + "{");
            foreach (var methodInfo in classReferenceInfo.Methods)
            {
                GenerateMethod(methodInfo, prefix + prefix, builder);
                if (generateAyncMethods)
                    GenerateAsyncMethod(methodInfo, prefix + prefix, builder);
            }

            builder.AppendLine(prefix + "}");
        }


        static void GenerateMethod(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool doSemicolon = true)
        {
            builder.AppendLine($"{prefix}{methodInfo.ReturnTypeName} {methodInfo.Name}({GenerateMethodParameters(methodInfo)}){(doSemicolon ? ";" : "")}");
            //generate empty data
            if (!doSemicolon)
            {
                builder.AppendLine($"{prefix}{{");
                builder.AppendLine($"{prefix + prefix}throw new NotSupportedException();");
                builder.AppendLine($"{prefix}}}");
            }
        }

        static void GenerateAsyncMethod(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool doSemicolon = true)
        {
            string returnType = "Task";
            if (methodInfo.ReturnTypeName != "void")
                returnType = "Task<" + methodInfo.ReturnTypeName + ">";
            builder.AppendLine($"{prefix}{returnType} {methodInfo.Name}Async({GenerateMethodParameters(methodInfo)}){(doSemicolon ? ";" : "")}");
            //generate empty data
            if (!doSemicolon)
            {
                builder.AppendLine($"{prefix}{{");
                if (methodInfo.ReturnTypeName != "void")
                {
                    var result = $"throw new NotSupportedException()";
                    builder.AppendLine($"{prefix + prefix}return System.Threading.Tasks.{returnType}.Factory.StartNew(() => {result});");
                }
                else
                    builder.AppendLine($"{prefix + prefix}return System.Threading.Tasks.{returnType}.Factory.StartNew(() => {{}});");
                builder.AppendLine($"{prefix}}}");
            }
        }


        static void GenerateProperty(PropertyReferenceInfo propertyInfo, string prefix, bool generateOnPropertyChanged, StringBuilder builder)
        {
            //create field
            builder.AppendLine($"{prefix}private {propertyInfo.ReturnTypeName} _{propertyInfo.Name};");
            builder.AppendLine($"{prefix}public {propertyInfo.ReturnTypeName} {propertyInfo.Name}");
            builder.AppendLine($"{prefix}{{");

            builder.AppendLine($"{prefix + prefix}get");
            builder.AppendLine($"{prefix + prefix}{{");
            builder.AppendLine($"{prefix + prefix + prefix}return _{propertyInfo.Name};");
            builder.AppendLine($"{prefix + prefix}}}");

            builder.AppendLine($"{prefix + prefix}set");
            builder.AppendLine($"{prefix + prefix}{{");
            builder.AppendLine($"{prefix + prefix + prefix}_{propertyInfo.Name} = value;");
            if (generateOnPropertyChanged)
                builder.AppendLine($"{prefix + prefix + prefix}OnPropertyChanged(nameof({propertyInfo.Name}));");

            builder.AppendLine($"{prefix + prefix}}}");

            builder.AppendLine($"{prefix}}}");
            builder.AppendLine();
        }

        static string GenerateMethodParameters(MethodReferenceInfo methodInfo)
        {
            StringBuilder builder = new StringBuilder();
            int index = 0;
            foreach (var item in methodInfo.Parameters)
            {
                if (index > 0)
                    builder.Append(", ");
                builder.Append($"{item.TypeName} {item.Name}");
                index++;
            }
            return builder.ToString();
        }

        static void GenerateHttpServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder)
        {
            builder.AppendLine(prefix + "public class " + classReferenceInfo.Name);
            builder.AppendLine(prefix + "{");
            foreach (var methodInfo in classReferenceInfo.Methods)
            {
                GenerateMethod(methodInfo, prefix + prefix, builder, false);
                GenerateAsyncMethod(methodInfo, prefix + prefix, builder, false);
            }
            builder.AppendLine(prefix + "}");
        }

        static void GenerateModelEnum(EnumReferenceInfo enumReferenceInfo, string prefix, StringBuilder builder)
        {
            builder.AppendLine(prefix + "public enum " + enumReferenceInfo.Name + " : " + enumReferenceInfo.TypeName);
            builder.AppendLine(prefix + "{");
            foreach (var name in enumReferenceInfo.KeyValues)
            {
                builder.AppendLine($"{prefix + prefix}{name.Key} = {name.Value},");
            }
            builder.AppendLine(prefix + "}");
            builder.AppendLine();
        }

        static void GenerateModelClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, MapDataClassInfo mapDataClassInfo)
        {
            string baseName = "";
            if (!string.IsNullOrEmpty(classReferenceInfo.BaseClassName))
                baseName = " : " + classReferenceInfo.BaseClassName;
            builder.AppendLine(prefix + "public class " + classReferenceInfo.Name + baseName);
            builder.AppendLine(prefix + "{");
            foreach (var propertyInfo in classReferenceInfo.Properties)
            {
                if (mapDataClassInfo != null && mapDataClassInfo.IgnoreProperties.Contains(propertyInfo.Name))
                    continue;
                GenerateProperty(propertyInfo, prefix + prefix, true, builder);
            }
            if (mapDataClassInfo != null && !string.IsNullOrEmpty(mapDataClassInfo.Body))
                builder.AppendLine(mapDataClassInfo.Body);
            builder.AppendLine();

            builder.AppendLine(prefix + "}");

            builder.AppendLine();
        }

        static string[] SplitWithIgnoreQuotes(string text, string splitText)
        {
            return System.Text.RegularExpressions.Regex.Split(text, splitText + "(?=(?:[^\"|']*[\"|'][^\"|']*[\"|'])*[^\"|']*$)");
        }

        static string[] GetListOfUsing(string text)
        {
            return text.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Replace("using ", "").Trim()).ToArray();
        }

        static Tuple<string, string> GetNameSpaceAndName(string text)
        {
            int lastDotIndex = text.LastIndexOf(".");
            if (lastDotIndex == -1)
                return new Tuple<string, string>("", text);
            string nameSpace = text.Substring(0, lastDotIndex);
            string name = text.Substring(lastDotIndex + 1);
            if (nameSpace.Contains("("))
                nameSpace = nameSpace.Substring(nameSpace.LastIndexOf("(") + 1);
            if (name.Contains(")"))
                name = name.Substring(0, name.IndexOf(")"));

            return new Tuple<string, string>(nameSpace.Trim(), name.Trim());
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