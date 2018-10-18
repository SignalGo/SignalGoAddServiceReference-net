using EnvDTE;
using SignalGo.Shared.Models.ServiceReference;
using SignalGoAddServiceReference.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SignalGoAddServiceReference.LanguageMaps
{
    public static class CsharpLanguageMap
    {
        public static string CalculateMapData(NamespaceReferenceInfo namespaceReferenceInfo, string serviceName)
        {
            Project project = BaseLanguageMap.GetActiveProject();
            List<MapDataClassInfo> MapDataClassInfoes = new List<MapDataClassInfo>();
            List<string> usingsOfClass = new List<string>();
            foreach (ProjectItem projectItem in BaseLanguageMap.GetAllProjectItemsWithoutServices(project.ProjectItems))
            {
                if (projectItem.FileCount == 0)
                    continue;
                string fileName = projectItem.FileNames[0];
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
                                    mapDataClassInfo.Usings.AddRange(uses);
                                    usingsOfClass.AddRange(uses);
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
                                        MapDataClassInfo find = MapDataClassInfoes.FirstOrDefault(x => x.Name == mapDataClassInfo.Name && (usingsOfClass.Contains(serviceName) || x.ServiceName == serviceName));
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
            }


            StringBuilder builderResult = new StringBuilder();
            builderResult.AppendLine(BaseLanguageMap.GetAutoGeneratedText());
            foreach (string item in namespaceReferenceInfo.Usings)
            {
                builderResult.AppendLine("using " + item + ";");
            }
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".ServerServices");
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".HttpServices");
            //usingsOfClass.Add(namespaceReferenceInfo.Name + ".Models");
            usingsOfClass.Add(namespaceReferenceInfo.Name + ".ClientServices");
            //usingsOfClass.Add(namespaceReferenceInfo.Name + ".Enums");

            foreach (string item in usingsOfClass.Where(x => !namespaceReferenceInfo.Usings.Contains(x)).Distinct())
            {
                builderResult.AppendLine("using " + item + ";");
            }
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".ServerServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ServiceLevel))
            {
                GenerateServiceClass(classInfo, "    ", builderResult, true, "ServiceType.ServerService");
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".StreamServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.StreamLevel))
            {
                GenerateServiceClass(classInfo, "    ", builderResult, true, "ServiceType.StreamService");
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");

            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".OneWayServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.OneWayLevel))
            {
                GenerateOneWayServiceClass(classInfo, "    ", builderResult, true, "ServiceType.OneWayService");
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".HttpServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo httpClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.HttpServiceLevel))
            {
                GenerateHttpServiceClass(httpClassInfo, "    ", builderResult);
            }

            builderResult.AppendLine("}");
            builderResult.AppendLine("");

            //Dictionary<string, string> AddedModels = new Dictionary<string, string>();
            //Dictionary<string, List<ClassReferenceInfo>> NeedToAddModels = new Dictionary<string, List<ClassReferenceInfo>>();

            foreach (IGrouping<string, ClassReferenceInfo> groupInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ModelLevel).GroupBy(x => x.NameSpace))
            {
                builderResult.AppendLine("namespace " + groupInfo.Key);
                builderResult.AppendLine("{");
                foreach (ClassReferenceInfo modelInfo in groupInfo)
                {
                    GenerateModelClass(modelInfo, "    ", builderResult, MapDataClassInfoes.Where(x => x.Name == modelInfo.Name).FirstOrDefault());
                }
                builderResult.AppendLine("}");
                builderResult.AppendLine("");
                //if (AddedModels.ContainsKey(modelInfo.Name))
                //{
                //    var find = NeedToAddModels.Where(x => !x.Value.Any(y => y.Name == modelInfo.Name)).Select(x => x.Value).FirstOrDefault();
                //    if (find != null)
                //        find.Add(modelInfo);
                //    else
                //    {
                //        var list = new List<ClassReferenceInfo>();
                //        NeedToAddModels.Add(nameSpaceName + (NeedToAddModels.Count + 2), list);
                //        list.Add(modelInfo);
                //    }
                //}
                //else
                //{
                //    AddedModels.Add(modelInfo.Name, nameSpaceName);
                //}
            }
            //builderResult.AppendLine("}");
            //builderResult.AppendLine("");

            //add duplicate models name to another name spaces
            //foreach (var item in NeedToAddModels)
            //{
            //    nameSpaceName = item.Key;
            //    builderResult.AppendLine(nameSpaceName);
            //    builderResult.AppendLine("{");
            //    foreach (var modelInfo in item.Value)
            //    {
            //        GenerateModelClass(modelInfo, "    ", builderResult, MapDataClassInfoes.Where(x => x.Name == modelInfo.Name).FirstOrDefault());
            //    }
            //    builderResult.AppendLine("}");
            //    builderResult.AppendLine();
            //}


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".ClientServices");
            builderResult.AppendLine("{");
            foreach (ClassReferenceInfo callbackInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.CallbackLevel))
            {
                GenerateServiceClass(callbackInfo, "    ", builderResult, false, "ServiceType.ClientService");
            }
            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            //builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".Enums");
            //builderResult.AppendLine("{");
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
            //builderResult.AppendLine("}");
            //builderResult.AppendLine("");


            return builderResult.ToString();
        }

        private static void GenerateOneWayServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, bool generateAyncMethods, string serviceType)
        {
            string serviceAttribute = $@"[ServiceContract(""{classReferenceInfo.ServiceName}"",{serviceType}, InstanceType.SingleInstance)]";
            builder.AppendLine(prefix + serviceAttribute);
            builder.AppendLine(prefix + "public class " + classReferenceInfo.Name);
            builder.AppendLine(prefix + "{");
            builder.AppendLine(prefix + prefix + "public static " + classReferenceInfo.Name + " Current { get; set; }");

            builder.AppendLine(prefix + prefix + "string _signalGoServerAddress = \"\";");
            builder.AppendLine(prefix + prefix + "int _signalGoPortNumber = 0;");
            builder.AppendLine(prefix + prefix + "public " + classReferenceInfo.Name + "(string signalGoServerAddress, int signalGoPortNumber)");
            builder.AppendLine(prefix + prefix + "{");
            builder.AppendLine(prefix + prefix + prefix + "_signalGoServerAddress = signalGoServerAddress;");
            builder.AppendLine(prefix + prefix + prefix + "_signalGoPortNumber = signalGoPortNumber;");
            builder.AppendLine(prefix + prefix + "}");

            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateOneWayMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder);
                if (generateAyncMethods)
                    GenerateOneWayMethodAsync(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder);
            }

            builder.AppendLine(prefix + "}");
        }

        private static void GenerateServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, bool generateAyncMethods, string serviceType)
        {
            bool isInterface = classReferenceInfo.Name.StartsWith("I");
            string serviceAttribute = $@"[ServiceContract(""{classReferenceInfo.ServiceName}"",{serviceType}, InstanceType.SingleInstance)]";
            builder.AppendLine(prefix + serviceAttribute);
            builder.AppendLine(prefix + "public interface " + (isInterface ? "" : "I") + classReferenceInfo.Name);
            builder.AppendLine(prefix + "{");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateMethod(methodInfo, prefix + prefix, builder);
                if (generateAyncMethods)
                    GenerateAsyncMethod(methodInfo, prefix + prefix, builder);
            }

            builder.AppendLine(prefix + "}");
        }

        private static void GenerateOneWayMethod(MethodReferenceInfo methodInfo, string serviceName, string prefix, StringBuilder builder)
        {
            builder.AppendLine($"{prefix} public {methodInfo.ReturnTypeName} {methodInfo.Name}({GenerateMethodParameters(methodInfo)})");
            builder.AppendLine($"{prefix}{{");
            builder.AppendLine($"{prefix + prefix}return SignalGo.Client.ClientProvider.SendOneWayMethod<{methodInfo.ReturnTypeName}>(_signalGoServerAddress, _signalGoPortNumber, \"{serviceName}\", \"{methodInfo.Name}\"{GenerateMethodParametersWitoutTypes(methodInfo)});");
            builder.AppendLine($"{prefix}}}");
        }

        private static void GenerateOneWayMethodAsync(MethodReferenceInfo methodInfo, string serviceName, string prefix, StringBuilder builder)
        {
            string returnType = "Task";
            if (methodInfo.ReturnTypeName != "void")
                returnType = "Task<" + methodInfo.ReturnTypeName + ">";

            builder.AppendLine($"{prefix} public {returnType} {methodInfo.Name}Async({GenerateMethodParameters(methodInfo)})");
            builder.AppendLine($"{prefix}{{");
            string result = $"SignalGo.Client.ClientProvider.SendOneWayMethodAsync<{methodInfo.ReturnTypeName}>(_signalGoServerAddress, _signalGoPortNumber, \"{serviceName}\", \"{methodInfo.Name}\"{GenerateMethodParametersWitoutTypes(methodInfo)})";

            builder.AppendLine($"{prefix + prefix}return {result};");

            builder.AppendLine($"{prefix}}}");
        }

        private static void GenerateMethod(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool doSemicolon = true)
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

        private static void GenerateAsyncMethod(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool doSemicolon = true)
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
                    string result = $"throw new NotSupportedException()";
                    builder.AppendLine($"{prefix + prefix}return System.Threading.Tasks.{returnType}.Factory.StartNew(() => {result});");
                }
                else
                    builder.AppendLine($"{prefix + prefix}return System.Threading.Tasks.{returnType}.Factory.StartNew(() => {{}});");
                builder.AppendLine($"{prefix}}}");
            }
        }

        private static void GenerateProperty(PropertyReferenceInfo propertyInfo, string prefix, bool generateOnPropertyChanged, StringBuilder builder)
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



        private static void GenerateHttpMethod(MethodReferenceInfo methodInfo, string serviceName, string prefix, StringBuilder builder, bool doSemicolon = true)
        {
            builder.AppendLine($"{prefix}public {methodInfo.ReturnTypeName} {methodInfo.Name}({GenerateMethodParameters(methodInfo)}){(doSemicolon ? ";" : "")}");
            //generate empty data
            if (!doSemicolon)
            {
                builder.AppendLine($"{prefix}{{");
                builder.AppendLine($"{prefix + prefix}SignalGo.Client.HttpClientResponse result = _httpClient.Post(_serverUrl + (_serverUrl.EndsWith(\"/\") ? \"\" : \"{"/"}\") + \"{serviceName}/{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
                builder.AppendLine($"{prefix + prefix}{{");
                GenerateHttpMethodParameters(methodInfo, prefix, builder, doSemicolon);
                builder.AppendLine($"{prefix + prefix}}});");
                builder.AppendLine($"{prefix + prefix}ResponseHeaders = result.ResponseHeaders;");
                builder.AppendLine($"{prefix + prefix}Status = result.Status;");
                if (methodInfo.ReturnTypeName != "void")
                    builder.AppendLine($"{prefix + prefix}return SignalGo.Client.ClientSerializationHelper.DeserializeObject<{methodInfo.ReturnTypeName}>(result.Data);");
                builder.AppendLine($"{prefix}}}");
            }
        }

        private static void GenerateHttpAsyncMethod(MethodReferenceInfo methodInfo,string serviceName, string prefix, StringBuilder builder, bool doSemicolon = true)
        {
            string returnType = "public async Task";
            if (methodInfo.ReturnTypeName != "void")
                returnType = "public async Task<" + methodInfo.ReturnTypeName + ">";
            builder.AppendLine($"{prefix}{returnType} {methodInfo.Name}Async({GenerateMethodParameters(methodInfo)}){(doSemicolon ? ";" : "")}");
            //generate empty data
            if (!doSemicolon)
            {
                builder.AppendLine($"{prefix}{{");
                builder.AppendLine($"{prefix + prefix}SignalGo.Client.HttpClientResponse result = await _httpClient.PostAsync(_serverUrl + (_serverUrl.EndsWith(\"/\") ? \"\" : \"{"/"}\") + \"{serviceName}/{methodInfo.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
                builder.AppendLine($"{prefix + prefix}{{");
                GenerateHttpMethodParameters(methodInfo, prefix, builder, doSemicolon);
                builder.AppendLine($"{prefix + prefix}}});");
                builder.AppendLine($"{prefix + prefix}ResponseHeaders = result.ResponseHeaders;");
                builder.AppendLine($"{prefix + prefix}Status = result.Status;");
                if (methodInfo.ReturnTypeName != "void")
                    builder.AppendLine($"{prefix + prefix}return SignalGo.Client.ClientSerializationHelper.DeserializeObject<{methodInfo.ReturnTypeName}>(result.Data);");
                builder.AppendLine($"{prefix}}}");
            }
        }

        private static void GenerateHttpMethodParameters(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool doSemicolon = true)
        {
            foreach (ParameterReferenceInfo parameter in methodInfo.Parameters)
            {
                builder.AppendLine($"{prefix + prefix + prefix} new  SignalGo.Shared.Models.ParameterInfo() {{ Name = nameof({parameter.Name}),Value = SignalGo.Client.ClientSerializationHelper.SerializeObject({parameter.Name}) }},");
            }
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

        private static string GenerateMethodParameters(MethodReferenceInfo methodInfo)
        {
            StringBuilder builder = new StringBuilder();
            int index = 0;
            foreach (ParameterReferenceInfo item in methodInfo.Parameters)
            {
                if (index > 0)
                    builder.Append(", ");
                builder.Append($"{item.TypeName} {item.Name}");
                index++;
            }
            return builder.ToString();
        }

        private static void GenerateHttpServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder)
        {
            builder.AppendLine(prefix + "public class " + classReferenceInfo.Name);
            builder.AppendLine(prefix + "{");
            builder.AppendLine("        public " + classReferenceInfo.Name + @"(string serverUrl, SignalGo.Client.HttpClient httpClient = null)
        {
            _serverUrl = serverUrl;
            _httpClient = httpClient;
            if (_httpClient == null)
                _httpClient = new SignalGo.Client.HttpClient();
        }

        private readonly string _serverUrl = null;
        private SignalGo.Client.HttpClient _httpClient;
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
        public System.Net.HttpStatusCode Status { get; set; }");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateHttpMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, false);
                GenerateHttpAsyncMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, false);
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

        public static void GenerateModelClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, MapDataClassInfo mapDataClassInfo)
        {
            string baseName = "";
            if (!string.IsNullOrEmpty(classReferenceInfo.BaseClassName))
                baseName = " : " + classReferenceInfo.BaseClassName;
            builder.AppendLine(prefix + "public class " + classReferenceInfo.Name + baseName);
            builder.AppendLine(prefix + "{");
            foreach (PropertyReferenceInfo propertyInfo in classReferenceInfo.Properties)
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

        public static string[] SplitWithIgnoreQuotes(string text, string splitText)
        {
            return System.Text.RegularExpressions.Regex.Split(text, splitText + "(?=(?:[^\"|']*[\"|'][^\"|']*[\"|'])*[^\"|']*$)");
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
