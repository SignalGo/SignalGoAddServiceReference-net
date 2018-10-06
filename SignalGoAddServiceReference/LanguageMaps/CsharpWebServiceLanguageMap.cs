﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SignalGoAddServiceReference.LanguageMaps.CsharpWebService
{
    public class XMLToCsharp
    {
        public List<ClassInfo> Generate(string url)
        {
            using (WebClient client = new WebClient())
            {
                XDocument doc = XDocument.Parse(client.DownloadString(url));
                string className = doc.Elements().FirstOrDefault().Attribute("name").Value;
                BaseClassInfo = new ClassInfo() { Name = className, TargetNameSpace = doc.Elements().FirstOrDefault().Attribute("targetNamespace").Value, Url = url.Substring(0, url.LastIndexOf("wsdl", StringComparison.OrdinalIgnoreCase)) + "service" };
                ClassesGenerated.Add(BaseClassInfo);
                XmlReader(doc);

                return ClassesGenerated;
            }
        }

        public string GeneratesharpCode()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (ClassInfo classInfo in ClassesGenerated)
            {
                stringBuilder.AppendLine($"\tpublic class {classInfo.Name}");
                stringBuilder.AppendLine("\t{");
                if (!string.IsNullOrEmpty(classInfo.TargetNameSpace))
                {
                    stringBuilder.AppendLine("\t\tpublic string TargetNameSpace { get; set; } = \"" + classInfo.TargetNameSpace + "\";");
                }
                if (!string.IsNullOrEmpty(classInfo.Url))
                {
                    stringBuilder.AppendLine("\t\tpublic string Url { get; set; } = \"" + classInfo.Url + "\";");
                }
                foreach (MethodInfo method in classInfo.Methods)
                {
                    stringBuilder.AppendLine($"\t\tpublic {method.ReturnType} {method.Name}({GetParameterString(method.ParameterInfoes)})");
                    stringBuilder.AppendLine("\t\t{");
                    stringBuilder.AppendLine($"\t\treturn SignalGo.Client.WebServiceProtocolHelper.CallWebServiceMethod<{(method.ReturnClassType == null ? method.ReturnType : method.ReturnClassType.Name)}>(Url, TargetNameSpace,\"{method.Name}\", new SignalGo.Shared.Models.ParameterInfo[]");
                    stringBuilder.AppendLine("\t\t{");
                    foreach (ParameterInfo parameter in method.ParameterInfoes)
                    {
                        stringBuilder.AppendLine($"\t\t new SignalGo.Shared.Models.ParameterInfo(){{ Name = \"{parameter.Name}\",Value = {parameter.Name}.ToString() }},");
                    }
                    stringBuilder.AppendLine("\t\t});");
                    stringBuilder.AppendLine("\t\t}");
                }
                foreach (PropertyInfo property in classInfo.PropertyInfoes)
                {
                    stringBuilder.AppendLine($"\t\tpublic {property.ReturnType} {property.Name}{{ get; set; }}");
                }
                stringBuilder.AppendLine("\t}");
            }
            return stringBuilder.ToString();
        }

        private string GetParameterString(List<ParameterInfo> parameterInfoes)
        {
            List<string> result = new List<string>();
            foreach (ParameterInfo item in parameterInfoes)
            {
                result.Add(item.Type + " " + item.Name);
            }
            return string.Join(", ", result);
        }

        private List<ClassInfo> ClassesGenerated { get; set; } = new List<ClassInfo>();
        private ClassInfo BaseClassInfo { get; set; }
        private void XmlReader(XContainer doc)
        {
            List<XElement> items = doc.Elements().ToList();
            foreach (XElement item in items)
            {
                if (item.Name.LocalName == "types")
                {
                    GenerateMethods(item.Elements().ToList());
                }
                XmlReader(item);
            }
        }

        private void GenerateMethods(List<XElement> items)
        {
            foreach (XElement item in items)
            {
                GenerateMethods(item.Elements().ToList());
                if (item.Name.LocalName == "element")
                {
                    XElement parent = FindMethodName(item);
                    string methodName = parent.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals("name")).Value;
                    if (BaseClassInfo.SkipMethods.Contains(methodName))
                        continue;
                    string methodReponse = GetMethodResponse(parent, methodName, out ClassInfo returnType);
                    if (!string.IsNullOrEmpty(methodReponse))
                        BaseClassInfo.Methods.Add(new MethodInfo() { ReturnType = methodReponse, Name = methodName, ParameterInfoes = GetMethodParameters(parent), ReturnClassType = returnType });
                    Console.WriteLine(item.NodeType + " " + item);
                }
            }
        }

        private string GetMethodResponse(XElement element, string name, out ClassInfo returnType)
        {
            returnType = null;
            XElement node = (XElement)element.NextNode;
            if (node == null)
                return null;
            XAttribute attribute = node.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals("name"));
            if (attribute != null && attribute.Value == name + "Response")
            {
                string className = name + "Response";
                if (BaseClassInfo.SkipMethods.Contains(className))
                    return null;
                BaseClassInfo.SkipMethods.Add(className);
                List<XElement> elements = new List<XElement>();
                FindAllElements(node, elements);
                if (elements.Count > 1)
                {
                    StringBuilder classBuilder = new StringBuilder();
                    ClassInfo classInfo = new ClassInfo() { Name = className };
                    foreach (XElement item in elements)
                    {
                        classInfo.PropertyInfoes.Add(new PropertyInfo() { ReturnType = CleanType(item.Attribute("type").Value), Name = item.Attribute("name").Value });
                    }
                    ClassesGenerated.Add(classInfo);
                    returnType = classInfo;
                    return className;
                }
                else if (elements.Count == 1)
                {
                    XElement first = elements.First();
                    return CleanType(first.Attribute("type").Value);
                }
                else
                {
                    return "void";
                }
            }
            return "void";
        }

        private string CleanType(string value)
        {
            if (value.Contains(":"))
            {
                return value.Substring(value.LastIndexOf(":") + 1);
            }
            return value;
        }

        private void FindAllElements(XElement element, List<XElement> elements)
        {
            foreach (XElement item in element.Elements())
            {
                if (item.Name.LocalName == "element")
                    elements.Add(item);
                FindAllElements(item, elements);
            }
        }

        private List<ParameterInfo> GetMethodParameters(XElement element)
        {
            List<ParameterInfo> result = new List<ParameterInfo>();
            List<XElement> elements = new List<XElement>();
            FindAllElements(element, elements);
            foreach (XElement item in elements)
            {
                result.Add(new ParameterInfo() { Name = item.Attribute("name").Value, Type = CleanType(item.Attribute("type").Value) });
            }
            return result;
        }

        private XElement FindMethodName(XElement element)
        {
            XElement name = null;
            XElement parent = element.Parent;
            while (parent != null && name == null)
            {
                XAttribute find = parent.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals("name"));
                if (find != null)
                    name = parent;
                parent = parent.Parent;
            }
            return name;
        }
    }

    public class ClassInfo
    {
        public string TargetNameSpace { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();
        public List<PropertyInfo> PropertyInfoes { get; set; } = new List<PropertyInfo>();
        public List<string> SkipMethods { get; set; } = new List<string>();
    }

    public class PropertyInfo
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
    }

    public class MethodInfo
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public ClassInfo ReturnClassType { get; set; }
        public List<ParameterInfo> ParameterInfoes { get; set; } = new List<ParameterInfo>();
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

}
