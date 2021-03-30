using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalGo.CodeGenerator.Models
{
    public enum GenericNumbericTemeplateType : byte
    {
        Skip = 0,
        DoNumberic = 1,
        Managed = 2
    }
    /// <summary>
    /// handle full generic class string to object access
    /// for example List<Message<Data>>
    /// </summary>
    public class GenericInfo
    {
        public string Name { get; set; }
        public List<GenericInfo> Childs { get; set; }
        public GenericNumbericTemeplateType DoNumbericTemplate { get; internal set; } = GenericNumbericTemeplateType.DoNumberic;
        public Func<string, bool> CanDoNumbericFunction { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (Childs.Count > 0)
            {
                stringBuilder.Append(Name.Contains(".") ? ("global::" + Name) : Name);
                stringBuilder.Append('<');
                foreach (GenericInfo item in Childs)
                {
                    stringBuilder.Append(item.ToString());
                    if (Childs.Last() != item)
                        stringBuilder.Append(',');
                }
                stringBuilder.Append('>');
            }
            else
            {
                stringBuilder.Append(Name.Contains(".") ? ("global::" + Name) : Name);
            }
            return stringBuilder.ToString();
        }

        public void ClearNameSpaces(Func<string, string> clearString)
        {
            int index = Name.LastIndexOf('.');
            if (index >= 0)
            {
                string name = clearString(Name.Substring(index + 1));
                Name = name.Contains(".") ? name.Replace("global::", "") : name;
            }
            if (Childs != null)
            {
                foreach (GenericInfo item in Childs)
                {
                    item.ClearNameSpaces(clearString);
                }
            }
        }

        public void ReplaceNameSpaces(Func<string, AddReferenceConfigInfo, string> clearNameSapceString, AddReferenceConfigInfo configInfo)
        {
            Name = Name.Trim();
            if (Name.Contains("."))
            {
                int index = Name.LastIndexOf('.');
                var nameSpace = Name.Substring(0, index);
                string name = Name.Substring(index + 1);
                Name = clearNameSapceString(nameSpace, configInfo) + "." + name;
            }

            if (Childs != null)
            {
                foreach (GenericInfo item in Childs)
                {
                    item.ReplaceNameSpaces(clearNameSapceString, configInfo);
                }
            }
        }

        public void ClearNamesAndTypes(Func<string, string> clearString)
        {
            string name = clearString(Name);
            Name = name;
            if (Childs != null)
            {
                foreach (GenericInfo item in Childs)
                {
                    item.ClearNamesAndTypes(clearString);
                }
            }
        }

        public void GetNameSpaces(Dictionary<string, List<string>> keyValuePairs, Func<string, string> clearString)
        {
            int index = Name.LastIndexOf('.');
            if (index >= 0)
            {
                string nameSpace = clearString(Name.Substring(0, index));
                if (!keyValuePairs.TryGetValue(nameSpace, out List<string> items))
                {
                    keyValuePairs.Add(nameSpace, new List<string>() { Name.Substring(index + 1) });
                }
                else
                {
                    if (!items.Contains(Name.Substring(index + 1)))
                        items.Add(Name.Substring(index + 1));
                }
            }
            if (Childs != null)
            {
                foreach (GenericInfo item in Childs)
                {
                    item.GetNameSpaces(keyValuePairs, clearString);
                }
            }
        }

        public static GenericInfo GenerateGeneric(string parent, GenericNumbericTemeplateType doNumericTemplate = GenericNumbericTemeplateType.DoNumberic, Func<string, bool> canDoNumbericFunction = null)
        {
            GenericInfo genericInfo = new GenericInfo
            {
                DoNumbericTemplate = doNumericTemplate
            };
            genericInfo.CanDoNumbericFunction = canDoNumbericFunction;
            genericInfo.Childs = genericInfo.FindChilds(parent);
            genericInfo.Name = genericInfo.GetName(parent);
            return genericInfo;
        }

        private List<GenericInfo> FindChilds(string parent)
        {
            List<GenericInfo> result = new List<GenericInfo>();
            string childText = GetBetween(parent, '<', '>');
            List<string> split = Split(childText, '<', '>', ',');
            foreach (string item in split)
            {
                result.Add(GenerateGeneric(item, DoNumbericTemplate, CanDoNumbericFunction));
            }
            return result;
        }

        private static string GetBetween(string text, char start, char end)
        {
            int indexToBreak = 0;
            StringBuilder result = new StringBuilder();
            bool isStarted = false;
            foreach (char item in text)
            {
                if (item == end && indexToBreak == 1)
                    break;
                if (isStarted)
                    result.Append(item);
                if (isStarted)
                {
                    if (item == start)
                        indexToBreak++;
                    else if (item == end)
                        indexToBreak--;
                    if (indexToBreak <= 0)
                        break;
                }
                else if (item == start)
                {
                    isStarted = true;
                    indexToBreak++;
                }
            }
            return result.ToString();
        }

        private static List<string> Split(string text, char start, char end, char splitChar)
        {
            List<string> result = new List<string>();
            int indexToBreak = 0;
            StringBuilder childText = new StringBuilder();
            foreach (char item in text)
            {
                if (item == splitChar && indexToBreak == 0)
                {
                    result.Add(childText.ToString());
                    childText.Clear();
                }
                else
                {
                    childText.Append(item);
                    if (item == start)
                        indexToBreak++;
                    else if (item == end)
                        indexToBreak--;
                }
            }
            if (childText.ToString().Trim().Length > 0)
                result.Add(childText.ToString());
            return result;
        }

        private string GetName(string parent)
        {
            if (!parent.Contains('<'))
                return parent;
            int findIndex = parent.IndexOf('<');
            string fixName = TakeBlock(parent, '<', '>', '<', '>');
            int indexName = fixName.Count(x => x == '>' || x == '<' || x == ',');
            string getName = parent.Substring(0, findIndex);
            if (DoNumbericTemplate == GenericNumbericTemeplateType.DoNumberic)
                return getName + indexName;
            else if (DoNumbericTemplate == GenericNumbericTemeplateType.Managed)
            {
                if (CanDoNumbericFunction(getName))
                    return getName + indexName;
                else
                    return getName;
            }
            else
                return getName;
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
    }

}
