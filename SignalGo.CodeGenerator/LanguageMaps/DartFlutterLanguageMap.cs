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
    public class DartFlutterLanguageMap
    {
        public void CalculateMapData(string savePath, NamespaceReferenceInfo namespaceReferenceInfo, string serviceName)
        {
            ProjectInfoBase project = LanguageMapBase.GetCurrent.GetActiveProject();
            List<MapDataClassInfo> MapDataClassInfoes = new List<MapDataClassInfo>();
            List<string> usingsOfClass = new List<string>();
            string fileName = "";
            foreach (ProjectItemInfoBase projectItem in LanguageMapBase.GetCurrent.GetAllProjectItemsWithoutServices(project.ProjectItemsInfoBase))
            {
                if (projectItem.GetFileCount() == 0)
                    continue;
                fileName = projectItem.GetFileName(0);
                if (Path.GetExtension(fileName).ToLower() == ".dart")
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


            foreach (IGrouping<string, ClassReferenceInfo> groupInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.ModelLevel || x.Type == ClassReferenceType.InterfaceLevel).GroupBy(x => x.NameSpace))
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
                    builderResult.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());
                    builderResult.AppendLine("*$-SignalGoNameSpaces-!*");
                    fileName = Path.Combine(folder, GetFileNameFromClassName(modelInfo.NormalizedName));
                    GenerateModelClass(modelInfo, "    ", builderResult, MapDataClassInfoes.Where(x => x.Name == modelInfo.NormalizedName).FirstOrDefault(), serviceName, namespaces);
                    StringBuilder nameSpacesResult = new StringBuilder();

                    foreach (KeyValuePair<string, Dictionary<string, string>> item in namespaces)
                    {
                        if (item.Key == modelInfo.NameSpace + "." + modelInfo.NormalizedName)
                            continue;
                        foreach (KeyValuePair<string, string> keyValue in item.Value)
                        {
                            string import = $"import 'package:{serviceName}/{keyValue.Key}/{keyValue.Value}.dart';";
                            nameSpacesResult.AppendLine(import);
                            AddToImport(import);
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
                builder.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());
                builder.AppendLine("*$-SignalGoNameSpaces-!*");
                GenerateHttpServiceClass(httpClassInfo, "    ", builder, serviceName, namespaces);
                StringBuilder nameSpacesResult = new StringBuilder();

                foreach (KeyValuePair<string, Dictionary<string, string>> item in namespaces)
                {
                    foreach (KeyValuePair<string, string> keyValue in item.Value)
                    {
                        string import = $"import 'package:{serviceName}/{keyValue.Key}/{keyValue.Value}.dart';";
                        nameSpacesResult.AppendLine(import);
                        AddToImport(import);
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
                File.WriteAllText(Path.Combine(savePath, httpClassInfo.ServiceName.Replace("/", "").Replace("\\", "") + "Service.dart"), builder.ToString(), Encoding.UTF8);
            }

            foreach (ClassReferenceInfo callbackClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.CallbackLevel))
            {
                string name = callbackClassInfo.NormalizedName;
                if (name.StartsWith("I"))
                    name = name.Remove(0, 1);
                if (!name.EndsWith("CallbackService"))
                    name += "CallbackService";
                //key is full name and value 1 is name space and value 2 is name
                Dictionary<string, Dictionary<string, string>> namespaces = new Dictionary<string, Dictionary<string, string>>();
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());
                builder.AppendLine("*$-SignalGoNameSpaces-!*");
                GenerateCallbackServiceClass(callbackClassInfo, "    ", builder, serviceName, namespaces);
                StringBuilder nameSpacesResult = new StringBuilder();

                foreach (KeyValuePair<string, Dictionary<string, string>> item in namespaces)
                {
                    foreach (KeyValuePair<string, string> keyValue in item.Value)
                    {
                        string import = $"import 'package:{serviceName}/{keyValue.Key}/{keyValue.Value}.dart';";
                        nameSpacesResult.AppendLine(import);
                        AddToImport(import);
                    }
                }

                builder.Replace("*$-SignalGoNameSpaces-!*", nameSpacesResult.ToString());
                File.WriteAllText(Path.Combine(savePath, name.Replace("/", "").Replace("\\", "") + ".dart"), builder.ToString(), Encoding.UTF8);
            }

            foreach (IGrouping<string, EnumReferenceInfo> groupInfo in namespaceReferenceInfo.Enums.GroupBy(x => x.NameSpace))
            {
                folder = Path.Combine(savePath, groupInfo.Key);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                foreach (EnumReferenceInfo enumInfo in groupInfo)
                {
                    StringBuilder builderResult = new StringBuilder();
                    builderResult.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());

                    fileName = Path.Combine(folder, GetFileNameFromClassName(enumInfo.Name));
                    GenerateModelEnum(enumInfo, "    ", builderResult);
                    File.WriteAllText(fileName, builderResult.ToString(), Encoding.UTF8);
                }

            }

            StringBuilder jsonInitializer = new StringBuilder();

            fileName = Path.Combine(savePath, "JsonInitializer.dart");

            //create base namespace
            jsonInitializer.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());
            foreach (ClassReferenceInfo callbackClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.CallbackLevel))
            {
                string name = callbackClassInfo.NormalizedName;
                if (name.StartsWith("I"))
                    name = name.Remove(0, 1);
                if (!name.EndsWith("CallbackService"))
                    name += "CallbackService";
                jsonInitializer.AppendLine($"import 'package:{serviceName}/CallbackServices/{name}.dart';");
            }
            jsonInitializer.AppendLine($"import 'package:{serviceName}/Runtime/ParameterInfo.dart';");
            jsonInitializer.AppendLine($"import 'package:{serviceName}/Runtime/TypeMode.dart';");
            jsonInitializer.AppendLine($"import 'package:{serviceName}/Runtime/TypeBuilder.dart';");
            jsonInitializer.AppendLine("##$IMPORTS");
            jsonInitializer.AppendLine(@"class JsonInitializer {");
            jsonInitializer.AppendLine('\t' + @"static void initialize() {");
            GetnerateJsonTypeInitializer(serviceName, namespaceReferenceInfo, jsonInitializer);
            GetnerateJsonTypeInitializerForCallbacks(namespaceReferenceInfo, jsonInitializer);
            jsonInitializer.AppendLine('\t' + "}");
            jsonInitializer.AppendLine("}");
            jsonInitializer.Replace("##$IMPORTS", string.Join("\r\n", JsonAllTypesImports));
            File.WriteAllText(fileName, jsonInitializer.ToString(), Encoding.UTF8);

            jsonInitializer = new StringBuilder();
            jsonInitializer.AppendLine(LanguageMapBase.GetCurrent.GetAutoGeneratedText());

            fileName = Path.Combine(savePath, "ServerProviderImports.dart");

            foreach (string item in FlutterAllImports)
            {
                string export = "export " + item.Substring(7);
                jsonInitializer.AppendLine(export);
            }

            File.WriteAllText(fileName, jsonInitializer.ToString(), Encoding.UTF8);

        }

        private void AddToImport(string import)
        {
            if (!FlutterAllImports.Contains(import))
                FlutterAllImports.Add(import);
        }

        private readonly List<string> FlutterAllImports = new List<string>();

        private readonly List<string> JsonAllTypesImports = new List<string>();
        private readonly List<string> JsonAllTypes = new List<string>();
        private readonly List<string> JsonAllGeneratedTypes = new List<string>();
        private readonly Dictionary<string, string> JsonVariablesTypes = new Dictionary<string, string>();

        private List<Func<bool, bool>> AddTypesLater { get; set; } = new List<Func<bool, bool>>();

        public void GetnerateJsonTypeInitializerForCallbacks(NamespaceReferenceInfo namespaceReferenceInfo, StringBuilder builder)
        {
            foreach (ClassReferenceInfo callbackClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.CallbackLevel))
            {
                string name = callbackClassInfo.NormalizedName;
                if (name.StartsWith("I"))
                    name = name.Remove(0, 1);
                if (!name.EndsWith("CallbackService"))
                    name += "CallbackService";
                //key is full name and value 1 is name space and value 2 is name
                builder.AppendLine($"TypeBuilder.make<{name}>(TypeMode.Object)");
                foreach (MethodReferenceInfo methodInfo in callbackClassInfo.Methods)
                {
                    string methodName = methodInfo.Name;
                    if (methodName.EndsWith("Async"))
                        methodName = methodName.Substring(0, methodName.Length - 5);
                    builder.AppendLine($@".addMethod(
          ""{methodName}"",
          {GenerateJsonInitializerParametersArray(methodInfo)},
          ({name} x, {GenerateJsonInitializerParametersKeyValue(methodInfo, true)}) =>
              x.{methodInfo.Name.ToCamelCase()}({GenerateJsonInitializerParametersKeyValue(methodInfo, false)}))");
                }
                builder.AppendLine($@".createInstance(() => new {name}())
      .build();");
            }
        }

        private string GenerateJsonInitializerParametersArray(MethodReferenceInfo methodReferenceInfo)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[");
            foreach (ParameterReferenceInfo item in methodReferenceInfo.Parameters)
            {
                GenericInfo generic = GenericInfo.GenerateGeneric(item.TypeName);
                generic.ClearNameSpaces(ClearString);
                builder.Append($"new ParameterInfo<{generic.ToString()}>(),");
            }
            builder.Remove(builder.Length - 1, 1);
            builder.Append("]");
            return builder.ToString();
        }

        private string GenerateJsonInitializerParametersKeyValue(MethodReferenceInfo methodReferenceInfo, bool generateKey)
        {
            StringBuilder builder = new StringBuilder();
            foreach (ParameterReferenceInfo item in methodReferenceInfo.Parameters)
            {
                GenericInfo generic = GenericInfo.GenerateGeneric(item.TypeName);
                generic.ClearNameSpaces(ClearString);
                if (generateKey)
                    builder.Append($"{generic.ToString()} {item.Name},");
                else
                    builder.Append($"{item.Name},");
            }
            builder.Remove(builder.Length - 1, 1);
            return builder.ToString();
        }

        public void GetnerateJsonTypeInitializer(string serviceName, NamespaceReferenceInfo namespaceReferenceInfo, StringBuilder builder)
        {
            foreach (string item in JsonAllTypes)
            {
                GetnerateJsonType(serviceName, namespaceReferenceInfo, builder, item);
            }

            while (AddTypesLater.Count > 0)
            {
                bool isChanged = false;
                foreach (Func<bool, bool> item in AddTypesLater.ToArray())
                {
                    bool created = item(false);
                    if (created)
                    {
                        AddTypesLater.Remove(item);
                        isChanged = true;
                    }
                }
                if (!isChanged)
                {
                    if (AddTypesLater.Count > 0)
                    {
                        isChanged = false;
                        foreach (Func<bool, bool> item in AddTypesLater.ToArray())
                        {
                            bool created = item(true);
                            if (created)
                            {
                                AddTypesLater.Remove(item);
                                isChanged = true;
                            }
                        }
                    }
                    if (!isChanged)
                        break;
                }
            }
        }

        private int generatedJsonIndex = 0;

        private void GetnerateJsonType(string serviceName, NamespaceReferenceInfo namespaceReferenceInfo, StringBuilder builder, string name)
        {
            if (JsonAllGeneratedTypes.Contains(name) ||
                name == "String" || name == "bool" || name == "T" || name == "int" || name == "double" || name == "DateTime")
                return;

            generatedJsonIndex++;

            JsonAllGeneratedTypes.Add(name);
            GenericInfo generic = GenericInfo.GenerateGeneric(name, GenericNumbericTemeplateType.Managed, (typeName) =>
            {
                if (typeName.Trim() == "List" || typeName.Trim() == "Map")
                    return false;
                return true;
            });
            foreach (GenericInfo item in generic.Childs)
            {
                GetnerateJsonType(serviceName, namespaceReferenceInfo, builder, item.ToString().Trim());
            }
            generatedJsonIndex++;

            if (generic.Name == "Map")
            {
                //code for map dictionary
            }
            else if (generic.Name == "List")
            {
                generic.ClearNameSpaces(ClearString);
                string genericName = generic.ToString();
                if (generic.Childs.Count > 0)
                {
                    string childName = generic.Childs.FirstOrDefault().ToString();
                    Func<bool, bool> func = (isForce) =>
                    {
                        if (JsonVariablesTypes.ContainsKey(childName))
                        {
                            generatedJsonIndex++;
                            builder.AppendLine($@"var typeInfo{generatedJsonIndex} = TypeBuilder.make<{genericName}>(TypeMode.Array)
          .addProperty<{childName}>(""Add"", TypeMode.Array, null,
              ({genericName} x, {childName} value) => x.add(value))
          .addGenericArgument({JsonVariablesTypes[childName]})
          .createInstance(() => new {genericName}())
          .getTypeFromcreateInstance(() => new {genericName}())
          .build();");
                            JsonVariablesTypes[genericName] = $"typeInfo{generatedJsonIndex}";
                            return true;
                        }
                        else if (isForce && !(name == "String" || name == "bool" || name == "T" || name == "int" || name == "double" || name == "DateTime"))
                        {
                            generatedJsonIndex++;
                            builder.AppendLine($@"var typeInfo{generatedJsonIndex} = TypeBuilder.make<{genericName}>(TypeMode.Array)
          .addProperty<{childName}>(""Add"", TypeMode.Array, null,
              ({genericName} x, {childName} value) => x.add(value))
          .createInstance(() => new {genericName}())
          .getTypeFromcreateInstance(() => new {genericName}())
          .build();");
                            JsonVariablesTypes[genericName] = $"typeInfo{generatedJsonIndex}";
                            return true;
                        }
                        return false;
                    };

                    if (JsonVariablesTypes.ContainsKey(childName))
                    {
                        bool result = func(false);
                        if (!result)
                            AddTypesLater.Add(func);
                    }
                    else
                        AddTypesLater.Add(func);
                }
            }
            else
            {
                string typeMode = "TypeMode.Object";
                string fullName = generic.ToString();
                string fullName2 = "";
                generic.ClearNameSpaces(ClearString);
                string genericName = generic.ToString();

                ClassReferenceInfo findClass = FindClassByName(namespaceReferenceInfo, fullName);
                string fileName = "";
                if (findClass == null)
                {
                    if (generic.Childs.Count > 0)
                    {
                        if (generic.Childs.Count == 1)
                            fullName2 = generic.Name + "<T>";
                        else
                        {
                            fullName2 = generic.Name + "<T,";
                            int i = 2;
                            foreach (GenericInfo item in generic.Childs.Skip(1))
                            {
                                fullName2 += " T" + i + ",";
                            }
                            fullName2 = fullName2.Remove(fullName2.Length - 1, 1);
                            fullName2 += ">";
                        }
                    }
                    fileName = generic.Name + ".dart";
                    findClass = FindClassByName(namespaceReferenceInfo, fullName2);
                }
                else
                {
                    fileName = findClass.NormalizedName + ".dart";
                }
                string createInstance = "";
                if (findClass != null)
                {
                    string import = $"import 'package:{serviceName}/{findClass.NameSpace}/{fileName}';";
                    if (!JsonAllTypesImports.Contains(import))
                        JsonAllTypesImports.Add(import);
                    createInstance = $".createInstance(() => new {genericName}())";
                    AddToImport(import);
                }
                else
                {
                    typeMode = "TypeMode.Enum";
                    EnumReferenceInfo findEnum = FindEnumByName(namespaceReferenceInfo, fullName);
                    if (findEnum != null)
                    {
                        string import = $"import 'package:{serviceName}/{findEnum.NameSpace}/{findEnum.Name}.dart';";
                        if (!JsonAllTypesImports.Contains(import))
                            JsonAllTypesImports.Add(import);
                        AddToImport(import);
                    }
                    else
                        return;
                    createInstance = $".createInstance(() => {genericName}.values)";
                }
                StringBuilder properties = new StringBuilder();
                GenerateJsonProperties(properties, findClass, generic);
                GenerateJsonBaseClasses(properties, namespaceReferenceInfo, findClass, generic);
                if (JsonVariablesTypes.ContainsKey(genericName))
                    throw new Exception($"Type {genericName} is duplicate but same name,please rename it and try again");
                JsonVariablesTypes.Add(genericName, $"typeInfo{ generatedJsonIndex}");
                builder.AppendLine($@"var typeInfo{generatedJsonIndex} = TypeBuilder.make<{genericName}>({typeMode})
      {properties.ToString()}
      {createInstance}
      .build();");
            }

            generic = GenericInfo.GenerateGeneric(name, GenericNumbericTemeplateType.Managed, (typeName) =>
            {
                if (typeName == "List" || typeName == "Map")
                    return false;
                return true;
            });

        }

        private void GenerateJsonProperties(StringBuilder builder, ClassReferenceInfo classReferenceInfo, GenericInfo generic)
        {
            if (classReferenceInfo == null)
                return;
            foreach (PropertyReferenceInfo property in classReferenceInfo.Properties)
            {
                if (property.ReturnTypeName == "T" || (property.ReturnTypeName.StartsWith("T") && int.TryParse(property.ReturnTypeName.Substring(1), out int detected)))
                    continue;
                string addPropertyName = "addProperty";
                string instancePropertyContent = "";
                if (property.ReturnTypeName.StartsWith("List<"))
                {
                    addPropertyName = "addPropertyWithInstance";
                    instancePropertyContent = $@",
          () => new {property.ReturnTypeName}()";
                }
                builder.AppendLine($@".{addPropertyName}<{property.ReturnTypeName}>(
          ""{property.Name.ToCamelCase()}"",
          TypeMode.Object,
          ({classReferenceInfo.NormalizedName} x) => x.{property.Name.ToCamelCase()},
          ({classReferenceInfo.NormalizedName} x, {property.ReturnTypeName} value) => x.{property.Name.ToCamelCase()} = value{instancePropertyContent})");
            }
            if (generic.Childs.Count > 0)
            {
                int index = 0;
                foreach (PropertyReferenceInfo property in classReferenceInfo.Properties)
                {
                    if (property.ReturnTypeName == "T" || (property.ReturnTypeName.StartsWith("T") && int.TryParse(property.ReturnTypeName.Substring(1), out int detected)))
                    {
                        string fullName = generic.ToString();
                        string child = generic.Childs[index].ToString();
                        string addPropertyName = "addProperty";
                        string instancePropertyContent = "";
                        if (child.StartsWith("List<"))
                        {
                            addPropertyName = "addPropertyWithInstance";
                            instancePropertyContent = $@",
                        () => new {child}()";
                        }
                        builder.AppendLine($@".{addPropertyName}<{child}>(
                        ""{property.Name.ToCamelCase()}"",
                        TypeMode.Object,
                        ({fullName} x) => x.{property.Name.ToCamelCase()},
                        ({fullName} x, {child} value) => x.{property.Name.ToCamelCase()} = value{instancePropertyContent})");
                        index++;
                    }
                }
            }
        }

        private void GenerateJsonBaseClasses(StringBuilder builder, NamespaceReferenceInfo namespaceReferenceInfo, ClassReferenceInfo classReferenceInfo, GenericInfo generic)
        {
            if (classReferenceInfo == null)
                return;
            if (!string.IsNullOrEmpty(classReferenceInfo.BaseClassName))
            {
                ClassReferenceInfo findClass = FindClassByName(namespaceReferenceInfo, classReferenceInfo.BaseClassName);
                if (findClass != null)
                {
                    GenericInfo g = GenericInfo.GenerateGeneric(classReferenceInfo.BaseClassName);
                    g.ClearNameSpaces(ClearString);
                    if (JsonVariablesTypes.TryGetValue(g.Name, out string variableName))
                    {
                        builder.AppendLine($".addBaseClass({variableName})");
                    }
                }
            }
        }

        private ClassReferenceInfo FindClassByName(NamespaceReferenceInfo namespaceReferenceInfo, string fullName)
        {
            ClassReferenceInfo find = namespaceReferenceInfo.Classes.FirstOrDefault(x => x.NameSpace + "." + x.NormalizedName == fullName);
            if (find == null)
                find = namespaceReferenceInfo.Classes.FirstOrDefault(x => x.NormalizedName == fullName);
            return find;
        }

        private EnumReferenceInfo FindEnumByName(NamespaceReferenceInfo namespaceReferenceInfo, string fullName)
        {
            return namespaceReferenceInfo.Enums.FirstOrDefault(x => x.NameSpace + "." + x.Name == fullName);
        }

        public string GetFileNameFromClassName(string name)
        {
            GenericInfo generic = GenericInfo.GenerateGeneric(name);
            generic.ClearNameSpaces(ClearString);
            return generic.Name.ToString() + ".dart";
        }

        private void GenerateMethod(string serviceName, MethodReferenceInfo methodInfo, string prefix, StringBuilder resultBuilder, bool doSemicolon, string baseServiceName, Dictionary<string, Dictionary<string, string>> nameSpaces, bool isAbstract, bool isDuplex, bool isAsync)
        {
            string returnTypeName = GetReturnTypeName(methodInfo.ReturnTypeName, baseServiceName, nameSpaces);
            //AddToDictionary(nameSpaces, returnTypeName);
            if (returnTypeName == "SignalGo.Shared.Http.ActionResult")
                return;
            if (isAbstract)
            {
                if (isAsync)
                    resultBuilder.AppendLine($"{prefix} Future<{returnTypeName}> {methodInfo.DuplicateName.ToCamelCase()}({GenerateMethodParameters(methodInfo, baseServiceName, nameSpaces)});");
                else
                    resultBuilder.AppendLine($"{prefix} {returnTypeName} {methodInfo.DuplicateName.ToCamelCase()}({GenerateMethodParameters(methodInfo, baseServiceName, nameSpaces)});");

                return;
            }
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"{prefix} Future<{returnTypeName}> {methodInfo.DuplicateName.ToCamelCase()}({GenerateMethodParameters(methodInfo, baseServiceName, nameSpaces)}) {{");
            if (isDuplex)
                builder.Append($@"return PostJsonToServerService.send<{returnTypeName}>('{serviceName}','{methodInfo.Name}',");
            else
                builder.Append($@"return PostJsonToServerService.post<{returnTypeName}>('{serviceName}/{methodInfo.Name}',");

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
                    builder.AppendLine(prefix + prefix + prefix + "\"" + item.Name + "\"" + ":" + item.Name);
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
            builder.AppendLine($"{prefix}{propertyInfo.ReturnTypeName} {propertyInfo.Name.ToCamelCase()};");

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
                { "bool","bool" },
                { "int","int" },
                { "system.int16","int" },
                { "system.int32","int" },
                { "system.int64","int" },
                { "system.string","String" },
                { "string","String" },
                { "long","int" },
                { "double","double" },
                { "byte","int" },
                { "short","int" },
                { "uint","int" },
                { "ushort","int" },
                { "sbyte","int" },
                { "ulong","int" },
                { "uint16","int" },
                { "uint32","int" },
                { "uint64","int" },
                { "uintptr","int" },
                { "intptr","int" },

                { "system.int16[]","List<int>" },
                { "system.int32[]","List<int>" },
                { "system.int64[]","List<int>" },
                { "byte[]","List<int>" },
                { "int[]","List<int>" },
                { "long[]","List<int>" },

                { "system.datetime","DateTime" },
                { "system.date","DateTime" },
                { "system.guid","String" },
                { "system.uri","String" },
            };
        private string GetReturnTypeName(string name, string serviceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
        {
            string baseBaseName = name;

            name = name.Replace("?", "");
            if (fixedReturnTypes.ContainsKey(name.ToLower()))
                name = name.Replace(name, fixedReturnTypes[name.ToLower()], StringComparison.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string> item in fixedReturnTypes)
            {
                if (name.ToLower().Contains("<" + item.Key + ">"))
                    name = name.Replace("<" + item.Key + ">", "<" + item.Value + ">", StringComparison.OrdinalIgnoreCase);
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
                name = name.Replace("System.Collections.Generic.ICollection<", "List<");
            }
            if (name.Contains("System.Collections.Generic.List<"))
            {
                name = name.Replace("System.Collections.Generic.List<", "List<");
            }
            if (name.Contains("System.Collections.Generic.IEnumerable<"))
            {
                name = name.Replace("System.Collections.Generic.IEnumerable<", "List<");
            }
            if (name.Contains("System.Collections.Generic.Dictionary<"))
            {
                name = name.Replace("System.Collections.Generic.Dictionary<", "Map<");
                //name = "{}";
            }
            foreach (KeyValuePair<string, string> item in fixedReturnTypes)
            {
                string text1 = $"<{item.Key} ";
                string text5 = $"<{item.Key},";
                string text2 = $"{item.Key},";
                string text3 = $"{item.Key}>";
                string text4 = $",{item.Key}";
                if (name.ToLower().Contains(text1))
                    name = name.Replace(text1, $"<{item.Value}", StringComparison.OrdinalIgnoreCase);
                if (name.ToLower().Contains(text5))
                    name = name.Replace(text5, $"<{item.Value},", StringComparison.OrdinalIgnoreCase);
                if (name.ToLower().Contains(text2))
                    name = name.Replace(text2, $"{item.Value},", StringComparison.OrdinalIgnoreCase);
                if (name.ToLower().Contains(text3))
                    name = name.Replace(text3, $"{item.Value}>", StringComparison.OrdinalIgnoreCase);
                if (name.ToLower().Contains(text4))
                    name = name.Replace(text4, $",{item.Value}", StringComparison.OrdinalIgnoreCase);
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
                builder.Append($"{returnType} {item.Name}");
                index++;
            }
            return builder.ToString();
        }

        private void GenerateHttpServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, string baseServiceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
        {
            string serviceName = FirstCharToUpper(classReferenceInfo.ServiceName);
            string import = $"import 'package:{baseServiceName}/PostJsonToServerService.dart';";
            builder.AppendLine(import);
            AddToImport(import);
            string name = serviceName.Replace("/", "").Replace("\\", "") + "Service";
            //generate abstract class
            builder.AppendLine(prefix + "abstract class " + name + "Base { ");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateMethod(serviceName, methodInfo, prefix + prefix, builder, false, baseServiceName, nameSpaces, true, false, true);
            }
            builder.AppendLine(prefix + "}");

            //generate main class with post
            builder.AppendLine(prefix + "class " + name + $" implements {name}Base {{ ");
            builder.AppendLine($"static {name}Base current;");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateMethod(serviceName, methodInfo, prefix + prefix, builder, false, baseServiceName, nameSpaces, false, false, true);
            }
            builder.AppendLine(prefix + "}");

            //generate main class with post
            builder.AppendLine(prefix + "class " + name + $"Duplex implements {name}Base {{ ");
            builder.AppendLine($"static {name}Base current;");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateMethod(serviceName, methodInfo, prefix + prefix, builder, false, baseServiceName, nameSpaces, false, true, true);
            }
            builder.AppendLine(prefix + "}");
        }

        private void GenerateCallbackServiceClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, string baseServiceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
        {
            string name = classReferenceInfo.NormalizedName;
            if (name.StartsWith("I"))
                name = name.Remove(0, 1);
            if (!name.EndsWith("CallbackService"))
                name += "CallbackService";
            name = FirstCharToUpper(name);
            //generate abstract class
            builder.AppendLine(prefix + "abstract class " + name + "Base { ");
            builder.AppendLine($"static String name = \"{classReferenceInfo.ServiceName}\";");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateMethod(name, methodInfo, prefix + prefix, builder, false, baseServiceName, nameSpaces, true, false, false);
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
            builder.AppendLine(prefix + "enum " + enumReferenceInfo.Name + " {");//+ " : " + enumReferenceInfo.TypeName
            foreach (SignalGo.Shared.Models.KeyValue<string, string> name in enumReferenceInfo.KeyValues)
            {
                builder.AppendLine($"{prefix + prefix}{name.Key},");
            }
            builder.AppendLine(prefix + "}");
            builder.AppendLine();
        }

        private string AddToDictionary(Dictionary<string, Dictionary<string, string>> keyValuePairs, string fullName)
        {
            if (string.IsNullOrEmpty(fullName) || fullName == "SignalGo.Shared.Http.ActionResult")
                return fullName;
            GenericInfo generic = GenericInfo.GenerateGeneric(fullName, GenericNumbericTemeplateType.Managed, (name) =>
            {
                if (name.Trim() == "List" || name.Trim() == "Map")
                    return false;
                return true;
            });

            Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();
            generic.GetNameSpaces(data, ClearString);
            foreach (KeyValuePair<string, List<string>> item in data)
            {
                foreach (string value in item.Value)
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
            AddGenericToJsonAllTypes(fullName);
            generic.ClearNameSpaces(ClearString);
            string result = generic.ToString();
            if (!JsonAllTypes.Contains(fullName.Trim()))
                JsonAllTypes.Add(fullName.Trim());
            return result;
        }

        private void AddGenericToJsonAllTypes(string fullName)
        {
            GenericInfo generic = GenericInfo.GenerateGeneric(fullName, GenericNumbericTemeplateType.Skip);
            if (!JsonAllTypes.Contains(fullName.Trim()))
                JsonAllTypes.Add(fullName.Trim());
            foreach (var item in generic.Childs)
            {
                AddGenericToJsonAllTypes(item.ToString());
            }
        }

        private List<string> GeneratedModels { get; set; } = new List<string>();

        private void GenerateModelClass(ClassReferenceInfo classReferenceInfo, string prefix, StringBuilder builder, MapDataClassInfo mapDataClassInfo, string baseServiceName, Dictionary<string, Dictionary<string, string>> nameSpaces)
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
            {
                GenericInfo generic = GenericInfo.GenerateGeneric(classReferenceInfo.BaseClassName);
                generic.ClearNameSpaces(ClearString);
                string typeName = generic.ToString();
                AddToDictionary(nameSpaces, classReferenceInfo.BaseClassName);

                baseName = " extends " + typeName;
            }
            builder.AppendLine(prefix + "class " + classReferenceInfo.NormalizedName + baseName + "{");
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