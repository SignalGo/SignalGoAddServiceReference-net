﻿using EnvDTE;
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
    /// <summary>
    /// angular have a bug in build version to export enums
    /// this version of compile will fix that bugs with generate all you need in new files
    /// </summary>
    public class AngularTypeScriptLanguageMap
    {
        public void CalculateMapData(string savePath, NamespaceReferenceInfo namespaceReferenceInfo, string serviceName)
        {
            Project project = BaseLanguageMap.GetActiveProject();
            List<MapDataClassInfo> MapDataClassInfoes = new List<MapDataClassInfo>();
            List<string> usingsOfClass = new List<string>();
            string fileName = "";
            foreach (ProjectItem projectItem in BaseLanguageMap.GetAllProjectItemsWithoutServices(project.ProjectItems))
            {
                if (projectItem.FileCount == 0)
                    continue;
                fileName = projectItem.FileNames[0];
                if (Path.GetExtension(fileName).ToLower() == ".ts")
                {
                    string dir = Path.GetDirectoryName(fileName);
                    if (File.Exists(Path.Combine(dir, "setting.signalgo")) || !File.Exists(fileName))
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

            string folder = "";


            foreach (IGrouping<string, ClassReferenceInfo> groupInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ModelLevel).GroupBy(x => x.NameSpace))
            {
                //namespaces.Add(groupInfo.Key);
                folder = Path.Combine(savePath, groupInfo.Key);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                foreach (ClassReferenceInfo modelInfo in groupInfo)
                {
                    //key is full name and value 1 is name space and value 2 is name
                    Dictionary<string, Dictionary<string, string>> namespaces = new Dictionary<string, Dictionary<string, string>>();
                    StringBuilder builderResult = new StringBuilder();
                    builderResult.AppendLine(BaseLanguageMap.GetAutoGeneratedText());
                    builderResult.AppendLine("*$-SignalGoNameSpaces-!*");
                    fileName = Path.Combine(folder, GetFileNameFromClassName(modelInfo.Name));
                    GenerateModelClass(modelInfo, "    ", builderResult, MapDataClassInfoes.Where(x => x.Name == modelInfo.Name).FirstOrDefault(), serviceName, namespaces);
                    StringBuilder nameSpacesResult = new StringBuilder();

                    foreach (KeyValuePair<string, Dictionary<string, string>> item in namespaces)
                    {
                        foreach (KeyValuePair<string, string> keyValue in item.Value)
                        {
                            nameSpacesResult.AppendLine($"import {{{ keyValue.Value }}} from \"../{keyValue.Key}/{keyValue.Value}\"");
                        }
                    }
                    builderResult.Replace("*$-SignalGoNameSpaces-!*", nameSpacesResult.ToString());
                    File.WriteAllText(fileName, builderResult.ToString(), Encoding.UTF8);
                }
            }

            foreach (ClassReferenceInfo httpClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.HttpServiceLevel))
            {
                //key is full name and value 1 is name space and value 2 is name
                Dictionary<string, Dictionary<string, string>> namespaces = new Dictionary<string, Dictionary<string, string>>();
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(BaseLanguageMap.GetAutoGeneratedText());
                builder.AppendLine("*$-SignalGoNameSpaces-!*");
                //builder.AppendLine("import { List } from 'src/app/SharedComponents/linqts';");
                GenerateHttpServiceClass(httpClassInfo, "    ", builder, serviceName, namespaces);
                StringBuilder nameSpacesResult = new StringBuilder();

                foreach (KeyValuePair<string, Dictionary<string, string>> item in namespaces)
                {
                    foreach (KeyValuePair<string, string> keyValue in item.Value)
                    {
                        nameSpacesResult.AppendLine($"import {{{ keyValue.Value }}} from \"./{keyValue.Key}/{keyValue.Value}\"");
                    }
                }

                //string result = builder.ToString();
                //foreach (string space in namespaces)
                //{
                //    if (result.Contains(serviceName + "." + space))
                //        continue;
                //    result = result.Replace(space, serviceName + "." + space);
                //}
                builder.Replace("*$-SignalGoNameSpaces-!*", nameSpacesResult.ToString());
                File.WriteAllText(Path.Combine(savePath, httpClassInfo.ServiceName + "Service.ts"), builder.ToString(), Encoding.UTF8);
            }


            foreach (IGrouping<string, EnumReferenceInfo> groupInfo in namespaceReferenceInfo.Enums.GroupBy(x => x.NameSpace))
            {
                folder = Path.Combine(savePath, groupInfo.Key);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                foreach (EnumReferenceInfo enumInfo in groupInfo)
                {
                    StringBuilder builderResult = new StringBuilder();
                    builderResult.AppendLine(BaseLanguageMap.GetAutoGeneratedText());

                    fileName = Path.Combine(folder, GetFileNameFromClassName(enumInfo.Name));
                    GenerateModelEnum(enumInfo, "    ", builderResult);
                    File.WriteAllText(fileName, builderResult.ToString(), Encoding.UTF8);
                }

            }

            folder = Path.Combine(savePath, "SignalGoReference.Models");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            //create base namespace
            StringBuilder defaultSignalgoClasses = new StringBuilder();
            defaultSignalgoClasses.AppendLine(BaseLanguageMap.GetAutoGeneratedText());
            defaultSignalgoClasses.AppendLine(@"
            export class Dictionary3<TKey,TValue> {
                [Key: string]: TValue;
            }");
            fileName = Path.Combine(folder, "Dictionary3.ts");
            File.WriteAllText(fileName, defaultSignalgoClasses.ToString(), Encoding.UTF8);


            //return builderResult.ToString();
        }

        public string GetFileNameFromClassName(string name)
        {
            GenericInfo generic = GenericInfo.GenerateGeneric(name);
            generic.ClearNameSpaces(ClearString);
            return generic.Name.ToString() + ".ts";
        }

        private void GenerateMethod(string serviceName, MethodReferenceInfo methodInfo, string prefix, StringBuilder resultBuilder, bool doSemicolon, string baseServiceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
        {
            StringBuilder builder = new StringBuilder();
            string returnTypeName = GetReturnTypeName(methodInfo.ReturnTypeName, baseServiceName, nameSpaces);
            //AddToDictionary(nameSpaces, returnTypeName);
            if (returnTypeName == "SignalGo.Shared.Http.ActionResult")
                return;
            builder.AppendLine($"{prefix}{methodInfo.DuplicateName}({GenerateMethodParameters(methodInfo, baseServiceName, nameSpaces)}): Promise<{returnTypeName}> {{");
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

        private void GenerateProperty(PropertyReferenceInfo propertyInfo, string prefix, bool generateOnPropertyChanged, StringBuilder builder, string baseServiceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
        {
            bool isNullable = propertyInfo.ReturnTypeName.Contains("?");
            propertyInfo.ReturnTypeName = GetReturnTypeName(propertyInfo.ReturnTypeName, baseServiceName, nameSpaces);
            //AddToDictionary(nameSpaces, propertyInfo.ReturnTypeName);

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

        private static readonly Dictionary<string, string> fixedReturnTypes = new Dictionary<string, string>()
        {
                { "bool","boolean" },
                { "int","number" },
                { "system.int16","number" },
                { "system.int32","number" },
                { "system.int64","number" },
                { "system.string","string" },
                { "long","number" },
                { "double","number" },
                { "byte","number" },
                { "short","number" },
                { "uint","number" },
                { "ushort","number" },
                { "sbyte","number" },
                { "ulong","number" },
                { "uint16","number" },
                { "uint32","number" },
                { "uint64","number" },
                { "uintptr","number" },
                { "intptr","number" },

                { "system.int16[]","number[]" },
                { "system.int32[]","number[]" },
                { "system.int64[]","number[]" },

                { "system.datetime","Date" },
                { "system.date","Date" },
                { "system.guid","string" },
                { "system.uri","string" },
            };
        private string GetReturnTypeName(string name, string serviceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
        {
            string baseBaseName = name;

            name = name.Replace("?", "");
            if (fixedReturnTypes.ContainsKey(name.ToLower()))
                name = ReplaceIgnoreCase(name, name, fixedReturnTypes[name.ToLower()]);

            foreach (KeyValuePair<string, string> item in fixedReturnTypes)
            {
                if (name.ToLower().Contains("<" + item.Key + ">"))
                    name = ReplaceIgnoreCase(name, "<" + item.Key + ">", "<" + item.Value + ">");
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
            if (name.Contains("System.Collections.Generic.List<"))
            {
                //name = name.Replace("System.Collections.Generic.List<", "List<");
                name = RemoveBlockToArray("System.Collections.Generic.List<", name);
            }
            if (name.Contains("System.Collections.Generic.Dictionary<"))
            {
                name = name.Replace("System.Collections.Generic.Dictionary<", "SignalGoReference.Models.Dictionary<");
                //name = "{}";
            }
            foreach (KeyValuePair<string, string> item in fixedReturnTypes)
            {
                string text1 = $"<{item.Key}";
                string text2 = $"{item.Key},";
                string text3 = $"{item.Key}>";
                string text4 = $",{item.Key}";
                if (name.ToLower().Contains(text1))
                    name = ReplaceIgnoreCase(name, text1, $"<{item.Value}");
                else if (name.ToLower().Contains(text2))
                    name = ReplaceIgnoreCase(name, text2, $"{item.Value},");
                else if (name.ToLower().Contains(text3))
                    name = ReplaceIgnoreCase(name, text3, $"{item.Value}>");
                else if (name.ToLower().Contains(text4))
                    name = ReplaceIgnoreCase(name, text4, $",{item.Value}");
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
                //string result = ReplaceSuffix(name);
                return AddToDictionary(nameSpaces, name);
            }
            return AddToDictionary(nameSpaces, name);
        }

        private static string ReplaceIgnoreCase(string input, string str1, string str2)
        {
            return Regex.Replace(input, str1, str2, RegexOptions.IgnoreCase);
        }

        private static string FixIllegalChars(string name)
        {
            if (name == null)
                return name;
            name = name.Replace("[", "").Replace("]", "");
            if (name.Contains("<"))
                return name.Substring(0, name.IndexOf('<')).Trim();
            return name.Trim();
        }
        private static string ClearString(string name)
        {
            name = name
                .Replace("System.Collections.Generic.List<", "")
                .Replace("System.Collections.Generic.ICollection<", "")
                .Replace("System.Collections.Generic", "SignalGoReference.Models");

            return name.Trim();
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

        private string GenerateMethodParameters(MethodReferenceInfo methodInfo, string baseServiceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
        {
            StringBuilder builder = new StringBuilder();
            int index = 0;
            foreach (ParameterReferenceInfo item in methodInfo.Parameters)
            {
                if (index > 0)
                    builder.Append(", ");
                string returnType = GetReturnTypeName(item.TypeName, baseServiceName, nameSpaces);
                //AddToDictionary(nameSpaces, returnType);
                builder.Append($"{item.Name}: {returnType}");
                index++;
            }
            return builder.ToString();
        }

        private void GenerateHttpServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, string baseServiceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
        {
            builder.AppendLine($@"import {{ Injectable }} from '@angular/core';
import {{ ServerConnectionService }} from './server-connection.service';
@Injectable({{
  providedIn: 'root'
}})");
            string serviceName = FirstCharToUpper(classReferenceInfo.ServiceName);
            builder.AppendLine(prefix + "export class " + serviceName + "Service {");
            builder.AppendLine(prefix + prefix + "constructor(private server: ServerConnectionService) { }");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {

                GenerateMethod(serviceName, methodInfo, prefix + prefix, builder, false, baseServiceName, nameSpaces);
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

        private string AddToDictionary(Dictionary<string, Dictionary<string, string>> keyValuePairs, string fullName)
        {
            if (string.IsNullOrEmpty(fullName) || fullName == "SignalGo.Shared.Http.ActionResult")
                return fullName;
            GenericInfo generic = GenericInfo.GenerateGeneric(fullName);

            Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();
            generic.GetNameSpaces(data, ClearString);
            foreach (KeyValuePair<string, List<string>> item in data)
            {
                foreach (var value in item.Value)
                {
                    string full = FixIllegalChars(item.Key + "." + value);
                    if (fixedReturnTypes.ContainsKey(full.ToLower()))
                        continue;
                    if (keyValuePairs.TryGetValue(full, out Dictionary<string, string> dic))
                    {
                        if (!dic.ContainsKey(FixIllegalChars(item.Key)))
                            dic[FixIllegalChars(item.Key)] = FixIllegalChars(value);
                    }
                    else
                    {
                        keyValuePairs[full] = new Dictionary<string, string>() { { FixIllegalChars(item.Key), FixIllegalChars(value) } };
                    }
                }
            }
            generic.ClearNameSpaces(ClearString);
            return generic.ToString();
        }

        private List<string> GeneratedModels { get; set; } = new List<string>();

        private void GenerateModelClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, MapDataClassInfo mapDataClassInfo, string baseServiceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
        {
            string mainName = classReferenceInfo.Name;
            if (mainName.Contains("<"))
                mainName = mainName.Substring(0, mainName.IndexOf('<'));

            string name = mainName;
            //while (RenamedModels.ContainsKey(classReferenceInfo.NameSpace + "." + name))
            //{
            //    index++;
            //    name = mainName + index;
            //}
            if (classReferenceInfo.Name.Contains("<"))
            {
                int indexName = classReferenceInfo.Name.Count(x => x == '>' || x == '<' || x == ',');
                mainName = name + indexName + classReferenceInfo.Name.Substring(classReferenceInfo.Name.IndexOf('<'));
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
            {
                GenericInfo generic = GenericInfo.GenerateGeneric(classReferenceInfo.BaseClassName);
                generic.ClearNameSpaces(ClearString);
                string typeName = generic.ToString();
                AddToDictionary(nameSpaces, classReferenceInfo.BaseClassName);

                baseName = " extends " + typeName;
            }
            builder.AppendLine(prefix + "export class " + classReferenceInfo.Name + baseName + "{");
            foreach (PropertyReferenceInfo propertyInfo in classReferenceInfo.Properties)
            {
                if (mapDataClassInfo != null && mapDataClassInfo.IgnoreProperties.Contains(propertyInfo.Name))
                    continue;
                GenerateProperty(propertyInfo, prefix + prefix, false, builder, baseServiceName, nameSpaces);
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
