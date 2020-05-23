using SignalGo.CodeGenerator.Helpers;
using SignalGo.CodeGenerator.Models;
using SignalGo.Shared.Models.ServiceReference;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SignalGo.CodeGenerator.LanguageMaps
{
    public static class CsharpLanguageMap
    {
        public static string CalculateMapData(NamespaceReferenceInfo namespaceReferenceInfo, AddReferenceConfigInfo config)
        {
            ProjectInfoBase project = LanguageMapBase.GetCurrent.GetActiveProject();
            List<MapDataClassInfo> MapDataClassInfoes = new List<MapDataClassInfo>();
            List<string> usingsOfClass = new List<string>();
            List<string> attributesForAll = new List<string>();
            foreach (ProjectItemInfoBase projectItem in LanguageMapBase.GetCurrent.GetAllProjectItemsWithoutServices(project.ProjectItemsInfoBase))
            {
                if (projectItem.GetFileCount() == 0)
                    continue;
                string fileName = projectItem.GetFileName(0);
                bool forAllClasses = false;
                List<string> attributes = new List<string>();
                if (Path.GetExtension(fileName).ToLower() == ".cs")
                {
                    string dir = Path.GetDirectoryName(fileName);
                    if (File.Exists(Path.Combine(dir, "setting.signalgo")))
                        continue;
                    string fileText = File.ReadAllText(fileName, Encoding.UTF8);
                    if (fileText.Contains("ModelMappAttribute(") || fileText.Contains("ModelMapp("))
                    {
                        using (StringReader streamReader = new StringReader(fileText))
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
                                if (lineResult.Trim().StartsWith("using ") && lineResult.Trim().EndsWith(";") && !lineResult.Contains("("))
                                {
                                    string[] uses = GetListOfUsing(lineResult);
                                    mapDataClassInfo.Usings.AddRange(uses.Distinct());
                                    usingsOfClass.AddRange(uses);
                                    usingsOfClass = usingsOfClass.Distinct().ToList();
                                }
                                if (line.TrimStart().StartsWith("["))
                                {
                                    if (!line.Contains("ModelMapp"))
                                        attributes.Add(line);
                                    else if (line.Contains("ForAllClassess"))
                                    {
                                        forAllClasses = true;
                                    }
                                }
                                if (findStartBlock && (line.Contains("{") || line.Contains("}")))
                                {
                                    int countPlus = line.Count(x => x == '{') - line.Count(x => x == '}');

                                    if (findEndBlock == int.MaxValue)
                                        findEndBlock = countPlus;
                                    else
                                        findEndBlock += countPlus;

                                    if (findEndBlock <= 0)
                                    {
                                        mapDataClassInfo.Body = builder.ToString();
                                        builder.Clear();
                                        MapDataClassInfo find = MapDataClassInfoes.FirstOrDefault(x => x.Name == mapDataClassInfo.Name && (usingsOfClass.Contains(config.ServiceNameSpace) || x.ServiceName == config.ServiceNameSpace));
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
                                    string[] splitInheritance = line.Split(':', ',');
                                    //multiple inheritance
                                    if (splitInheritance.Length > 1)
                                    {
                                        foreach (string item in splitInheritance.Skip(1))
                                        {
                                            Tuple<string, string> nameSpaceAndName = GetNameSpaceAndName(item);
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


                                    string[] split = SplitWithIgnoreQuotes(lineResult.Substring(index + length), ",");
                                    foreach (string item in split)
                                    {
                                        if (item.ToLower().Contains("maptotype") || item.Contains("typeof"))
                                        {
                                            Tuple<string, string> nameSpaceAndName = GetNameSpaceAndName(item.Split('=').LastOrDefault());
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
                                            Tuple<string, string> nameSpaceAndName = GetNameSpaceAndName(item.Split('=').LastOrDefault());
                                            Regex reg = new Regex("\".*?\"");
                                            MatchCollection matches = reg.Matches(nameSpaceAndName.Item2);
                                            foreach (object str in matches)
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

                if (forAllClasses)
                    attributesForAll.AddRange(attributes);
            }


            StringBuilder builderResult = new StringBuilder();
            builderResult.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".Interfaces");
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".ServerServices");
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".HttpServices");
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".ClientServices");
            namespaceReferenceInfo.Usings.AddRange(usingsOfClass);
            namespaceReferenceInfo.Usings.Add("System");
            var customNameSpaces = LanguageMapBase.GetCustomNameSpaces(config);
            if (customNameSpaces != null)
                namespaceReferenceInfo.Usings.AddRange(customNameSpaces);
            foreach (var item in namespaceReferenceInfo.Classes.ToArray())
            {
                item.NameSpace = LanguageMapBase.ReplaceNameSpace(item.NameSpace, config);
            }
            foreach (var item in namespaceReferenceInfo.Enums.ToArray())
            {
                item.NameSpace = LanguageMapBase.ReplaceNameSpace(item.NameSpace, config);
            }
            foreach (var item in namespaceReferenceInfo.Usings.ToArray())
            {
                var newItem = LanguageMapBase.ReplaceNameSpace(item, config);
                if (newItem != item)
                {
                    namespaceReferenceInfo.Usings.Remove(item);
                    namespaceReferenceInfo.Usings.Add(newItem);
                }
            }
            namespaceReferenceInfo.Usings = namespaceReferenceInfo.Usings.Distinct().ToList();
            foreach (string item in namespaceReferenceInfo.Usings)
            {
                builderResult.AppendLine("using " + item + ";");
            }

            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".Interfaces");
            builderResult.AppendLine("{");
            builderResult.AppendLine("");

            List<string> interfaces = new List<string>();
            foreach (ClassReferenceInfo item in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ServiceLevel || x.Type == ClassReferenceType.StreamLevel))
            {
                if (interfaces.Contains(item.NormalizedName))
                    continue;
                interfaces.Add(item.NormalizedName);
                GenerateServiceInterface(item, "    ", builderResult, config.IsGenerateAsyncMethods, GetServiceType(item.Type, item.NormalizedName), config.IsAutomaticSyncAndAsyncDetection, config, false, false);
                GenerateServiceInterface(item, "    ", builderResult, config.IsGenerateAsyncMethods, GetServiceType(item.Type, item.NormalizedName), config.IsAutomaticSyncAndAsyncDetection, config, false, true);
                GenerateServiceInterface(item, "    ", builderResult, config.IsGenerateAsyncMethods, GetServiceType(item.Type, item.NormalizedName), config.IsAutomaticSyncAndAsyncDetection, config, false, null);
            }
            foreach (ClassReferenceInfo item in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.HttpServiceLevel || x.Type == ClassReferenceType.OneWayLevel))
            {
                if (interfaces.Contains(item.NormalizedName))
                    continue;
                interfaces.Add(item.NormalizedName);
                GenerateServiceInterface(item, "    ", builderResult, config.IsGenerateAsyncMethods, GetServiceType(item.Type, item.NormalizedName), config.IsAutomaticSyncAndAsyncDetection, config, false, false);
                GenerateServiceInterface(item, "    ", builderResult, config.IsGenerateAsyncMethods, GetServiceType(item.Type, item.NormalizedName), config.IsAutomaticSyncAndAsyncDetection, config, false, true);
                GenerateServiceInterface(item, "    ", builderResult, config.IsGenerateAsyncMethods, GetServiceType(item.Type, item.NormalizedName), config.IsAutomaticSyncAndAsyncDetection, config, false, null);
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");

            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".ServerServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ServiceLevel))
            {
                GenerateServiceClass(classInfo, "    ", builderResult, config.IsGenerateAsyncMethods, "ServiceType.ServerService", config.IsAutomaticSyncAndAsyncDetection, config);
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".StreamServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.StreamLevel))
            {
                GenerateStreamServiceClass(classInfo, "    ", builderResult, config.IsGenerateAsyncMethods, "ServiceType.StreamService", config.IsAutomaticSyncAndAsyncDetection, config);
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");

            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".OneWayServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.OneWayLevel))
            {
                GenerateOneWayServiceClass(classInfo, "    ", builderResult, config.IsGenerateAsyncMethods, "ServiceType.OneWayService", config.IsAutomaticSyncAndAsyncDetection, config);
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".HttpServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo httpClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.HttpServiceLevel))
            {
                GenerateHttpServiceClass(httpClassInfo, "    ", config.IsGenerateAsyncMethods, builderResult, config.IsAutomaticSyncAndAsyncDetection, config);
            }

            builderResult.AppendLine("}");
            builderResult.AppendLine("");

            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".ClientServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo callbackInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.CallbackLevel))
            {
                GenerateServiceInterface(callbackInfo, "    ", builderResult, false, "ServiceType.ClientService", config.IsAutomaticSyncAndAsyncDetection, config, false, null);
                GenerateServiceInterface(callbackInfo, "    ", builderResult, false, "ServiceType.ClientService", config.IsAutomaticSyncAndAsyncDetection, config, true, null);
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");

            if (!config.IsJustGenerateServices)
            {
                foreach (IGrouping<string, ClassReferenceInfo> groupInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ModelLevel).GroupBy(x => x.NameSpace))
                {
                    builderResult.AppendLine("namespace " + groupInfo.Key);
                    builderResult.AppendLine("{");
                    foreach (ClassReferenceInfo modelInfo in groupInfo)
                    {
                        GenerateModelClass(modelInfo, "    ", builderResult, MapDataClassInfoes.Where(x => x.Name == modelInfo.NormalizedName && (x.Usings.Contains(modelInfo.NameSpace) || x.ServiceName == modelInfo.NameSpace)).FirstOrDefault(), config, attributesForAll);
                    }
                    builderResult.AppendLine("}");
                    builderResult.AppendLine("");
                }

                foreach (IGrouping<string, EnumReferenceInfo> groupInfo in namespaceReferenceInfo.Enums.GroupBy(x => x.NameSpace))
                {
                    builderResult.AppendLine("namespace " + groupInfo.Key);
                    builderResult.AppendLine("{");
                    foreach (EnumReferenceInfo enumInfo in groupInfo)
                    {
                        GenerateModelEnum(enumInfo, "    ", builderResult);
                    }
                    builderResult.AppendLine("}");
                    builderResult.AppendLine("");
                }
            }

            return builderResult.ToString();
        }

        private static string GetServiceType(ClassReferenceType classReferenceType, string className)
        {
            if (classReferenceType == ClassReferenceType.ServiceLevel)
                return "ServiceType.ServerService";
            else if (classReferenceType == ClassReferenceType.StreamLevel)
                return "ServiceType.StreamService";
            else if (classReferenceType == ClassReferenceType.OneWayLevel)
                return "ServiceType.OneWayService";
            else if (classReferenceType == ClassReferenceType.HttpServiceLevel)
                return "ServiceType.HttpService";
            return "not suport yet!";
        }

        private static void GenerateOneWayServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, bool generateAyncMethods, string serviceType, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            string serviceAttribute = $@"[ServiceContract(""{classReferenceInfo.ServiceName}"",{serviceType}, InstanceType.SingleInstance)]";
            builder.AppendLine(prefix + serviceAttribute);
            builder.AppendLine(prefix + "public partial class " + classReferenceInfo.NormalizedName + $" : I{classReferenceInfo.NormalizedName}");
            builder.AppendLine(prefix + "{");
            builder.AppendLine(prefix + prefix + "public static " + classReferenceInfo.NormalizedName + " Current { get; set; }");

            builder.AppendLine(prefix + prefix + "string _signalGoServerAddress = \"\";");
            builder.AppendLine(prefix + prefix + "int _signalGoPortNumber = 0;");
            builder.AppendLine(prefix + prefix + "public " + classReferenceInfo.NormalizedName + "(string signalGoServerAddress, int signalGoPortNumber)");
            builder.AppendLine(prefix + prefix + "{");
            builder.AppendLine(prefix + prefix + prefix + "_signalGoServerAddress = signalGoServerAddress;");
            builder.AppendLine(prefix + prefix + prefix + "_signalGoPortNumber = signalGoPortNumber;");
            builder.AppendLine(prefix + prefix + "}");

            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                if (isAutoDetection)
                {
                    GenerateOneWayMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, config);
                    if (generateAyncMethods)
                        GenerateOneWayMethodAsync(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, isAutoDetection, config);
                }
                else
                {
                    if (methodInfo.Name.HasEndOfAsync())
                        GenerateOneWayMethodAsync(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, isAutoDetection, config);
                    else
                        GenerateOneWayMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, config);
                }
            }

            builder.AppendLine(prefix + "}");
        }

        //private static void GenerateServerServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, bool generateAyncMethods, string serviceType)
        //{
        //    bool isInterface = classReferenceInfo.Name.StartsWith("I");
        //    string serviceAttribute = $@"[ServiceContract(""{classReferenceInfo.ServiceName}"",{serviceType}, InstanceType.SingleInstance)]";
        //    builder.AppendLine(prefix + serviceAttribute);
        //    builder.AppendLine(prefix + "public interface " + (isInterface ? "" : "I") + classReferenceInfo.Name);
        //    builder.AppendLine(prefix + "{");
        //    foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
        //    {
        //        GenerateMethod(methodInfo, prefix + prefix, builder);
        //        if (generateAyncMethods)
        //            GenerateAsyncMethod(methodInfo, prefix + prefix, builder);
        //    }

        //    builder.AppendLine(prefix + "}");
        //}

        private static void GenerateServiceInterface(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, bool generateAyncMethods, string serviceType, bool isAutoDetection, AddReferenceConfigInfo config, bool justAsync, bool? autoDetectionAsyncClass)
        {
            if (justAsync && autoDetectionAsyncClass.HasValue)
                return;
            if (autoDetectionAsyncClass == null)
            {
                string serviceAttribute = $@"{prefix}[ServiceContract(""{classReferenceInfo.ServiceName}"", {serviceType}, InstanceType.SingleInstance)]";
                builder.AppendLine(serviceAttribute);
            }

            string interfacePrefix;
            if (classReferenceInfo.Type == ClassReferenceType.CallbackLevel)
                interfacePrefix = classReferenceInfo.NormalizedName.StartsWith("i", StringComparison.OrdinalIgnoreCase) ? "" : "I";
            else
                interfacePrefix = classReferenceInfo.NormalizedName.Length > 1 && classReferenceInfo.NormalizedName[1] == 'I' ? "" : "I";
            if (justAsync)
                builder.AppendLine(prefix + $"public partial interface {interfacePrefix}{classReferenceInfo.NormalizedName}Async");
            else
            {
                if (autoDetectionAsyncClass.HasValue)
                {
                    if (autoDetectionAsyncClass.Value)
                        builder.AppendLine(prefix + $"public partial interface {interfacePrefix}{classReferenceInfo.NormalizedName}Async");
                    else
                        builder.AppendLine(prefix + $"public partial interface {interfacePrefix}{classReferenceInfo.NormalizedName}Sync");
                }
                else
                {
                    builder.AppendLine(prefix + $"public partial interface {interfacePrefix}{classReferenceInfo.NormalizedName}: {interfacePrefix}{classReferenceInfo.NormalizedName}Async, {interfacePrefix}{classReferenceInfo.NormalizedName}Sync");
                }
            }
            builder.AppendLine(prefix + "{");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                if (justAsync)
                {
                    GenerateInterfaceMethodAsync(methodInfo, prefix + prefix, builder, config);
                }
                else if (isAutoDetection)
                {
                    if (autoDetectionAsyncClass.HasValue)
                    {
                        if (autoDetectionAsyncClass.Value && generateAyncMethods)
                            GenerateInterfaceMethodAsync(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                        else
                            GenerateInterfaceMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                    }
                    else if (serviceType == "ServiceType.ClientService")
                    {
                        GenerateInterfaceMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                        if (generateAyncMethods)
                            GenerateInterfaceMethodAsync(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                    }
                }
                else
                {
                    if (methodInfo.Name.HasEndOfAsync())
                        GenerateInterfaceMethodAsync(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                    else
                        GenerateInterfaceMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                }
            }

            builder.AppendLine(prefix + "}");
        }

        private static void GenerateStreamServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, bool generateAyncMethods, string serviceType, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            string serviceAttribute = $@"[ServiceContract(""{classReferenceInfo.ServiceName}"",{serviceType}, InstanceType.SingleInstance)]";
            builder.AppendLine(prefix + serviceAttribute);
            builder.AppendLine(prefix + "public partial class " + classReferenceInfo.NormalizedName + $" : I{classReferenceInfo.NormalizedName}");
            builder.AppendLine(prefix + "{");
            builder.AppendLine($@"        public string ServerAddress {{ get; set; }}
        public int? Port {{ get; set; }}
        private string ServiceName {{ get; set; }}

        private SignalGo.Client.ClientProvider CurrentProvider {{ get; set; }}
        public {classReferenceInfo.NormalizedName}(SignalGo.Client.ClientProvider clientProvider = null)
        {{
            CurrentProvider = clientProvider;
            ServiceName = GetType().GetServerServiceName(true);
        }}

        public {classReferenceInfo.NormalizedName}(string serverAddress = null, int? port = null, SignalGo.Client.ClientProvider clientProvider = null)
        {{
            ServerAddress = serverAddress;
            Port = port;
            CurrentProvider = clientProvider;
            ServiceName = GetType().GetServerServiceName(true);
        }}");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                if (isAutoDetection)
                {
                    GenerateStreamMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                    if (generateAyncMethods)
                        GenerateStreamAsyncMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                }
                else
                {
                    if (methodInfo.Name.HasEndOfAsync())
                        GenerateStreamAsyncMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                    else
                        GenerateStreamMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                }

            }

            builder.AppendLine(prefix + "}");
        }

        private static void GenerateServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, bool generateAyncMethods, string serviceType, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            string serviceAttribute = $@"[ServiceContract(""{classReferenceInfo.ServiceName}"",{serviceType}, InstanceType.SingleInstance)]";
            builder.AppendLine(prefix + serviceAttribute);
            builder.AppendLine(prefix + "public partial class " + classReferenceInfo.NormalizedName + $" : I{classReferenceInfo.NormalizedName}");
            builder.AppendLine(prefix + "{");
            builder.AppendLine($@"        private SignalGo.Client.ClientProvider CurrentProvider {{ get; set; }}
        string ServiceName {{ get; set; }}
        public {classReferenceInfo.NormalizedName}(SignalGo.Client.ClientProvider clientProvider)
        {{
            CurrentProvider = clientProvider;
            ServiceName = this.GetType().GetServerServiceName(true);
        }}");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                if (isAutoDetection)
                {
                    GenerateMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                    if (generateAyncMethods)
                        GenerateAsyncMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                }
                else
                {
                    if (methodInfo.Name.HasEndOfAsync())
                        GenerateAsyncMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                    else
                        GenerateMethod(methodInfo, prefix + prefix, builder, isAutoDetection, config);
                }

            }

            builder.AppendLine(prefix + "}");
        }

        private static void GenerateOneWayMethod(MethodReferenceInfo methodInfo, string serviceName, string prefix, StringBuilder builder, AddReferenceConfigInfo config)
        {
            builder.AppendLine($"{prefix}public {ReplaceNameSpace(methodInfo.ReturnTypeName, config)} {methodInfo.Name}({GenerateMethodParameters(methodInfo, config)})");
            builder.AppendLine($"{prefix}{{");
            builder.AppendLine($"{prefix + prefix}return SignalGo.Client.ClientProvider.SendOneWayMethod<{ReplaceNameSpace(methodInfo.ReturnTypeName, config)}>(_signalGoServerAddress, _signalGoPortNumber, \"{serviceName}\", \"{methodInfo.Name}\"{GenerateMethodParametersWitoutTypes(methodInfo)});");
            builder.AppendLine($"{prefix}}}");
        }

        private static void GenerateOneWayMethodAsync(MethodReferenceInfo methodInfo, string serviceName, string prefix, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            string returnType = "Task";
            if (methodInfo.ReturnTypeName != "void")
                returnType = "Task<" + ReplaceNameSpace(methodInfo.ReturnTypeName, config) + ">";
            string asyncName = isAutoDetection ? "Async" : "";
            builder.AppendLine($"{prefix}public {returnType} {methodInfo.Name}{asyncName}({GenerateMethodParameters(methodInfo, config)})");
            builder.AppendLine($"{prefix}{{");
            string result = $"SignalGo.Client.ClientProvider.SendOneWayMethodAsync<{ReplaceNameSpace(methodInfo.ReturnTypeName, config)}>(_signalGoServerAddress, _signalGoPortNumber, \"{serviceName}\", \"{methodInfo.Name}\"{GenerateMethodParametersWitoutTypes(methodInfo)})";

            builder.AppendLine($"{prefix + prefix}return {result};");

            builder.AppendLine($"{prefix}}}");
        }

        private static void GenerateStreamMethod(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            builder.AppendLine($"{prefix}public {ReplaceNameSpace(methodInfo.ReturnTypeName, config)} {methodInfo.Name.RemoveEndOfAsync(isAutoDetection)}({GenerateMethodParameters(methodInfo, config)})");
            builder.AppendLine($"{prefix}{{");
            string returnType = ReplaceNameSpace(methodInfo.ReturnTypeName, config);
            string returnValue = "return ";
            if (returnType == "void")
            {
                returnValue = "";
                returnType = "object";
            }
            builder.AppendLine($"{prefix + prefix}{returnValue} SignalGo.Client.ClientProvider.UploadStreamSync<{returnType}>(CurrentProvider, ServerAddress, Port, ServiceName ,\"{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
            builder.AppendLine($"{prefix + prefix}{{");
            string streamParameter = GenerateHttpMethodParameters(methodInfo, prefix, builder, isAutoDetection, false);
            builder.AppendLine($"{prefix + prefix}}}, {streamParameter});");
            builder.AppendLine($"{prefix}}}");
        }

        private static void GenerateStreamAsyncMethod(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            string returnType = "Task";
            string returnTypeValue = "";
            if (methodInfo.ReturnTypeName != "void")
            {
                returnType = "Task<" + ReplaceNameSpace(methodInfo.ReturnTypeName, config) + ">";
                returnTypeValue = "<" + ReplaceNameSpace(methodInfo.ReturnTypeName, config) + ">";
            }
            string normalReturnType = ReplaceNameSpace(methodInfo.ReturnTypeName, config);
            if (normalReturnType == "void")
            {
                normalReturnType = "object";
            }

            string asyncName = isAutoDetection ? "Async" : "";

            builder.AppendLine($"{prefix}public {returnType} {methodInfo.Name.RemoveEndOfAsync(isAutoDetection)}{asyncName}({GenerateMethodParameters(methodInfo, config)})");
            builder.AppendLine($"{prefix}{{");
            builder.AppendLine($"{prefix + prefix}return SignalGo.Client.ClientProvider.UploadStreamAsync<{normalReturnType}>(CurrentProvider, ServerAddress, Port, ServiceName ,\"{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
            builder.AppendLine($"{prefix + prefix}{{");
            string streamParameter = GenerateHttpMethodParameters(methodInfo, prefix, builder, isAutoDetection, false);
            builder.AppendLine($"{prefix + prefix}}}, {streamParameter});");
            builder.AppendLine($"{prefix}}}");
        }

        private static void GenerateMethod(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            builder.AppendLine($"{prefix}public {ReplaceNameSpace(methodInfo.ReturnTypeName, config)} {methodInfo.Name.RemoveEndOfAsync(isAutoDetection)}({GenerateMethodParameters(methodInfo, config)})");
            builder.AppendLine($"{prefix}{{");
            string returnType = ReplaceNameSpace(methodInfo.ReturnTypeName, config);
            string returnValue = "return ";
            if (returnType == "void")
            {
                returnValue = "";
                returnType = "object";
            }
            builder.AppendLine($"{prefix + prefix}{returnValue} SignalGo.Client.ClientManager.ConnectorExtensions.SendDataSync<{returnType}>(CurrentProvider, ServiceName,\"{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
            builder.AppendLine($"{prefix + prefix}{{");
            GenerateHttpMethodParameters(methodInfo, prefix, builder, isAutoDetection, false);
            builder.AppendLine($"{prefix + prefix}}});");
            builder.AppendLine($"{prefix}}}");
        }

        private static void GenerateInterfaceMethod(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            builder.AppendLine($"{prefix}{ReplaceNameSpace(methodInfo.ReturnTypeName, config)} {methodInfo.Name.RemoveEndOfAsync(isAutoDetection)}({GenerateMethodParameters(methodInfo, config)});");
        }

        private static void GenerateInterfaceMethodAsync(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            string returnType = "Task";
            if (ReplaceNameSpace(methodInfo.ReturnTypeName, config) != "void")
                returnType = "Task<" + ReplaceNameSpace(methodInfo.ReturnTypeName, config) + ">";
            string asyncName = isAutoDetection ? "Async" : "";
            builder.AppendLine($"{prefix}{returnType} {methodInfo.Name.RemoveEndOfAsync(isAutoDetection)}{asyncName}({GenerateMethodParameters(methodInfo, config)});");
        }
        private static void GenerateInterfaceMethodAsync(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, AddReferenceConfigInfo config)
        {
            string returnType = "Task";
            if (ReplaceNameSpace(methodInfo.ReturnTypeName, config) != "void")
                returnType = "Task<" + ReplaceNameSpace(methodInfo.ReturnTypeName, config) + ">";
            builder.AppendLine($"{prefix}{returnType} {methodInfo.Name}({GenerateMethodParameters(methodInfo, config)});");
        }

        private static void GenerateAsyncMethod(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            string returnType = "Task";
            string returnTypeValue = "";
            if (methodInfo.ReturnTypeName != "void")
            {
                returnType = "Task<" + ReplaceNameSpace(methodInfo.ReturnTypeName, config) + ">";
                returnTypeValue = "<" + ReplaceNameSpace(methodInfo.ReturnTypeName, config) + ">";
            }
            string asyncName = isAutoDetection ? "Async" : "";
            builder.AppendLine($"{prefix}public {returnType} {methodInfo.Name.RemoveEndOfAsync(isAutoDetection)}{asyncName}({GenerateMethodParameters(methodInfo, config)})");
            builder.AppendLine($"{prefix}{{");
            builder.AppendLine($"{prefix + prefix}return SignalGo.Client.ClientManager.ConnectorExtensions.SendDataAsync{returnTypeValue}(CurrentProvider, ServiceName,\"{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
            builder.AppendLine($"{prefix + prefix}{{");
            GenerateHttpMethodParameters(methodInfo, prefix, builder, isAutoDetection, false);
            builder.AppendLine($"{prefix + prefix}}});");
            builder.AppendLine($"{prefix}}}");
        }


        private static void GenerateHttpMethod(MethodReferenceInfo methodInfo, string serviceName, string prefix, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config, bool doSemicolon = true)
        {
            builder.AppendLine($"{prefix}public {ReplaceNameSpace(methodInfo.ReturnTypeName, config)} {methodInfo.Name.RemoveEndOfAsync(isAutoDetection)}({GenerateMethodParameters(methodInfo, config)}){(doSemicolon ? ";" : "")}");
            bool isStream = methodInfo.ReturnTypeName.StartsWith("SignalGo.Shared.Models.StreamInfo");
            //generate empty data
            if (!doSemicolon)
            {
                string uploadParameter = "";
                if (methodInfo.Parameters.Any(x => x.TypeName.StartsWith("SignalGo.Shared.Models.StreamInfo")))
                {
                    uploadParameter = $", {methodInfo.Parameters.Where(x => x.TypeName.StartsWith("SignalGo.Shared.Models.StreamInfo")).Select(x => x.Name).FirstOrDefault()}";
                }
                builder.AppendLine($"{prefix}{{");
                if (isStream)
                    builder.AppendLine($"{prefix + prefix}SignalGo.Client.HttpClientResponseBase result = _httpClient.PostHead(_serverUrl + (_serverUrl.EndsWith(\"/\") ? \"\" : \"{"/"}\") + \"{serviceName}/{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
                else
                    builder.AppendLine($"{prefix + prefix}SignalGo.Client.HttpClientResponse result = _httpClient.Post(_serverUrl + (_serverUrl.EndsWith(\"/\") ? \"\" : \"{"/"}\") + \"{serviceName}/{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
                builder.AppendLine($"{prefix + prefix}{{");
                GenerateHttpMethodParameters(methodInfo, prefix, builder, doSemicolon);
                builder.AppendLine($"{prefix + prefix}}}{uploadParameter});");
                builder.AppendLine($"{prefix + prefix}ResponseHeaders = result.ResponseHeaders;");
                builder.AppendLine($"{prefix + prefix}Status = result.Status;");
                if (!isStream)
                {
                    builder.AppendLine($"{prefix + prefix}if (Status == System.Net.HttpStatusCode.InternalServerError)");
                    builder.AppendLine($"{prefix + prefix + prefix}throw new Exception(result.Data);");
                    if (methodInfo.ReturnTypeName != "void")
                        builder.AppendLine($"{prefix + prefix}return _httpClient.Deserialize<{ReplaceNameSpace(methodInfo.ReturnTypeName, config)}>(result.Data);");
                }
                else
                    builder.AppendLine($"{prefix + prefix}return result.GetStream<{ReplaceNameSpace(methodInfo.ReturnTypeName, config)}>();");

                builder.AppendLine($"{prefix}}}");
            }
        }

        private static void GenerateHttpAsyncMethod(MethodReferenceInfo methodInfo, string serviceName, string prefix, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config, bool doSemicolon = true)
        {
            string returnType = "public async Task";
            if (methodInfo.ReturnTypeName != "void")
                returnType = "public async Task<" + ReplaceNameSpace(methodInfo.ReturnTypeName, config) + ">";
            string asyncName = isAutoDetection ? "Async" : "";
            bool isStream = methodInfo.ReturnTypeName.StartsWith("SignalGo.Shared.Models.StreamInfo");
            builder.AppendLine($"{prefix}{returnType} {methodInfo.Name.RemoveEndOfAsync(isAutoDetection)}{asyncName}({GenerateMethodParameters(methodInfo, config)}){(doSemicolon ? ";" : "")}");
            //generate empty data
            if (!doSemicolon)
            {
                string uploadParameter = "";
                if (methodInfo.Parameters.Any(x => x.TypeName.StartsWith("SignalGo.Shared.Models.StreamInfo")))
                {
                    uploadParameter = $", {methodInfo.Parameters.Where(x => x.TypeName.StartsWith("SignalGo.Shared.Models.StreamInfo")).Select(x => x.Name).FirstOrDefault()}";
                }
                builder.AppendLine($"{prefix}{{");
                if (isStream)
                    builder.AppendLine($"{prefix + prefix}SignalGo.Client.HttpClientResponseBase result = await _httpClient.PostHeadAsync(_serverUrl + (_serverUrl.EndsWith(\"/\") ? \"\" : \"{"/"}\") + \"{serviceName}/{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
                else
                    builder.AppendLine($"{prefix + prefix}SignalGo.Client.HttpClientResponse result = await _httpClient.PostAsync(_serverUrl + (_serverUrl.EndsWith(\"/\") ? \"\" : \"{"/"}\") + \"{serviceName}/{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
                builder.AppendLine($"{prefix + prefix}{{");
                GenerateHttpMethodParameters(methodInfo, prefix, builder, doSemicolon);
                builder.AppendLine($"{prefix + prefix}}}{uploadParameter});");
                builder.AppendLine($"{prefix + prefix}ResponseHeaders = result.ResponseHeaders;");
                builder.AppendLine($"{prefix + prefix}Status = result.Status;");
                if (!isStream)
                {
                    builder.AppendLine($"{prefix + prefix}if (Status == System.Net.HttpStatusCode.InternalServerError)");
                    builder.AppendLine($"{prefix + prefix + prefix}throw new Exception(result.Data);");
                    if (methodInfo.ReturnTypeName != "void")
                        builder.AppendLine($"{prefix + prefix}return _httpClient.Deserialize<{ReplaceNameSpace(methodInfo.ReturnTypeName, config)}>(result.Data);");
                }
                else
                    builder.AppendLine($"{prefix + prefix}return result.GetStream<{ReplaceNameSpace(methodInfo.ReturnTypeName, config)}>();");

                builder.AppendLine($"{prefix}}}");
            }
        }


        private static void GenerateProperty(PropertyReferenceInfo propertyInfo, string prefix, bool generateOnPropertyChanged, StringBuilder builder, AddReferenceConfigInfo config)
        {
            //create field
            builder.AppendLine($"{prefix}private {ReplaceNameSpace(propertyInfo.ReturnTypeName, config)} _{propertyInfo.Name};");
            builder.AppendLine($"{prefix}public {ReplaceNameSpace(propertyInfo.ReturnTypeName, config)} {propertyInfo.Name}");
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



        private static string GenerateHttpMethodParameters(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool isAutoDetection, bool doSemicolon = true)
        {
            string streamParameterName = "null";
            foreach (ParameterReferenceInfo parameter in methodInfo.Parameters)
            {
                builder.AppendLine($"{prefix + prefix + prefix} new  SignalGo.Shared.Models.ParameterInfo() {{ Name = nameof({parameter.Name}),Value = SignalGo.Client.ClientSerializationHelper.SerializeObject({parameter.Name}) }},");
                if (parameter.TypeName.StartsWith("SignalGo.Shared.Models.StreamInfo"))
                    streamParameterName = parameter.Name;
            }
            return streamParameterName;
        }
        private static string GenerateMethodParametersWitoutTypes(MethodReferenceInfo methodInfo)
        {
            StringBuilder builder = new StringBuilder();
            foreach (ParameterReferenceInfo item in methodInfo.Parameters)
            {
                builder.Append(", ");
                builder.Append($"new SignalGo.Shared.Models.ParameterInfo() {{  Name = \"{item.Name}\", Value = SignalGo.Client.ClientSerializationHelper.SerializeObject({item.Name}) }}");
            }
            return builder.ToString();
        }

        private static string GenerateMethodParameters(MethodReferenceInfo methodInfo, AddReferenceConfigInfo config)
        {
            StringBuilder builder = new StringBuilder();
            int index = 0;
            foreach (ParameterReferenceInfo item in methodInfo.Parameters)
            {
                if (index > 0)
                    builder.Append(", ");
                builder.Append($"{ReplaceNameSpace(item.TypeName, config)} {item.Name}");
                index++;
            }
            return builder.ToString();
        }

        static string ReplaceNameSpace(string typeName, AddReferenceConfigInfo config)
        {
            if (!typeName.Contains("."))
                return typeName;
            var full = GenericInfo.GenerateGeneric(typeName, GenericNumbericTemeplateType.Skip);
            full.ReplaceNameSpaces(LanguageMapBase.ReplaceNameSpace, config);

            return full.ToString();
        }

        private static void GenerateHttpServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, bool generateAyncMethods, StringBuilder builder, bool isAutoDetection, AddReferenceConfigInfo config)
        {
            builder.AppendLine(prefix + "public partial class " + classReferenceInfo.NormalizedName + $" : I{classReferenceInfo.NormalizedName}");
            builder.AppendLine(prefix + "{");
            builder.AppendLine("        public " + classReferenceInfo.NormalizedName + @"(string serverUrl, SignalGo.Client.IHttpClient httpClient = null)
        {
            _serverUrl = serverUrl;
            _httpClient = httpClient;
            if (_httpClient == null)
                _httpClient = new SignalGo.Client.HttpClient();
        }

        private readonly string _serverUrl = null;
        private SignalGo.Client.IHttpClient _httpClient;
        public SignalGo.Shared.Http.WebHeaderCollection RequestHeaders
        {
            get
            {
                return _httpClient.RequestHeaders;
            }
            set
            {
                _httpClient.RequestHeaders = value;
            }
        }

        public SignalGo.Shared.Http.WebHeaderCollection ResponseHeaders { get; set; }
        public System.Net.HttpStatusCode Status { get; set; }
        public static " + classReferenceInfo.NormalizedName + " Current { get; set; }");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                if (isAutoDetection)
                {
                    GenerateHttpMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, isAutoDetection, config, false);
                    if (generateAyncMethods)
                        GenerateHttpAsyncMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, isAutoDetection, config, false);
                }
                else
                {
                    if (methodInfo.Name.HasEndOfAsync())
                        GenerateHttpAsyncMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, isAutoDetection, config, false);
                    else
                        GenerateHttpMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, isAutoDetection, config, false);
                }

            }
            builder.AppendLine(prefix + "}");
        }

        public static void GenerateModelEnum(EnumReferenceInfo enumReferenceInfo, string prefix, StringBuilder builder)
        {
            builder.AppendLine(prefix + "public enum " + enumReferenceInfo.Name + " : " + enumReferenceInfo.TypeName);
            builder.AppendLine(prefix + "{");
            foreach (SignalGo.Shared.Models.KeyValue<string, string> name in enumReferenceInfo.KeyValues)
            {
                builder.AppendLine($"{prefix + prefix}{name.Key} = {name.Value},");
            }
            builder.AppendLine(prefix + "}");
            builder.AppendLine();
        }

        public static void GenerateModelClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, MapDataClassInfo mapDataClassInfo, AddReferenceConfigInfo config, List<string> attributes)
        {
            string baseName = "";
            if (!string.IsNullOrEmpty(classReferenceInfo.BaseClassName))
                baseName = " : " + ReplaceNameSpace(classReferenceInfo.BaseClassName, config);
            foreach (var item in attributes)
            {
                builder.AppendLine(item);
            }
            builder.AppendLine(prefix + "public partial class " + classReferenceInfo.NormalizedName + baseName);
            builder.AppendLine(prefix + "{");
            foreach (PropertyReferenceInfo propertyInfo in classReferenceInfo.Properties)
            {
                if (mapDataClassInfo != null && mapDataClassInfo.IgnoreProperties.Contains(propertyInfo.Name))
                    continue;
                GenerateProperty(propertyInfo, prefix + prefix, true, builder, config);
            }
            if (mapDataClassInfo != null && !string.IsNullOrEmpty(mapDataClassInfo.Body))
                builder.AppendLine(mapDataClassInfo.Body);
            builder.AppendLine();

            builder.AppendLine(prefix + "}");

            builder.AppendLine();
        }

        public static string[] SplitWithIgnoreQuotes(string text, string splitText)
        {
            return System.Text.RegularExpressions.Regex.Split(text, splitText + "(?=(?:[^\"{}|']*[\"{}|'][^\"{}|']*[\"{}|'])*[^\"{}|']*$)");//(?=(?:[^\"|']*[\"|'][^\"|']*[\"|'])*[^\"|']*$)
        }

        public static string[] GetListOfUsing(string text)
        {
            return text.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Replace("using ", "").Trim()).ToArray();
        }

        public static Tuple<string, string> GetNameSpaceAndName(string text)
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
    }
}
