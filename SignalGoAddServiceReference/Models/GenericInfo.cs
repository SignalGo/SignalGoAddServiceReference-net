using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGoAddServiceReference.Models
{
    public class GenericInfo
    {
        public string Name { get; set; }
        public List<GenericInfo> Childs { get; set; }
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (Childs.Count > 0)
            {
                stringBuilder.Append(Name);
                stringBuilder.Append('<');
                foreach (var item in Childs)
                {
                    stringBuilder.Append(item.ToString());
                    if (Childs.Last() != item)
                        stringBuilder.Append(',');
                }
                stringBuilder.Append('>');
            }
            else
            {
                stringBuilder.Append(Name);
            }
            return stringBuilder.ToString();
        }

        public static GenericInfo GenerateGeneric(string parent)
        {
            GenericInfo genericInfo = new GenericInfo
            {
                Name = GetName(parent),
                Childs = FindChilds(parent)
            };
            return genericInfo;
        }

        private static List<GenericInfo> FindChilds(string parent)
        {
            List<GenericInfo> result = new List<GenericInfo>();
            string childText = GetBetween(parent, '<', '>');
            List<string> split = Split(childText, '<', '>', ',');
            foreach (var item in split)
            {
                result.Add(GenerateGeneric(item));
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

        private static string GetName(string parent)
        {
            if (!parent.Contains('<'))
                return parent;
            int findIndex = parent.IndexOf('<');
            string fixName = TakeBlock(parent, '<', '>', '<', '>');
            int indexName = fixName.Count(x => x == '>' || x == '<' || x == ',');
            string getName = parent.Substring(0, findIndex);
            return getName + indexName;
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
