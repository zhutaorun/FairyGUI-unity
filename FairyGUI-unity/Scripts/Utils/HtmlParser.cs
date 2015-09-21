using System;
using System.Collections.Generic;
using System.Text;

namespace FairyGUI.Utils
{
    public abstract class HtmlElement
    {
    }

    public class HtmlElement_Text : HtmlElement
    {
        public string text;
        public TextFormat format;

        public HtmlElement_Text()
        {
            format = new TextFormat();
        }
    }

    public class HtmlElement_A : HtmlElement
    {
        public string href;
        public string target;
        public bool end;

        public int quadStart;
        public int quadEnd;
    }

    public class HtmlElement_Img : HtmlElement
    {
        public string src;
        public int width;
        public int height;

        public int realWidth;
        public int realHeight;
        public DisplayObject obj;
    }

    public class HtmlParser
    {
        static List<TextFormat> sTextFormatStack = new List<TextFormat>();
        static int sTextFormatStackTop = 0;
        static TextFormat _format = new TextFormat();

        static Stack<HtmlElement_Text> sTextElementPool = new Stack<HtmlElement_Text>();
        static Stack<HtmlElement_A> sAElementPool = new Stack<HtmlElement_A>();
        static Stack<HtmlElement_Img> sImgElementPool = new Stack<HtmlElement_Img>();

        static string[] fontAttrNames = new string[] { "size", "color" };
        static string[] aAttrNames = new string[] { "href", "target" };
        static string[] imgAttrNames = new string[] { "src", "width", "height" };

        static List<HtmlElement> _elements;

        static public void Parse(string aSource, TextFormat defaultFormat, List<HtmlElement> elements)
        {
            _elements = elements;

            int pos = 0, pos2, length = 0, tagType = 0;
            string tagName = null;
            string tagSource;
            int skipText = 0;

            sTextFormatStackTop = 0;
            _format.CopyFrom(defaultFormat);

            while ((pos2 = EnumTag(aSource, pos, ref tagName, ref length, ref tagType)) != -1)
            {
                if (pos != pos2 && skipText == 0)
                    AppendText(aSource.Substring(pos, pos2 - pos), true);

                tagSource = aSource.Substring(pos2, length);
                switch (tagName)
                {
                    case "b":
                        if (tagType == 0)
                        {
                            PushTextFormat(_format);
                            _format.bold = true;
                        }
                        else
                            PopTextFormat(_format);
                        break;

                    case "i":
                        if (tagType == 0)
                        {
                            PushTextFormat(_format);
                            _format.italic = true;
                        }
                        else
                            PopTextFormat(_format);
                        break;

                    case "u":
                        if (tagType == 0)
                        {
                            PushTextFormat(_format);
                            _format.underline = true;
                        }
                        else
                            PopTextFormat(_format);
                        break;

                    case "sub":
                        break;

                    case "sup":
                        break;

                    case "font":
                        if (tagType == 0)
                        {
                            PushTextFormat(_format);
                            string[] values = GetAttributeValue(tagSource, fontAttrNames);
                            string size = values[0];
                            if (size != null)
                                _format.size = Convert.ToInt32(size);
                            string color = values[1];
                            if (color != null)
                                _format.color = ToolSet.ConvertFromHtmlColor(color);
                        }
                        else if (tagType == 1)
                            PopTextFormat(_format);
                        break;

                    case "br":
                        AppendText("\n", false);
                        break;

                    case "img":
                        if (tagType == 0 || tagType == 2)
                        {
                            string[] values = GetAttributeValue(tagSource, imgAttrNames);
                            HtmlElement_Img element;
                            if (sImgElementPool.Count > 0)
                                element = sImgElementPool.Pop();
                            else
                                element = new HtmlElement_Img();
                            element.src = values[0];
                            if (values[1] != null)
                                element.width = Convert.ToInt32(values[1]);
                            else
                                element.width = 0;
                            if (values[2] != null)
                                element.height = Convert.ToInt32(values[2]);
                            else
                                element.height = 0;
                            _elements.Add(element);
                        }
                        break;

                    case "a":
                        if (tagType == 0)
                        {
                            string[] values = GetAttributeValue(tagSource, aAttrNames);
                            HtmlElement_A element;
                            if (sAElementPool.Count > 0)
                                element = sAElementPool.Pop();
                            else
                                element = new HtmlElement_A();
                            element.href = values[0];
                            element.target = values[1];
                            element.end = false;
                            _elements.Add(element);
                        }
                        else if (tagType == 1)
                        {
                            HtmlElement_A element;
                            if (sAElementPool.Count > 0)
                                element = sAElementPool.Pop();
                            else
                                element = new HtmlElement_A();
                            element.end = true;
                            _elements.Add(element);
                        }
                        break;
                }

                pos = pos2 + length;
            }

            if (pos != aSource.Length)
                AppendText(aSource.Substring(pos, aSource.Length - pos), true);

            _elements = null;
        }

