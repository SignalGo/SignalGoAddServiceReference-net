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
    public class PostmanLanguageMap
    {
        public static string CalculateMapData(NamespaceReferenceInfo namespaceReferenceInfo, AddReferenceConfigInfo config)
        {
            ProjectInfoBase project = LanguageMapBase.GetCurrent.GetActiveProject();
            List<MapDataClassInfo> MapDataClassInfoes = new List<MapDataClassInfo>();
            List<string> attributesForAll = new List<string>();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($@"{{
	""info"": {{
		""_postman_id"": ""93f204d3-4545-471c-953c-092514b2ca22"",
		""name"": ""{config.ServiceNameSpace}"",
		""schema"": ""https://schema.getpostman.com/json/collection/v2.1.0/collection.json""
	}},
	""item"": [
		{{");
            foreach (ClassReferenceInfo httpClassInfo in namespaceReferenceInfo.Classes.Where(x => x.Type == ClassReferenceType.HttpServiceLevel))
            {
                GenerateHttpServiceClass(httpClassInfo, builder);
            }
            builder.AppendLine("}]}");
            return builder.ToString();
        }

        private static void GenerateHttpMethod(string serviceName, MethodReferenceInfo methodInfo, StringBuilder builder)
        {
            builder.AppendLine($"\"name\": \"{methodInfo.GetMethodName()}\",");
            builder.AppendLine($@"""request"": {{
						""method"": ""POST"",
						""header"": [],
						""body"": {{
							""mode"": ""raw"",
							""raw"": ""{GenerateMethodParameter(methodInfo)}"",
							""options"": {{
								""raw"": {{
									""language"": ""json""
								}}
							}}
						}},
						""url"": {{
							""raw"": ""{{{{endpoint}}}}/{serviceName}/{methodInfo.GetMethodName()}"",
							""host"": [
							""{{{{endpoint}}}}""
							],
							""path"": [
								""{serviceName}"",
								""{methodInfo.GetMethodName()}""
							]
						}}
					}}");
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
            builder.AppendLine($"\"name\": \"{classReferenceInfo.ServiceName}\",\"item\": [{{");

            foreach (MethodReferenceInfo methodInfo in classReferenceInfo.Methods)
            {
                GenerateHttpMethod(classReferenceInfo.ServiceName, methodInfo, builder);
            }
            builder.AppendLine("}]");
        }

    }
}
