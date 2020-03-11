using Newtonsoft.Json;
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
    public class TypeScriptLanguageMap
    {
        public string CalculateMapData(string savePath, NamespaceReferenceInfo namespaceReferenceInfo, string serviceName)
        {
            namespaceReferenceInfo = JsonConvert.DeserializeObject<NamespaceReferenceInfo>(JsonConvert.SerializeObject(namespaceReferenceInfo));
            ProjectInfoBase project = LanguageMapBase.GetCurrent.GetActiveProject();
            List<MapDataClassInfo> MapDataClassInfoes = new List<MapDataClassInfo>();
            List<string> usingsOfClass = new List<string>();
            foreach (ProjectItemInfoBase projectItem in LanguageMapBase.GetCurrent.GetAllProjectItemsWithoutServices(project.ProjectItemsInfoBase))
            {
                if (projectItem.GetFileCount() == 0)
                    continue;
                string fileName = projectItem.GetFileName(0);
                if (Path.GetExtension(fileName).ToLower() == ".ts")
                {
                    string dir = Path.GetDirectoryName(fileName);
                    if (File.Exists(Path.Combine(dir, "setting.signalgo")) || !File.Exists(fileName))
                        continue;
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
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
                                //if (lineResult.Trim().StartsWith("using ") && lineResult.Trim().EndsWith(";") && !lineResult.Contains("("))
                                //{
                                //    var uses = GetListOfUsing(lineResult);
                                //    mapDataClassInfo.Usings.AddRange(uses);
                                //    usingsOfClass.AddRange(uses);
                                //}

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
            builderResult.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());


            //builderResult.AppendLine("import { List } from 'src/app/SharedComponents/linqts';");
            builderResult.AppendLine($"export module {serviceName} {{");
            //builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".ServerServices");
            //builderResult.AppendLine("{");
            //foreach (var classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ServiceLevel))
            //{
            //    GenerateServiceClass(classInfo, "    ", builderResult, true, "ServiceType.ServerService");
            //}
            //builderResult.AppendLine("}");
            //builderResult.AppendLine("");


            //builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".StreamServices");
            //builderResult.AppendLine("{");
            //foreach (var classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.StreamLevel))
            //{
            //    GenerateServiceClass(classInfo, "    ", builderResult, true, "ServiceType.StreamService");
            //}
            //builderResult.AppendLine("}");
            //builderResult.AppendLine("");

            //builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".OneWayServices");
            //builderResult.AppendLine("{");
            //foreach (var classInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.OneWayLevel))
            //{
            //    GenerateOneWayServiceClass(classInfo, "    ", builderResult, true, "ServiceType.OneWayService");
            //}
            //builderResult.AppendLine("}");
            //builderResult.AppendLine("");




            //Dictionary<string, string> AddedModels = new Dictionary<string, string>();
            //Dictionary<string, List<ClassReferenceInfo>> NeedToAddModels = new Dictionary<string, List<ClassReferenceInfo>>();
            List<string> namespaces = new List<string>();
            foreach (IGrouping<string, ClassReferenceInfo> groupInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ModelLevel).GroupBy(x => x.NameSpace))
            {
                namespaces.Add(groupInfo.Key);
                builderResult.AppendLine("export namespace " + groupInfo.Key + " {");
                foreach (ClassReferenceInfo modelInfo in groupInfo)
                {
                    GenerateModelClass(modelInfo, "    ", builderResult, MapDataClassInfoes.Where(x => x.Name == modelInfo.NormalizedName).FirstOrDefault(), serviceName);
                }
                builderResult.AppendLine("}");
                builderResult.AppendLine("");
            }

            foreach (ClassReferenceInfo httpClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.HttpServiceLevel))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());
                //builder.AppendLine("import { List } from 'src/app/SharedComponents/linqts';");
                GenerateHttpServiceClass(httpClassInfo, "    ", builder, serviceName);
                string result = builder.ToString();
                foreach (string space in namespaces)
                {
                    if (result.Contains(serviceName + "." + space))
                        continue;
                    result = result.Replace(space, serviceName + "." + space);
                }
                httpClassInfo.ServiceName = httpClassInfo.ServiceName.Replace("/", "").Replace("\\", "");
                File.WriteAllText(Path.Combine(savePath, httpClassInfo.ServiceName + "Service.ts"), result, Encoding.UTF8);
            }
            //builderResult.AppendLine("namespace " + namespaceReferenceInfo.Name + ".ClientServices");
            //builderResult.AppendLine("{");
            //foreach (var callbackInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.CallbackLevel))
            //{
            //    GenerateServiceClass(callbackInfo, "    ", builderResult, false, "ServiceType.ClientService");
            //}
            //builderResult.AppendLine("}");
            //builderResult.AppendLine("");


            foreach (IGrouping<string, EnumReferenceInfo> groupInfo in namespaceReferenceInfo.Enums.GroupBy(x => x.NameSpace))
            {
                builderResult.AppendLine("export namespace " + groupInfo.Key + " {");
                foreach (EnumReferenceInfo enumInfo in groupInfo)
                {
                    GenerateModelEnum(enumInfo, "    ", builderResult);
                }
                builderResult.AppendLine("}");
                builderResult.AppendLine("");
            }

            //create base namespace
            builderResult.AppendLine("export namespace SignalGoReference.Models {");
            builderResult.AppendLine("");
            builderResult.AppendLine(@"
    export class KeyValuePair<TKey,TValue>{
        Key: TKey;
        Value: TValue;
    }
    export class Dictionary3<TKey,TValue> {
        [Key: string]: TValue;
    }");

            builderResult.AppendLine("}");
            builderResult.AppendLine("}");


            return builderResult.ToString();
        }

        private void GenerateMethod(string serviceName, MethodReferenceInfo methodInfo, string prefix, StringBuilder resultBuilder, bool doSemicolon, string baseServiceName)
        {
            StringBuilder builder = new StringBuilder();
            string returnTypeName = GetReturnTypeName(methodInfo.ReturnTypeName, baseServiceName);
            builder.AppendLine($"{prefix}{methodInfo.DuplicateName}({GenerateMethodParameters(methodInfo, baseServiceName)}): Promise<{returnTypeName}> {{");
            builder.Append($@"return this.server.post<{returnTypeName}>('{serviceName}/{methodInfo.Name}',");
            int index = 0;
            if (methodInfo.Parameters.Count == 0)
                builder.AppendLine("null");
            else
            {
                builder.AppendLine(" {");
                foreach (ParameterReferenceInfo item in methodInfo.Parameters)
                {
                    if (index > 0)
                        builder.Append(", ");
                    builder.AppendLine(prefix + prefix + prefix + item.Name + ":" + item.Name);
                    index++;
                }
                builder.Append(prefix + prefix + "}");
            }
            builder.AppendLine(");");
            builder.AppendLine(prefix + "}");
            string result = builder.ToString();
            if (!result.Contains("SignalGo.Shared"))
                resultBuilder.AppendLine(result);
        }

        private void GenerateProperty(PropertyReferenceInfo propertyInfo, string prefix, bool generateOnPropertyChanged, StringBuilder builder, string baseServiceName)
        {
            bool isNullable = propertyInfo.ReturnTypeName.Contains("?");
            propertyInfo.ReturnTypeName = GetReturnTypeName(propertyInfo.ReturnTypeName, baseServiceName);
            //create field
            builder.AppendLine($"{prefix}{propertyInfo.Name}{(isNullable ? "?" : "")}: {propertyInfo.ReturnTypeName};");

            //builder.AppendLine($"{prefix}public get {propertyInfo.Name}(): {propertyInfo.ReturnTypeName} {{");
            //builder.AppendLine($"{prefix + prefix }return this._{propertyInfo.Name};");
            //builder.AppendLine($"{prefix}}}");

            //builder.AppendLine($"{prefix}public set {propertyInfo.Name}(v: {propertyInfo.ReturnTypeName}) {{");
            //builder.AppendLine($"{prefix + prefix}this._{propertyInfo.Name} = v;");

            //if (generateOnPropertyChanged)
            //    builder.AppendLine($"{prefix + prefix + prefix}OnPropertyChanged(nameof({propertyInfo.Name}));");

            //builder.AppendLine($"{prefix}}}");

            builder.AppendLine();
        }

        private static string GetReturnTypeName(string name, string serviceName)
        {
            string baseBaseName = name;
            Dictionary<string, string> returnTypes = new Dictionary<string, string>()
            {
                { "bool","boolean" },
                { "int","number" },
                { "System.Int16","number" },
                { "System.Int32","number" },
                { "System.Int64","number" },
                { "long","number" },
                { "double","number" },
                { "byte","number" },
                { "float","number" },
                { "short","number" },
                { "uint","number" },
                { "ushort","number" },
                { "sbyte","number" },
                { "ulong","number" },
                { "UInt16","number" },
                { "UInt32","number" },
                { "UInt64","number" },
                { "UIntPtr","number" },
                { "IntPtr","number" },

                { "System.Int16[]","number[]" },
                { "System.Int32[]","number[]" },
                { "System.Int64[]","number[]" },
                { "byte[]","number[]" },

                { "System.DateTime","Date" },
                { "System.Guid","string" },
                { "System.Uri","string" },
            };
            name = name.Replace("?", "");
            if (returnTypes.ContainsKey(name))
                name = name.Replace(name, returnTypes[name]);

            foreach (KeyValuePair<string, string> item in returnTypes)
            {
                if (name.Contains("<" + item.Key + ">"))
                    name = name.Replace("<" + item.Key + ">", "<" + item.Value + ">");
            }
            //if (name.StartsWith("System.Collections.Generic.ICollection<"))
            //{
            //    name = name.Replace("System.Collections.Generic.ICollection<", "").Replace(">", "[]");
            //}
            //else if (name.StartsWith("System.Collections.Generic.List<"))
            //{
            //    name = name.Replace("System.Collections.Generic.List<", "").Replace(">", "[]");
            //}
            if (name.Contains("System.Collections.Generic.ICollection<"))
            {
                //name = name.Replace("System.Collections.Generic.ICollection<", "List<");
                name = RemoveBlockToArray("System.Collections.Generic.ICollection<", name);
            }
            else if (name.Contains("System.Collections.Generic.List<"))
            {
                //name = name.Replace("System.Collections.Generic.List<", "List<");
                name = RemoveBlockToArray("System.Collections.Generic.List<", name);
            }
            else if (name.Contains("System.Collections.Generic.Dictionary<"))
            {
                name = name.Replace("System.Collections.Generic.Dictionary<", serviceName + ".SignalGoReference.Models.Dictionary<");
                //name = "{}";
            }
            foreach (KeyValuePair<string, string> item in returnTypes)
            {
                string text1 = $"<{item.Key}";
                string text2 = $"{item.Key},";
                string text3 = $"{item.Key}>";
                string text4 = $",{item.Key}";
                if (name.Contains(text1))
                    name = name.Replace(text1, $"<{item.Value}");
                else if (name.Contains(text2))
                    name = name.Replace(text2, $"{item.Value},");
                else if (name.Contains(text3))
                    name = name.Replace(text3, $"{item.Value}>");
                else if (name.Contains(text4))
                    name = name.Replace(text4, $",{item.Value}");
            }
            //string findName = name;
            bool hasSuffix = false;
            if (name.Contains("<"))
            {
                //findName = name.Substring(0, name.IndexOf('<'));
                hasSuffix = true;
            }

            //KeyValuePair<string, string>? find = RenamedModels.Where(x => (!hasSuffix && x.Value == findName) || (hasSuffix && x.Value.Contains("<") && x.Value.Substring(0, x.Value.IndexOf('<')) == findName)).Select(x => (KeyValuePair<string, string>?)x).FirstOrDefault();

            if (hasSuffix)
            {
                return ReplaceSuffix(name);
            }
            return name;
        }

        private static string TakeBlock(string text, char startBlock, char endBlock, char plusChar, char endPlusChar)
        {
            int indexToBreak = 0;
            bool canAppend = false;
            StringBuilder result = new StringBuilder();
            bool isStarted = false;
            foreach (char item in text)
            {
                if (isStarted)
                {
                    if (item == '<')
                        indexToBreak++;
                    else if (item == '>')
                        indexToBreak--;
                    if (indexToBreak < -1)
                        break;
                    canAppend = indexToBreak <= 0;
                    if (canAppend && item == '>' && indexToBreak == 0)
                        continue;
                }
                if (item == startBlock && !isStarted)
                {
                    canAppend = true;
                    isStarted = true;
                }
                if (canAppend)
                    result.Append(item);
            }
            return result.ToString();
        }

        private static string ReplaceSuffix(string newString)
        {
            if (newString.Contains("FoodCategoryInfo"))
            {
            }
            return GenericInfo.GenerateGeneric(newString).ToString();
        }

        private static string RemoveBlockToArray(string title, string name)
        {
            while (name.Contains(title))
            {
                string block = title;
                int start = name.IndexOf(block) + block.Length;
                int findLast = start;
                int canBreak = 0;
                foreach (char item in name.Substring(start))
                {
                    if (item == '<')
                        canBreak++;
                    else if (item == '>')
                    {
                        if (canBreak == 0)
                        {
                            break;
                        }
                        canBreak--;
                    }
                    start++;
                }
                StringBuilder aStringBuilder = new StringBuilder(name);
                aStringBuilder.Remove(start, 1);
                aStringBuilder.Insert(start, "[]");
                aStringBuilder.Remove(name.IndexOf(block), block.Length);
                name = aStringBuilder.ToString();
                //name = name;//.Replace(block, "");
            }
            return name;
        }

        private string GenerateMethodParametersWitoutTypes(MethodReferenceInfo methodInfo)
        {
            StringBuilder builder = new StringBuilder();
            foreach (ParameterReferenceInfo item in methodInfo.Parameters)
            {
                builder.Append(", ");
                builder.Append($"{item.Name}");
            }
            return builder.ToString();
        }

        private string GenerateMethodParameters(MethodReferenceInfo methodInfo, string baseServiceName)
        {
            StringBuilder builder = new StringBuilder();
            int index = 0;
            foreach (ParameterReferenceInfo item in methodInfo.Parameters)
            {
                if (index > 0)
                    builder.Append(", ");
                builder.Append($"{item.Name}: {GetReturnTypeName(item.TypeName, baseServiceName)}");
                index++;
            }
            return builder.ToString();
        }

        private void GenerateHttpServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, string baseServiceName)
        {
            builder.AppendLine($@"import {{ Injectable }} from '@angular/core';
import {{ ServerConnectionService }} from './server-connection.service';
import {{ {baseServiceName} }} from './Reference';
@Injectable({{
  providedIn: 'root'
}})");
            string serviceName = FirstCharToUpper(classReferenceInfo.ServiceName);
            builder.AppendLine(prefix + "export class " + serviceName + "Service {");
            builder.AppendLine(prefix + prefix + "constructor(private server: ServerConnectionService) { }");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateMethod(serviceName, methodInfo, prefix + prefix, builder, false, baseServiceName);
                //GenerateAsyncMethod(methodInfo, prefix + prefix, builder, false);
            }
            builder.AppendLine(prefix + "}");
        }

        private static string FirstCharToUpper(string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        private void GenerateModelEnum(EnumReferenceInfo enumReferenceInfo, string prefix, StringBuilder builder)
        {
            builder.AppendLine(prefix + "export enum " + enumReferenceInfo.Name + " {");//+ " : " + enumReferenceInfo.TypeName
            foreach (SignalGo.Shared.Models.KeyValue<string, string> name in enumReferenceInfo.KeyValues)
            {
                builder.AppendLine($"{prefix + prefix}{name.Key} = {name.Value},");
            }
            builder.AppendLine(prefix + "}");
            builder.AppendLine();
        }

        private List<string> GeneratedModels { get; set; } = new List<string>();

        private void GenerateModelClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, MapDataClassInfo mapDataClassInfo, string baseServiceName)
        {
            string mainName = classReferenceInfo.NormalizedName;
            if (mainName.Contains("<"))
                mainName = mainName.Substring(0, mainName.IndexOf('<'));

            string name = mainName;
            //while (RenamedModels.ContainsKey(classReferenceInfo.NameSpace + "." + name))
            //{
            //    index++;
            //    name = mainName + index;
            //}
            if (classReferenceInfo.NormalizedName.Contains("<"))
            {
                int indexName = classReferenceInfo.NormalizedName.Count(x => x == '>' || x == '<' || x == ',');
                mainName = name + indexName + classReferenceInfo.NormalizedName.Substring(classReferenceInfo.NormalizedName.IndexOf('<'));
                if (GeneratedModels.Contains(classReferenceInfo.NameSpace + "." + mainName))
                    return;
                GeneratedModels.Add(classReferenceInfo.NameSpace + "." + mainName);
                classReferenceInfo.Name = mainName;
                //RenamedModels.Add(classReferenceInfo.NameSpace + "." + name, classReferenceInfo.NameSpace + "." + oldName);
            }
            else
            {
                if (GeneratedModels.Contains(classReferenceInfo.NameSpace + "." + name))
                    return;
                GeneratedModels.Add(classReferenceInfo.NameSpace + "." + name);
                classReferenceInfo.Name = name;
                //RenamedModels.Add(classReferenceInfo.NameSpace + "." + name, classReferenceInfo.NameSpace + "." + oldName);
            }
            string baseName = "";
            if (!string.IsNullOrEmpty(classReferenceInfo.BaseClassName) && !classReferenceInfo.BaseClassName.StartsWith("SignalGo."))
                baseName = " extends " + classReferenceInfo.BaseClassName;
            builder.AppendLine(prefix + "export class " + classReferenceInfo.NormalizedName + baseName + "{");
            foreach (PropertyReferenceInfo propertyInfo in classReferenceInfo.Properties)
            {
                if (mapDataClassInfo != null && mapDataClassInfo.IgnoreProperties.Contains(propertyInfo.Name))
                    continue;
                GenerateProperty(propertyInfo, prefix + prefix, false, builder, baseServiceName);
            }
            if (mapDataClassInfo != null && !string.IsNullOrEmpty(mapDataClassInfo.Body))
                builder.AppendLine(mapDataClassInfo.Body);
            builder.AppendLine();

            builder.AppendLine(prefix + "}");

            builder.AppendLine();
        }

        private static string[] SplitWithIgnoreQuotes(string text, string splitText)
        {
            return System.Text.RegularExpressions.Regex.Split(text, splitText + "(?=(?:[^\"|']*[\"|'][^\"|']*[\"|'])*[^\"|']*$)");
        }

        private static string[] GetListOfUsing(string text)
        {
            return text.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Replace("using ", "").Trim()).ToArray();
        }

        private static Tuple<string, string> GetNameSpaceAndName(string text)
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