        static void PushTextFormat(TextFormat format)
        {
            TextFormat tf;
            if (sTextFormatStack.Count <= sTextFormatStackTop)
            {
                tf = new TextFormat();
                sTextFormatStack.Add(tf);
            }
            else
                tf = sTextFormatStack[sTextFormatStackTop];
            tf.CopyFrom(_format);
            sTextFormatStackTop++;
        }

        static void PopTextFormat(TextFormat assignTo)
        {
            if (sTextFormatStackTop > 0)
            {
                assignTo.CopyFrom(sTextFormatStack[sTextFormatStackTop - 1]);
                sTextFormatStackTop--;
            }
        }

        static public void CreateSingleTextNode(string aSource, TextFormat defaultFormat, List<HtmlElement> elements)
        {
            _format.CopyFrom(defaultFormat);
            _elements = elements;
            AppendText(aSource, false);
            _elements = null;
        }

        static public void ClearList(List<HtmlElement> elements)
        {
            int count = elements.Count;
            for (int i = 0; i < count; i++)
            {
                HtmlElement element = elements[i];
                if (element is HtmlElement_Text)
                    sTextElementPool.Push((HtmlElement_Text)element);
                else if (element is HtmlElement_A)
                    sAElementPool.Push((HtmlElement_A)element);
                else if (element is HtmlElement_Img)
                {
                    ((HtmlElement_Img)element).obj = null;
                    sImgElementPool.Push((HtmlElement_Img)element);
                }
            }
            elements.Clear();
        }

        static private void AppendText(string text, bool html)
        {
            if (html)
                text = XML.DecodeString(text);

            HtmlElement_Text element;
            if (_elements.Count > 0)
            {
                element = _elements[_elements.Count - 1] as HtmlElement_Text;
                if (element != null)
                {
                    if (element.format.EqualStyle(_format))
                    {
                        element.text += text;
                        return;
                    }
                }
            }

            if (sTextElementPool.Count > 0)
                element = sTextElementPool.Pop();
            else
                element = new HtmlElement_Text();
            element.text = text;
            element.format.CopyFrom(_format);
            _elements.Add(element);
        }

        //tag type:0 - start tag, 1 - end tag, 2 - empty content tag
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

                if (aSource[pos] == '/')
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
                else if (aTagType != 1)
                    aTagType = 0;
                aTagName = sb.ToString();
                return aOffset;
            }

            return -1;
        }

        static string[] GetAttributeValue(string aSource, string[] aAttrNames)
        {
            string attrName = null;
            int pos = 0;
            int attrLength = 0, valueStart = 0, valueLength = 0;
            string[] result = new string[aAttrNames.Length];
            while (true)
            {
                pos = EnumAttribute(aSource, pos, ref attrName, ref attrLength, ref valueStart, ref valueLength);
                if (pos == -1)
                    break;

                for (int i = 0; i < aAttrNames.Length; i++)
                {
                    if (aAttrNames[i].Equals(attrName, StringComparison.OrdinalIgnoreCase))
                    {
                        result[i] = aSource.Substring(valueStart, valueLength);
                        break;
                    }
                }

                pos += attrLength;
            }

            return result;
        }

        static int EnumAttribute(string aSource, int aOffset, ref string aAttrName, ref int aAttrLength, ref int aValueStart, ref int aValueLength)
        {
            int sourceLen = aSource.Length;
            int attrStart = -1, attrEnd = -1;
            bool waitValue = false;
            int i = aOffset;
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
                        aAttrName = sb.ToString();
                        aValueStart = valueStart;
                        aValueLength = valueEnd - valueStart + 1;
                        aAttrLength = attrEnd - attrStart + 1;
                        return attrStart;
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
                            aAttrName = sb.ToString();
                            aValueStart = attrStart + aAttrName.Length;
                            aValueLength = 0;
                            aAttrLength = i - attrStart;
                            return attrStart;
                        }

                        waitValue = false;
                        sb.Length = 0;
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

            return -1;
        }
    }
}
