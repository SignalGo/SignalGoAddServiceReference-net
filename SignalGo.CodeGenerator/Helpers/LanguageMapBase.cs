﻿using SignalGo.CodeGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SignalGo.CodeGenerator.Helpers
{
    public abstract class ProjectInfoBase
    {
        public ProjectItemsInfoBase ProjectItemsInfoBase { get; set; }
    }

    public abstract class ProjectItemsInfoBase
    {

    }

    public abstract class ProjectItemInfoBase
    {
        public abstract int GetFileCount();
        public abstract string GetFileName(short index);
    }

    public abstract class LanguageMapBase
    {
        protected internal static LanguageMapBase GetCurrent { get; set; }

        public abstract string GetAutoGeneratedText();

        public abstract string DownloadService(string servicePath, AddReferenceConfigInfo config);

        public abstract ProjectInfoBase GetActiveProject();

        public abstract List<ProjectItemInfoBase> GetAllProjectItemsWithoutServices(ProjectItemsInfoBase project);

        public static string ReplaceNameSpace(string nameSpace, AddReferenceConfigInfo config)
        {
            if (config.ReplaceNameSpaces != null)
            {
                var find = config.ReplaceNameSpaces.FirstOrDefault(x => x.From == nameSpace);
                if (find != null)
                {
                    return find.To;
                }
            }
            return nameSpace;
        }
        public static string[] GetCustomNameSpaces(AddReferenceConfigInfo config)
        {
            if (!string.IsNullOrEmpty(config.CustomNameSpaces))
            {
                return Regex.Split(config.CustomNameSpaces, "\r\n");
            }
            return null;
        }
    }

    public static class StringExtensions
    {
        public static string ToCamelCase(this string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            return name.Substring(0, 1).ToLower() + name.Substring(1);
        }

        public static string Replace(this string source, string oldValue, string newValue, StringComparison comparisonType)
        {
            if (source.Length == 0 || oldValue.Length == 0)
                return source;

            var result = new System.Text.StringBuilder();
            int startingPos = 0;
            int nextMatch;
            while ((nextMatch = source.IndexOf(oldValue, startingPos, comparisonType)) > -1)
            {
                result.Append(source, startingPos, nextMatch - startingPos);
                result.Append(newValue);
                startingPos = nextMatch + oldValue.Length;
            }
            result.Append(source, startingPos, source.Length - startingPos);

            return result.ToString();
        }
    }
}
