using System;
using System.Collections.Generic;
using System.Text;

namespace FairyGUI.Utils
{
    //A simplest & readonly XML class
    public class XML
    {
        public string name { get; private set; }
        public string text { get; private set; }

        Dictionary<string, string> _attributes;
        XMLList _children;

        static char[] commaSplitter = new char[] { ',' };
        const string CDATA_START = "<![CDATA[";
        const string CDATA_END = "]]>";

        public XML(string text)
        {
            Parse(text);
        }

        private XML()
        {
        }

        public bool hasAttribute(string attrName)
        {
            if (_attributes == null)
                return false;

            return _attributes.ContainsKey(attrName);
        }

        public string GetAttribute(string attrName)
        {
            return GetAttribute(attrName, null);
        }

        public string GetAttribute(string attrName, string defValue)
        {
            if (_attributes == null)
                return defValue;

            string ret;
            if (_attributes.TryGetValue(attrName, out ret))
                return ret;
            else
                return defValue;
        }

        public int GetAttributeInt(string attrName)
        {
            return GetAttributeInt(attrName, 0);
        }

        public int GetAttributeInt(string attrName, int defValue)
        {
            string value = GetAttribute(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            int ret;
            if (int.TryParse(value, out ret))
                return ret;
            else
                return defValue;
        }

        public bool GetAttributeBool(string attrName)
        {
            return GetAttributeBool(attrName, false);
        }

        public bool GetAttributeBool(string attrName, bool defValue)
        {
            string value = GetAttribute(attrName);
            if (value == null || value.Length == 0)
                return defValue;

            bool ret;
            if (bool.TryParse(value, out ret))
                return ret;
            else
                return defValue;
        }

        public string[] GetAttributeArray(string attrName)
        {
            string value = GetAttribute(attrName);
            if (value != null)
                return value.Split(commaSplitter);
            else
                return null;
        }

        public void SetAttribute(string attrName, string attrValue)
        {
            if (_attributes == null)
                _attributes = new Dictionary<string, string>();

            _attributes[attrName] = attrValue;
        }

        public XML GetNode(string selector)
        {
            if (_children == null)
                return null;
            else
                return _children.Find(selector);
        }

        public XMLList Elements()
        {
            if (_children == null)
                _children = new XMLList();
            return _children;
        }

        public XMLList Elements(string selector)
        {
            if (_children == null)
                _children = new XMLList();
            return _children.Filter(selector);
        }

        static Stack<XML> sNodeStack = new Stack<XML>();
        void Parse(string aSource)
        {
            int pos = 0, pos2, length = 0, tagType = 0;
            string tagName = null;
            string text = null;
            XML lastOpenNode = null;
            sNodeStack.Clear();

            while ((pos2 = EnumTag(aSource, pos, ref tagName, ref length, ref tagType)) != -1)
            {
                if (pos != pos2)
                    text = aSource.Substring(pos, pos2 - pos);
                else
                    text = string.Empty;

                if (tagType == 0 || tagType == 2)
                {
                    XML childNode;
                    if (lastOpenNode != null)
                        childNode = new XML();
                    else
                    {
                        if (this.name != null)
                        {
                            Cleanup();
                            throw new Exception("Invalid xml format - no root node.");
                        }
                        childNode = this;
                    }

                    childNode.name = tagName;
                    childNode.GetAttributes(aSource.Substring(pos2, length));

                    if (lastOpenNode != null)
                    {
                        if (tagType != 2)
                            sNodeStack.Push(lastOpenNode);
                        if (lastOpenNode._children == null)
                            lastOpenNode._children = new XMLList();
                        lastOpenNode._children.Add(childNode);
                    }
                    if (tagType != 2)
                        lastOpenNode = childNode;
                }
                else if (tagType == 1)
                {
                    if (lastOpenNode == null || lastOpenNode.name != tagName)
                    {
                        Cleanup();
                        throw new Exception("Invalid xml format - <" + tagName + "> dismatched.");
                    }

                    if (lastOpenNode._children == null || lastOpenNode._children.Count == 0)
                    {
                        if (text.StartsWith(CDATA_START))
                            text = text.Substring(9, text.Length - 12);
                        lastOpenNode.text = DecodeString(text);
                    }

                    if (sNodeStack.Count > 0)
                        lastOpenNode = sNodeStack.Pop();
                    else
                        lastOpenNode = null;
                }

                pos = pos2 + length;
            }
        }

        void Cleanup()
        {
            this.name = null;
            if (this._attributes != null)
                this._attributes.Clear();
            if (this._children != null)
                this._children.Clear();
            this.text = null;
        }

        //tag type:0 - start tag, 1 - end tag, 2 - empty content tag, 3 - declaration
        static int EnumTag(string aSource, int aOffset, ref string aTagName, ref int aLength, ref int aTagType)
        {
            int sourceLen = aSource.Length;
            if (aOffset >= sourceLen)
                return -1;

            StringBuilder sb = new StringBuilder();
            int pos;
            aTagType = 0;
            while ((pos = aSource.IndexOf('<', aOffset)) != -1)
            {
                aOffset = pos;
                pos++;

                if (pos == sourceLen)
                    return -1;

                if (aSource[pos] == '!')
                {
                    if (sourceLen > pos + 7 && aSource.Substring(pos - 1, 9) == CDATA_START)
                    {
                        pos = aSource.IndexOf(CDATA_END, pos);
                        if (pos == -1)
                            return -1;
                        else
                        {
                            aOffset = pos + 1;
                            continue;
                        }
                    }
                }
                else if (aSource[pos] == '/')
                {
                    pos++;
                    aTagType = 1;
                }

                for (; pos < sourceLen; pos++)
                {
                    char c = aSource[pos];
                    if (Char.IsWhiteSpace(c) || c == '>' || c == '/')
                        break;
                }
                if (pos == sourceLen)
                    return -1;

                sb.Append(aSource, aOffset + 1, pos - aOffset - 1);
                if (sb.Length > 0 && sb[0] == '/')
                    sb.Remove(0, 1);

                bool singleQuoted = false, doubleQuoted = false;
                int possibleEnd = -1;
                for (; pos < sourceLen; pos++)
                {
                    char c = aSource[pos];
                    if (c == '"')
                    {
                        if (!singleQuoted)
                            doubleQuoted = !doubleQuoted;
                    }
                    else if (c == '\'')
                    {
                        if (!doubleQuoted)
                            singleQuoted = !singleQuoted;
                    }

                    if (c == '>')
                    {
                        if (!(singleQuoted || doubleQuoted)) 
                        {
                            possibleEnd = -1;
                            break;
                        }

                        possibleEnd = pos;
                    }
                    else if (c == '<')
                        break;
                }
                if (possibleEnd != -1)
                    pos = possibleEnd;

                if (pos == sourceLen)
                    return -1;

                aLength = pos - aOffset + 1;
                if (aSource[pos - 1] == '/')
                    aTagType = 2;
                else if (aSource[pos - 1] == '?')
                    aTagType = 3;
                else if (aTagType != 1)
                    aTagType = 0;
                aTagName = sb.ToString();
                return aOffset;
            }

            return -1;
        }

        void GetAttributes(string aSource)
        {
            int sourceLen = aSource.Length;
            int attrStart = -1, attrEnd = -1;
            bool waitValue = false;
            int i = 0;
            StringBuilder sb = new StringBuilder();

            if (i < sourceLen && aSource[i] == '<')
            {
                for (; i < sourceLen; i++)
                {
                    char c = aSource[i];
                    if (Char.IsWhiteSpace(c) || c == '>' || c == '/')
                        break;
                }
            }

            for (; i < sourceLen; i++)
            {
                char c = aSource[i];
                if (c == '=')
                {
                    int valueStart = i + 1;
                    int valueEnd = -1;
                    bool started = false, singleQuoted = false, doubleQuoted = false;
                    for (int j = i + 1; j < sourceLen; j++)
                    {
                        char c2 = aSource[j];
                        if (Char.IsWhiteSpace(c2))
                        {
                            if (started && !(singleQuoted || doubleQuoted))
                            {
                                attrEnd = j - 1;
                                valueEnd = attrEnd;
                                break;
                            }
                        }
                        else if (c2 == '>')
                        {
                            if(!(singleQuoted || doubleQuoted)) 
                            {
                                attrEnd = j - 1;
                                valueEnd = attrEnd;
                                break;
                            }
                        }
                        else if (c2 == '"')
                        {
                            if (started)
                            {
                                if (!singleQuoted)
                                {
                                    attrEnd = j;
                                    valueEnd = j - 1;
                                    break;
                                }
                            }
                            else
                            {
                                started = true;
                                doubleQuoted = true;
                                valueStart = j + 1;
                            }
                        }
                        else if (c2 == '\'')
                        {
                            if (started)
                            {
                                if (!doubleQuoted)
                                {
                                    attrEnd = j;
                                    valueEnd = j - 1;
                                    break;
                                }
                            }
                            else
                            {
                                started = true;
                                singleQuoted = true;
                                valueStart = j + 1;
                            }
                        }
                        else if (!started)
                        {
                            started = true;
                            valueStart = j;
                        }
                    }

                    if (attrEnd != -1)
                    {
                        string attrName = sb.ToString();
                        sb.Length = 0;

                        if (_attributes == null)
                            _attributes = new Dictionary<string, string>();

                        _attributes[attrName] = DecodeString(aSource.Substring(valueStart, valueEnd - valueStart + 1));
                        i = attrEnd + 1;
                        sb.Length = 0;
                    }
                    else
                        break;
                }
                else if (!Char.IsWhiteSpace(c))
                {
                    if (waitValue || c == '/' || c == '>')
                    {
                        if (sb.Length > 0)
                        {
                            string attrName = sb.ToString();
                            sb.Length = 0;

                            if (_attributes == null)
                                _attributes = new Dictionary<string, string>();

                            _attributes[attrName] = string.Empty;
                        }

                        waitValue = false;
                    }

                    if (sb.Length == 0)
                    {
                        if (c != '/')
                            attrStart = i;
                        else
                            continue;
                    }

                    sb.Append(c);
                }
                else
                {
                    if (sb.Length > 0)
                        waitValue = true;
                }
            }
        }

        static public string DecodeString(string aSource)
        {
            int len = aSource.Length;
            StringBuilder sb = new StringBuilder();
            int pos1 = 0, pos2 = 0;

            while (true)
            {
                pos2 = aSource.IndexOf('&', pos1);
                if (pos2 == -1)
                {
                    sb.Append(aSource.Substring(pos1));
                    break;
                }
                sb.Append(aSource.Substring(pos1, pos2 - pos1));

                pos1 = pos2 + 1;
                pos2 = pos1;
                int end = Math.Min(len, pos2 + 10);
                for (; pos2 < end; pos2++)
                {
                    if (aSource[pos2] == ';')
                        break;
                }
                if (pos2 < end && pos2 > pos1)
                {
                    string entity = aSource.Substring(pos1, pos2 - pos1);
                    int u = 0;
                    if (entity[0] == '#')
                    {
                        if (entity.Length > 1)
                        {
                            if (entity[1] == 'x')
                                u = Convert.ToInt16(entity.Substring(2), 16);
                            else
                                u = Convert.ToInt16(entity.Substring(1));
                            sb.Append((char)u);
                            pos1 = pos2 + 1;
                        }
                        else
                            sb.Append('&');
                    }
                    else
                    {
                        switch (entity)
                        {
                            case "amp":
                                u = 38;
                                break;

                            case "apos":
                                u = 39;
                                break;

                            case "gt":
                                u = 62;
                                break;

                            case "lt":
                                u = 60;
                                break;

                            case "nbsp":
                                u = 32;
                                break;

                            case "quot":
                                u = 34;
                                break;
                        }
                        if (u > 0)
                        {
                            sb.Append((char)u);
                            pos1 = pos2 + 1;
                        }
                        else
                            sb.Append('&');
                    }
                }
                else
                {
                    sb.Append('&');
                }
            }

            return sb.ToString();
        }

        public static string EncodeString(string str)
        {
            if (str == null || str.Length == 0)
                return "";
            else
                return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;");
        }
    }
}