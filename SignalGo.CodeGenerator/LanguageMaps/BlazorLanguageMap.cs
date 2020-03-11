using SignalGo.CodeGenerator.Helpers;
using SignalGo.CodeGenerator.Models;
using SignalGo.Shared.Models.ServiceReference;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SignalGo.CodeGenerator.LanguageMaps
{
    public static class BlazorLanguageMap
    {
        public static string CalculateMapData(NamespaceReferenceInfo namespaceReferenceInfo, string serviceName, AddReferenceConfigInfo config)
        {
            var project = LanguageMapBase.GetCurrent.GetActiveProject();
            List<MapDataClassInfo> MapDataClassInfoes = new List<MapDataClassInfo>();
            List<string> usingsOfClass = new List<string>();
            foreach (var projectItem in LanguageMapBase.GetCurrent.GetAllProjectItemsWithoutServices(project.ProjectItemsInfoBase))
            {
                if (projectItem.GetFileCount() == 0)
                    continue;
                string fileName = projectItem.GetFileName(0);
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
                                    string[] uses = CsharpLanguageMap.GetListOfUsing(lineResult);
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
                                            Tuple<string, string> nameSpaceAndName = CsharpLanguageMap.GetNameSpaceAndName(item);
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


                                    string[] split = CsharpLanguageMap.SplitWithIgnoreQuotes(lineResult.Substring(index + length), ",");
                                    foreach (string item in split)
                                    {
                                        if (item.ToLower().Contains("maptotype") || item.Contains("typeof"))
                                        {
                                            Tuple<string, string> nameSpaceAndName = CsharpLanguageMap.GetNameSpaceAndName(item.Split('=').LastOrDefault());
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
                                            Tuple<string, string> nameSpaceAndName = CsharpLanguageMap.GetNameSpaceAndName(item.Split('=').LastOrDefault());
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
            builderResult.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());

            string[] usings = new string[] {
                "System",
                "System.Collections.Generic",
                "System.Net",
                "System.Net.Http.Headers",
                "System.Net.Http",
                "System.Text",
                "System.ComponentModel",
            };
            foreach (var item in usings)
            {
                if (!namespaceReferenceInfo.Usings.Any(x => x.Equals(item, StringComparison.OrdinalIgnoreCase)))
                    namespaceReferenceInfo.Usings.Add(item);
            }
            foreach (string item in namespaceReferenceInfo.Usings)
            {
                if (item.StartsWith("SignalGo.".Trim()))
                    continue;
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
            //foreach (ClassReferenceInfo classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ServiceLevel))
            //{
            //    GenerateServiceClass(classInfo, "    ", builderResult, true, "ServiceType.ServerService");
            //}
            builderResult.AppendLine("}");
            builderResult.AppendLine("");


            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".StreamServices");
            builderResult.AppendLine("{");
            //foreach (ClassReferenceInfo classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.StreamLevel))
            //{
            //    GenerateServiceClass(classInfo, "    ", builderResult, true, "ServiceType.StreamService");
            //}
            builderResult.AppendLine("}");
            builderResult.AppendLine("");

            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".OneWayServices");
            builderResult.AppendLine("{");
            //foreach (ClassReferenceInfo classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.OneWayLevel))
            //{
            //    GenerateOneWayServiceClass(classInfo, "    ", builderResult, true, "ServiceType.OneWayService");
            //}
            builderResult.AppendLine("}");
            builderResult.AppendLine("");



            builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".HttpServices");
            builderResult.AppendLine("{");

            builderResult.AppendLine(@"    /// <summary>
    /// reponse of http request
    /// </summary>
    public class HttpClientResponse
    {
        /// <summary>
        /// status
        /// </summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        /// <summary>
        /// data of response
        /// </summary>
        public string Data { get; set; }
        /// <summary>
        /// response headers
        /// </summary>
        public HttpResponseHeaders ResponseHeaders { get; set; }
    }

    /// <summary>
    /// a parameter data for method call
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// type of parameter
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// value of parameter
        /// </summary>
        public string Value { get; set; }
    }

    public class SignalGoBlazorHttpClient
    {
        public HttpRequestHeaders RequestHeaders { get; set; } = new HttpRequestMessage().Headers;

        public async Task<HttpClientResponse> PostAsync(string url, ParameterInfo[] parameterInfoes)
        {
            Microsoft.AspNetCore.Blazor.Browser.Http.BrowserHttpMessageHandler browserHttpMessageHandler = new Microsoft.AspNetCore.Blazor.Browser.Http.BrowserHttpMessageHandler();
            using (HttpClient httpClient = new System.Net.Http.HttpClient(browserHttpMessageHandler))
            {
                foreach (KeyValuePair<string, IEnumerable<string>> item in RequestHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                }

                MultipartFormDataContent form = new MultipartFormDataContent();
                foreach (ParameterInfo item in parameterInfoes)
                {
                    StringContent jsonPart = new StringContent(item.Value.ToString(), Encoding.UTF8, ""application/json"");
                    jsonPart.Headers.ContentDisposition = new ContentDispositionHeaderValue(""form-data"");
                    jsonPart.Headers.ContentDisposition.Name = item.Name;
                    jsonPart.Headers.ContentType = new MediaTypeHeaderValue(""application/json"");
                    form.Add(jsonPart);
                }

                HttpResponseMessage httpresponse = await httpClient.PostAsync(url, form);
                if (!httpresponse.IsSuccessStatusCode)
                {
                    // Unwrap the response and throw as an Api Exception:
                    throw new Exception(await httpresponse.Content.ReadAsStringAsync());
                }
                else
                {
                    httpresponse.EnsureSuccessStatusCode();
                    return new HttpClientResponse() { Data = await httpresponse.Content.ReadAsStringAsync(), ResponseHeaders = httpresponse.Headers, Status = httpresponse.StatusCode };
                }
            }
        }
    }");
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
                    CsharpLanguageMap.GenerateModelClass(modelInfo, "    ", builderResult, MapDataClassInfoes.Where(x => x.Name == modelInfo.NormalizedName).FirstOrDefault(), config, new List<string>());
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
            //foreach (ClassReferenceInfo callbackInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.CallbackLevel))
            //{
            //    GenerateServiceClass(callbackInfo, "    ", builderResult, false, "ServiceType.ClientService");
            //}
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
                    CsharpLanguageMap.GenerateModelEnum(enumInfo, "    ", builderResult);
                }
                builderResult.AppendLine("}");
                builderResult.AppendLine("");
            }
            //builderResult.AppendLine("}");
            //builderResult.AppendLine("");


            return builderResult.ToString();
        }


        private static void GenerateHttpServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder)
        {
            builder.AppendLine(prefix + "public class " + classReferenceInfo.NormalizedName);
            builder.AppendLine(prefix + "{");
            builder.AppendLine("        public " + classReferenceInfo.NormalizedName + $@"(string serverUrl, SignalGoBlazorHttpClient httpClient = null)
        {{
            _serverUrl = serverUrl;
            _httpClient = httpClient;
            if (_httpClient == null)
                _httpClient = new SignalGoBlazorHttpClient();
        }}

        private readonly string _serverUrl = null;
        private SignalGoBlazorHttpClient _httpClient;
        public static {classReferenceInfo.NormalizedName} Current {{ get; set; }}
        public HttpResponseHeaders ResponseHeaders {{ get; set; }}
        public System.Net.HttpStatusCode Status {{ get; set; }}");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateHttpAsyncMethod(methodInfo, classReferenceInfo.ServiceName, prefix + prefix, builder, false);
            }
            builder.AppendLine(prefix + "}");
        }

        private static void GenerateHttpAsyncMethod(MethodReferenceInfo methodInfo, string serviceName, string prefix, StringBuilder builder, bool doSemicolon = true)
        {
            if (methodInfo.ReturnTypeName.Contains("SignalGo.Shared.Http.ActionResult"))
                return;
            string returnType = "public async Task";
            if (methodInfo.ReturnTypeName != "void")
                returnType = "public async Task<" + methodInfo.ReturnTypeName + ">";
            builder.AppendLine($"{prefix}{returnType} {methodInfo.Name}Async({GenerateMethodParameters(methodInfo)}){(doSemicolon ? ";" : "")}");
            //generate empty data
            if (!doSemicolon)
            {
                builder.AppendLine($"{prefix}{{");
                builder.AppendLine($"{prefix + prefix}HttpClientResponse result = await _httpClient.PostAsync(_serverUrl + (_serverUrl.EndsWith(\"/\") ? \"\" : \"{"/"}\") + \"{serviceName}/{methodInfo.Name}\", new ParameterInfo[]");
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

        private static void GenerateHttpMethodParameters(MethodReferenceInfo methodInfo, string prefix, StringBuilder builder, bool doSemicolon = true)
        {
            foreach (ParameterReferenceInfo parameter in methodInfo.Parameters)
            {
                builder.AppendLine($"{prefix + prefix + prefix} new ParameterInfo() {{ Name = nameof({parameter.Name}),Value = Newtonsoft.Json.JsonConvert.SerializeObject({parameter.Name}) }},");
            }
        }
    }
}
