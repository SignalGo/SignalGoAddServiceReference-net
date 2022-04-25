using SignalGo.CodeGenerator.Helpers;
using SignalGo.CodeGenerator.Models;
using SignalGo.Shared.Models.ServiceReference;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalGo.CodeGenerator.LanguageMaps
{
    public class PostmanLanguageMap
    {
        static string GetTabs(int count)
        {
            return new string('\t', count);
        }

        public static string CalculateMapData(NamespaceReferenceInfo namespaceReferenceInfo, AddReferenceConfigInfo config)
        {
            ProjectInfoBase project = LanguageMapBase.GetCurrent.GetActiveProject();
            List<MapDataClassInfo> MapDataClassInfoes = new List<MapDataClassInfo>();
            List<string> attributesForAll = new List<string>();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($@"{{
{GetTabs(1)}""info"": {{
{GetTabs(2)}""_postman_id"": ""93f204d3-4545-471c-953c-092514b2ca22"",
{GetTabs(2)}""name"": ""{config.ServiceNameSpace}"",
{GetTabs(2)}""schema"": ""https://schema.getpostman.com/json/collection/v2.1.0/collection.json""
{GetTabs(1)}}},
{GetTabs(1)}""item"": [");
            foreach (ClassReferenceInfo httpClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.HttpServiceLevel))
            {
                builder.AppendLine($"{GetTabs(2)}{{");
                GenerateHttpServiceClass(httpClassInfo, builder);
                builder.AppendLine($"{GetTabs(2)}}},");
            }
            if (builder[builder.Length - 3] == ',')
                builder.Remove(builder.Length - 3, 1);
            builder.AppendLine($"{GetTabs(1)}]");
            builder.AppendLine($"}}");
            return builder.ToString();
        }

        private static void GenerateHttpMethod(string serviceName, MethodReferenceInfo methodInfo, StringBuilder builder)
        {
            builder.AppendLine($"{GetTabs(4)}{{");
            builder.AppendLine($"{GetTabs(5)}\"name\": \"{methodInfo.GetMethodName()}\",");
            builder.AppendLine($@"{GetTabs(5)}""request"": {{
{GetTabs(6)}""method"": ""POST"",
{GetTabs(6)}""header"": [],
{GetTabs(6)}""body"": {{
{GetTabs(6)}	""mode"": ""raw"",
{GetTabs(6)}	""raw"": ""{GenerateMethodParameter(methodInfo)}"",
{GetTabs(6)}	""options"": {{
{GetTabs(6)}		""raw"": {{
{GetTabs(6)}			""language"": ""json""
{GetTabs(6)}		}}
{GetTabs(6)}	}}
{GetTabs(6)}}},
{GetTabs(6)}""url"": {{
{GetTabs(6)}	""raw"": ""{{{{endpoint}}}}/{serviceName}/{methodInfo.GetMethodName()}"",
{GetTabs(6)}	""host"": [
{GetTabs(6)}	""{{{{endpoint}}}}""
{GetTabs(6)}	],
{GetTabs(6)}	""path"": [
{GetTabs(6)}		""{serviceName}"",
{GetTabs(6)}		""{methodInfo.GetMethodName()}""
{GetTabs(6)}	]
{GetTabs(6)}}}
{GetTabs(5)}}}
{GetTabs(4)}}},");
        }

        private static string GenerateMethodParameter(MethodReferenceInfo methodInfo)
        {
            StringBuilder rawBuilder = new StringBuilder();
            rawBuilder.Append("{\\n");
            bool isFirst = true;
            foreach (var parameter in methodInfo.Parameters)
            {
                if (!isFirst)
                    rawBuilder.Append(",\\n");

                rawBuilder.Append($"\\\"{parameter.Name}\\\" : null");
                isFirst = false;
            }
            rawBuilder.Append("\\n}");
            return rawBuilder.ToString();
        }

        private static void GenerateHttpServiceClass(ClassReferenceInfo classReferenceInfo, StringBuilder builder)
        {
            builder.AppendLine($"{GetTabs(3)}\"name\": \"{classReferenceInfo.ServiceName}\",");
            builder.AppendLine($"{GetTabs(3)}\"item\": [");
            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateHttpMethod(classReferenceInfo.ServiceName, methodInfo, builder);
            }
            if (builder[builder.Length - 3] == ',')
                builder.Remove(builder.Length - 3, 1);
            builder.AppendLine($"{GetTabs(3)}]");
        }

    }
}
