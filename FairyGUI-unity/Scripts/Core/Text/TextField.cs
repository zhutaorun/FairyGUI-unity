using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    public class TextField : DisplayObject
    {
        public EventListener onFocusIn { get; private set; }
        public EventListener onFocusOut { get; private set; }
        public EventListener onChanged { get; private set; }

        AlignType _align;
        TextFormat _textFormat;
        bool _input;
        string _text;
        bool _autoSize;
        bool _wordWrap;
        bool _displayAsPassword;
        bool _singleLine;
        int _maxLength;
        bool _html;
        int _caretPosition;
        CharPosition? _selectionStart;
        InputCaret _caret;
        Highlighter _highlighter;
        bool _stroke;
        Color _strokeColor;
        float _fontScale;

        List<HtmlElement> _elements;
        List<LineInfo> _lines;
        internal Container objectContainer;

        IMobileInputAdapter _mobileInputAdapter;

        BaseFont _font;
        float _textWidth;
        float _textHeight;
        bool _meshChanged;
        bool _textChanged;
        bool _rectchanged;

        const int GUTTER_X = 2;
        const int GUTTER_Y = 2;
        const char E_TAG = (char)1;
        static float[] STROKE_OFFSET = new float[]
        {
            -1f, 0f, 1f, 0f,
            0f, -1f, 0f, 1f
        };
        static float[] BOLD_OFFSET = new float[]
        {
            -0.5f, 0f, 0.5f, 0f,
            0f, -0.5f, 0f, 0.5f
        };

        public TextField()
        {
            _textFormat = new TextFormat();
            _textFormat.size = 12;
            _textFormat.lineSpacing = 3;
            _fontScale = 1;
            _strokeColor = new Color(0, 0, 0, 1);

            _wordWrap = true;
            _displayAsPassword = false;
            optimizeNotTouchable = true;
            _maxLength = int.MaxValue;
            _text = string.Empty;

            _elements = new List<HtmlElement>();
            _lines = new List<LineInfo>();

            quadBatch = new QuadBatch(gameObject);

            onFocusIn = new EventListener(this, "onFocusIn");
            onFocusOut = new EventListener(this, "onFocusOut");
            onChanged = new EventListener(this, "onChanged");
        }

        public TextFormat textFormat
        {
            get { return _textFormat; }
            set
            {
                _textFormat = value;

                string fontName = _textFormat.font;
                if (_font == null || _font.name != fontName)
                {
                    _font = FontManager.GetFont(fontName);
                    if (_font != null)
                    {
                        quadBatch.texture = _font.mainTexture;
                        if (quadBatch.shader.IndexOf(ShaderConfig.grayedShaderSuffix) != -1)
                            quadBatch.shader = ShaderConfig.GetGrayedVersion(_font.shader, true);
                        else
                            quadBatch.shader = _font.shader;

                        _fontScale = _font.scale;
                    }
                }
                if (!string.IsNullOrEmpty(_text))
                    _textChanged = true;
            }
        }

        internal BaseFont font
        {
            get { return _font; }
        }

        public AlignType align
        {
            get { return _align; }
            set
            {
                if (_align != value)
                {
                    _align = value;
                    if (!string.IsNullOrEmpty(_text))
                        _textChanged = true;
                }
            }
        }

        public bool input
        {
            get { return _input; }
            set
            {
                if (_input != value)
                {
                    _input = value;
                    optimizeNotTouchable = !_input;

                    if (_input)
                    {
                        onFocusIn.AddCapture(__focusIn);
                        onFocusOut.AddCapture(__focusOut);

                        if (Stage.touchScreen && _mobileInputAdapter == null)
                            _mobileInputAdapter = ToolSet.CreateMobileInputAdapter();
                    }
                    else
                    {
                        onFocusIn.RemoveCapture(__focusIn);
                        onFocusOut.RemoveCapture(__focusOut);

                        if (_mobileInputAdapter != null)
                        {
                            _mobileInputAdapter.CloseKeyboard();
                            _mobileInputAdapter = null;
                        }
                    }
                }
            }
        }
        public string text
        {
            get { return _text; }
            set
            {
                _text = value;
                _textChanged = true;
                _html = false;
                if (_caretPosition > _text.Length)
                    _caretPosition = _text.Length;
            }
        }
        public string htmlText
        {
            get { return _text; }
            set
            {
                _text = value;
                _textChanged = true;
                _html = true;
                if (_caretPosition > _text.Length)
                    _caretPosition = _text.Length;

            }
        }
        public bool autoSize
        {
            get { return _autoSize; }
            set
            {
                if (_autoSize != value)
                {
                    _autoSize = value;
                    _textChanged = true;
                }
            }
        }
        public bool wordWrap
        {
            get { return _wordWrap; }
            set { _wordWrap = value; _textChanged = true; }
        }
        public bool singleLine
        {
            get { return _singleLine; }
            set { _singleLine = value; _textChanged = true; }
        }
        public bool displayAsPassword
        {
            get { return _displayAsPassword; }
            set { _displayAsPassword = value; }
        }
        public int maxLength
        {
            get { return _maxLength; }
            set { _maxLength = value; }
        }
        public bool stroke
        {
            get
            {
                return _stroke;
            }
            set
            {
                if (_stroke != value)
                {
                    _stroke = value;
                    _meshChanged = true;
                }
            }
        }

        public Color strokeColor
        {
            get
            {
                return _strokeColor;
            }
            set
            {
                if (_strokeColor != value)
                {
                    _strokeColor = value;
                    _meshChanged = true;
                }
            }
        }

        public float textWidth
        {
            get
            {
                if (_textChanged)
                    Wrap();

                return _textWidth;
            }
        }
        public float textHeight
        {
            get
            {
                if (_textChanged)
                    Wrap();

                return _textHeight;
            }
        }
        public override float width
        {
            get
            {
                if (_textChanged && _autoSize)
                    Wrap();
                return contentRect.width;
            }
            set
            {
                if (contentRect.width != value)
                {
                    _rectchanged = true;
                    if (_wordWrap)
                        _textChanged = true;
                    contentRect.width = value;
                }
            }
        }

        public override float height
        {
            get
            {
                if (_textChanged && _autoSize)
                    Wrap();
                return contentRect.height;
            }
            set
            {
                if (contentRect.height != value)
                {
                    _rectchanged = true;
                    contentRect.height = value;
                }
            }
        }

        public int caretPosition
        {
            get { return _caretPosition; }
            set
            {
                _caretPosition = value;
                if (_caretPosition > _text.Length)
                    _caretPosition = _text.Length;

                if (_caret != null)
                {
                    _selectionStart = null;
                    AdjustCaret(GetCharPosition(_caretPosition));
                }
            }
        }

        public void ReplaceSelection(string value)
        {
            if (!_input)
                return;

            InsertText(value);
        }

        public override void Update(UpdateContext context, float parentAlpha)
        {
            if (_mobileInputAdapter != null)
            {
                string s = _mobileInputAdapter.GetInput();

                if (s != null && s != _text)
                    this.text = s;
            }

            if (_caret != null)
            {
                string s = Input.inputString;
                if (!string.IsNullOrEmpty(s))
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < s.Length; ++i)
                    {
                        char ch = s[i];
                        if (ch >= ' ') sb.Append(ch.ToString());
                    }
                    if (sb.Length > 0)
                        InsertText(sb.ToString());
                }
            }

            if (_font != null)
            {
                if (_font.mainTexture != quadBatch.texture)
                {
                    if (!_textChanged)
                        RequestText();
                    quadBatch.texture = _font.mainTexture;
                    _meshChanged = true;
                }

                if (_textChanged)
                    Wrap();

                if (_meshChanged)
                    RebuildMesh();

                if (_rectchanged)
                    ApplyClip();
            }

            quadBatch.Update(context, parentAlpha * alpha);
        }

        internal void OnFontRebuild()
        {
            RequestText();
            quadBatch.texture = _font.mainTexture;
            RebuildMesh();
        }

        public override void Dispose()
        {
            base.Dispose();
            Cleanup();
            quadBatch.Dispose();
        }

        //准备字体纹理
        void RequestText()
        {
            int count = _elements.Count;
            for (int i = 0; i < count; i++)
            {
                HtmlElement_Text element = _elements[i] as HtmlElement_Text;
                if (element != null)
                {
                    _font.size = element.format.size;
                    _font.SetFontStyle(element.format);
                    _font.PrepareCharacters(element.text);
                    _font.PrepareCharacters("_-*");
                }
            }
        }

        FontStyle GetFontStyle(TextFormat format)
        {
            if (format.bold)
            {
                if (format.italic)
                    return FontStyle.BoldAndItalic;
                else
                    return FontStyle.Bold;
            }
            else
            {
                if (format.italic)
                    return FontStyle.Italic;
                else
                    return FontStyle.Normal;
            }
        }

        void Wrap()
        {
            _textChanged = false;
            _meshChanged = true;

            Cleanup();

            if (_caret != null)
                _caret.SetSizeAndColor(Mathf.FloorToInt((float)_textFormat.size * _fontScale), _textFormat.color);

            if (_text.Length == 0)
            {
                SetEmpty();
                return;
            }

            int letterSpacing = Mathf.FloorToInt(_textFormat.letterSpacing * _fontScale);
            int lineSpacing = Mathf.FloorToInt(_textFormat.lineSpacing * _fontScale - 1);
            int rectWidth = Mathf.RoundToInt(contentRect.width) - GUTTER_X * 2;
            int lineWidth = 0, lineHeight = 0, lineTextHeight = 0;
            int glyphWidth = 0, glyphHeight = 0;
            int wordChars = 0, wordStart = 0, wordEnd = 0;
            int lastLineHeight = 0;
            StringBuilder lineBuffer = new StringBuilder();
            TextFormat format = _textFormat;
            _font.size = format.size;
            _font.SetFontStyle(format);
            bool wrap;
            if (_input)
            {
                letterSpacing++;
                wrap = !_singleLine;
            }
            else
                wrap = _wordWrap && !_singleLine;
            int lineY = GUTTER_Y;
            LineInfo line;

            if (_displayAsPassword)
            {
                StringBuilder tmp = new StringBuilder(_text.Length);
                int textLen = _text.Length;
                for (int i = 0; i < textLen; i++)
                {
                    char c = _text[i];
                    tmp.Append("*");
                }
                HtmlParser.Parse(tmp.ToString(), _textFormat, _elements);
            }
            else if (_html && !_input)
                HtmlParser.Parse(_text, _textFormat, _elements);
            else
                HtmlParser.CreateSingleTextNode(_text, _textFormat, _elements);

            int count = _elements.Count;
            if (count == 0)
            {
                SetEmpty();
                return;
            }
            RequestText();

            for (int i = 0; i < count; i++)
            {
                HtmlElement element = _elements[i];

                if (!_input) //输入文本不支持html
                {
                    //写入特殊的tag，表示这里是一个Element开始
                    lineBuffer.Append(E_TAG);
                    lineBuffer.Append((char)(i + 33));
                    if (wordChars > 0)
                        wordEnd = lineWidth;
                    wordChars = 0;
                }

                if (element is HtmlElement_Text)
                {
                    string elementText = ((HtmlElement_Text)element).text;

                    if (!_input)
                    {
                        format = ((HtmlElement_Text)element).format;
                        _font.size = format.size;
                        _font.SetFontStyle(format);
                    }

                    int textLength = elementText.Length;
                    for (int offset = 0; offset < textLength; ++offset)
                    {
                        char ch = elementText[offset];
                        if (ch == E_TAG)
                            ch = '?';

                        if (ch == '\n')
                        {
                            lineBuffer.Append(ch);
                            line = LineInfo.Borrow();
                            line.width = lineWidth;
                            if (lineTextHeight == 0)
                            {
                                if (lastLineHeight == 0)
                                    lastLineHeight = Mathf.FloorToInt(format.size * _fontScale);
                                if (lineHeight == 0)
                                    lineHeight = lastLineHeight;
                                lineTextHeight = lineHeight;
                            }
                            line.height = lineHeight;
                            lastLineHeight = lineHeight;
                            line.textHeight = lineTextHeight;
                            line.text = lineBuffer.ToString();
                            line.y = lineY;
                            lineY += (line.height + lineSpacing);
                            if (line.width > _textWidth)
                                _textWidth = line.width;
                            _lines.Add(line);
                            lineBuffer.Length = 0;
                            lineWidth = 0;
                            lineHeight = 0;
                            lineTextHeight = 0;
                            wordChars = 0;
                            wordStart = 0;
                            wordEnd = 0;
                            continue;
                        }

                        if (ch > 256 || ch <= ' ')
                        {
                            if (wordChars > 0)
                                wordEnd = lineWidth;
                            wordChars = 0;
                        }
                        else
                        {
                            if (wordChars == 0)
                                wordStart = lineWidth;
                            wordChars++;
                        }

                        if (_font.GetGlyphSize(ch, out glyphWidth, out glyphHeight))
                        {
                            if (glyphHeight > lineTextHeight)
                                lineTextHeight = glyphHeight;

                            if (glyphHeight > lineHeight)
                                lineHeight = glyphHeight;

                            if (lineWidth != 0)
                                lineWidth += letterSpacing;
                            lineWidth += glyphWidth;
                        }

                        if (!wrap || lineWidth <= rectWidth)
                        {
                            lineBuffer.Append(ch);
                        }
                        else
                        {
                            line = LineInfo.Borrow();
                            line.height = lineHeight;
                            line.textHeight = lineTextHeight;

                            if (lineBuffer.Length == 0) //the line cannt fit even a char
                            {
                                line.text = ch.ToString();
                            }
                            else if (wordChars > 0 && wordEnd > 0) //if word had broken, move it to new line
                            {
                                lineBuffer.Append(ch);
                                int len = lineBuffer.Length - wordChars;
                                line.text = lineBuffer.ToString(0, len);
                                if (!_input)
                                    line.text = line.text.TrimEnd();
                                line.width = wordEnd;
                                lineBuffer.Remove(0, len);

                                lineWidth -= wordStart;
                            }
                            else
                            {
                                line.text = lineBuffer.ToString();
                                line.width = lineWidth - (glyphWidth + letterSpacing);
                                lineBuffer.Length = 0;

                                lineBuffer.Append(ch);
                                lineWidth = glyphWidth;
                                lineHeight = glyphHeight;
                                lineTextHeight = glyphHeight;
                            }
                            line.y = lineY;
                            lineY += (line.height + lineSpacing);
                            if (line.width > _textWidth)
                                _textWidth = line.width;

                            wordChars = 0;
                            wordStart = 0;
                            wordEnd = 0;
                            _lines.Add(line);
                        }
                    }
                }
                else if (element is HtmlElement_Img)
                {
                    HtmlElement_Img img = (HtmlElement_Img)element;
                    int imageWidth = img.width;
                    int imageHeight = img.height;
                    img.obj = RichTextField.objectFactory.CreateObject(img.src, ref imageWidth, ref imageHeight);
                    img.realWidth = Mathf.FloorToInt((float)imageWidth * _font.scale);
                    img.realHeight = Mathf.FloorToInt((float)imageHeight * _font.scale);
                    glyphWidth = img.realWidth + 2;
                    glyphHeight = img.realHeight;

                    if (glyphHeight > lineHeight)
                        lineHeight = glyphHeight;

                    if (lineWidth != 0)
                        lineWidth += letterSpacing;
                    lineWidth += glyphWidth;

                    if (wrap && lineWidth > rectWidth && glyphWidth < rectWidth)
                    {
                        line = LineInfo.Borrow();
                        line.height = lineHeight;
                        line.textHeight = lineTextHeight;
                        int len = lineBuffer.Length - 2;
                        line.text = lineBuffer.ToString(0, len - 2);
                        line.width = lineWidth - (glyphWidth + letterSpacing);
                        lineBuffer.Remove(0, len - 2);
                        lineWidth = glyphWidth;
                        line.y = lineY;
                        lineY += (line.height + lineSpacing);
                        if (line.width > _textWidth)
                            _textWidth = line.width;

                        lineTextHeight = 0;
                        lineHeight = glyphHeight;
                        wordChars = 0;
                        wordStart = 0;
                        wordEnd = 0;
                        _lines.Add(line);
                    }
                }
            }

            if (lineBuffer.Length > 0 || _lines.Count > 0 && _lines[_lines.Count - 1].text.EndsWith("\n"))
            {
                line = LineInfo.Borrow();
                line.width = lineWidth;
                if (lineHeight == 0)
                    lineHeight = lastLineHeight;
                if (lineTextHeight == 0)
                    lineTextHeight = lineHeight;
                line.height = lineHeight;
                line.textHeight = lineTextHeight;
                line.text = lineBuffer.ToString();
                line.y = lineY;
                if (line.width > _textWidth)
                    _textWidth = line.width;
                _lines.Add(line);
            }

            if (_textWidth > 0)
                _textWidth += GUTTER_X * 2;

            count = _lines.Count;
            line = _lines[_lines.Count - 1];
            _textHeight = line.y + line.height + GUTTER_Y;
            if (_autoSize)
            {
                contentRect.width = _textWidth;
                contentRect.height = _textHeight;
            }
        }

        void SetEmpty()
        {
            LineInfo emptyLine = LineInfo.Borrow();
            emptyLine.width = 0;
            emptyLine.height = 0;
            emptyLine.text = string.Empty;
            emptyLine.y = GUTTER_Y;
            _lines.Add(emptyLine);

            _textWidth = 0;
            _textHeight = 0;
            if (_autoSize)
            {
                contentRect.width = _textWidth;
                contentRect.height = _textHeight;
            }
        }

        void RebuildMesh()
        {
            _meshChanged = false;
            _rectchanged = true;

            if (_textWidth == 0)
            {
                quadBatch.Clear();
                if (_caret != null)
                {
                    _caretPosition = 0;
                    CharPosition cp = GetCharPosition(_caretPosition);
                    AdjustCaret(cp);
                }
                return;
            }

            int letterSpacing = Mathf.FloorToInt(_textFormat.letterSpacing * _fontScale);
            int lineSpacing = Mathf.FloorToInt(_textFormat.lineSpacing * _fontScale - 1);
            int rectWidth = Mathf.RoundToInt(contentRect.width) - GUTTER_X * 2;
            TextFormat format = _textFormat;
            _font.size = format.size;
            _font.SetFontStyle(format);
            Color32 color = format.color;
            Color32 white = Color.white;
            bool customBold = _font.customBold;
            if (_input)
            {
                letterSpacing++;
                customBold = false;
            }

            Vector3 v0 = Vector3.zero, v1 = Vector3.zero;
            Vector2 u0 = Vector2.zero, u1 = Vector2.zero;

            List<Vector3> vertList = QuadBatch.sCachedVerts;
            List<Vector2> uvList = QuadBatch.sCachedUVs;
            List<Color32> colList = QuadBatch.sCachedCols;

            HtmlElement_A currentLink = null;

            float charX;
            float tmpX;
            int lineIndent;
            int charIndent = 0;
            bool hasImage = false;

            int lineCount = _lines.Count;
            for (int i = 0; i < lineCount; ++i)
            {
                LineInfo line = _lines[i];

                if (_align == AlignType.Center)
                    lineIndent = (rectWidth - line.width) / 2;
                else if (_align == AlignType.Right)
                    lineIndent = rectWidth - line.width;
                else
                    lineIndent = 0;

                charX = GUTTER_X + lineIndent;

                int textLength = line.text.Length;
                for (int j = 0; j < textLength; j++)
                {
                    char ch = line.text[j];
                    if (ch == E_TAG)
                    {
                        int elementIndex = (int)line.text[++j] - 33;
                        HtmlElement element = _elements[elementIndex];
                        if (element is HtmlElement_Text)
                        {
                            format = ((HtmlElement_Text)element).format;
                            _font.size = format.size;
                            _font.SetFontStyle(format);
                            color = format.color;
                        }
                        else if (element is HtmlElement_A)
                        {
                            if (!((HtmlElement_A)element).end)
                            {
                                currentLink = (HtmlElement_A)element;
                                currentLink.quadStart = vertList.Count / 4;
                            }
                            else if (currentLink != null)
                            {
                                currentLink.quadEnd = vertList.Count / 4;
                                currentLink = null;
                            }
                        }
                        else if (element is HtmlElement_Img)
                        {
                            DisplayObject obj = ((HtmlElement_Img)element).obj;
                            if (obj != null)
                            {
                                obj.x = charX + 1;
                                obj.y = line.y + (line.height - ((HtmlElement_Img)element).realHeight) / 2;
                                hasImage = true;
                            }
                            charX += ((HtmlElement_Img)element).realWidth + letterSpacing + 2;
                        }
                        continue;
                    }

                    if (ch == ' ')
                    {
                        if (format.underline)
                            ch = '_';
                    }

                    GlyphInfo glyph = _font.GetGlyph(ch);
                    if (glyph != null)
                    {
                        tmpX = charX;
                        charIndent = (line.height + line.textHeight) / 2 - glyph.height;
                        v0.x = charX + glyph.vert.xMin;
                        v0.y = -line.y - charIndent + glyph.vert.yMin;
                        v1.x = charX + glyph.vert.xMax;
                        v1.y = -line.y - charIndent + glyph.vert.yMax;

                        u0.x = glyph.uv.xMin;
                        u0.y = glyph.uv.yMin;
                        u1.x = glyph.uv.xMax;
                        u1.y = glyph.uv.yMax;

                        if (_font.hasChannel)
                        {
                            //对于由BMFont生成的字体，使用这个特殊的设置告诉着色器告诉用的是哪个通道
                            u0.x = 10 * glyph.channel + u0.x;
                            u1.x = 10 * glyph.channel + u1.x;
                        }
                        else if (_font.canLight && format.bold)
                        {
                            //对于动态字体，使用这个特殊的设置告诉着色器这个文字不需要点亮（粗体亮度足够，不需要）
                            u0.x = 10 + u0.x;
                            u1.x = 10 + u1.x;
                        }

                        if (!format.bold || !customBold)
                        {
                            if (glyph.flipped)
                            {
                                uvList.Add(u0);
                                uvList.Add(new Vector2(u1.x, u0.y));
                                uvList.Add(u1);
                                uvList.Add(new Vector2(u0.x, u1.y));
                            }
                            else
                            {
                                uvList.Add(u0);
                                uvList.Add(new Vector2(u0.x, u1.y));
                                uvList.Add(u1);
                                uvList.Add(new Vector2(u1.x, u0.y));
                            }

                            vertList.Add(v0);
                            vertList.Add(new Vector3(v0.x, v1.y));
                            vertList.Add(new Vector3(v1.x, v1.y));
                            vertList.Add(new Vector3(v1.x, v0.y));
                            line.quadCount++;

                            if (_font.canTint)
                            {
                                colList.Add(color);
                                colList.Add(color);
                                colList.Add(color);
                                colList.Add(color);
                            }
                            else
                            {
                                colList.Add(white);
                                colList.Add(white);
                                colList.Add(white);
                                colList.Add(white);
                            }
                        }
                        else
                        {
                            for (int b = 0; b < 4; b++)
                            {
                                if (glyph.flipped)
                                {
                                    uvList.Add(u0);
                                    uvList.Add(new Vector2(u1.x, u0.y));
                                    uvList.Add(u1);
                                    uvList.Add(new Vector2(u0.x, u1.y));
                                }
                                else
                                {
                                    uvList.Add(u0);
                                    uvList.Add(new Vector2(u0.x, u1.y));
                                    uvList.Add(u1);
                                    uvList.Add(new Vector2(u1.x, u0.y));
                                }

                                float fx = BOLD_OFFSET[b * 2];
                                float fy = BOLD_OFFSET[b * 2 + 1];

                                vertList.Add(new Vector3(v0.x + fx, v0.y + fy));
                                vertList.Add(new Vector3(v0.x + fx, v1.y + fy));
                                vertList.Add(new Vector3(v1.x + fx, v1.y + fy));
                                vertList.Add(new Vector3(v1.x + fx, v0.y + fy));
                                line.quadCount++;

                                if (_font.canTint)
                                {
                                    colList.Add(color);
                                    colList.Add(color);
                                    colList.Add(color);
                                    colList.Add(color);
                                }
                                else
                                {
                                    colList.Add(white);
                                    colList.Add(white);
                                    colList.Add(white);
                                    colList.Add(white);
                                }
                            }
                        }

                        charX += letterSpacing + glyph.width;

                        if (format.underline || currentLink != null)
                        {
                            glyph = _font.GetGlyph('_');
                            if (glyph == null)
                                continue;

                            u0.x = glyph.uv.xMin;
                            u0.y = glyph.uv.yMin;
                            u1.x = glyph.uv.xMax;
                            u1.y = glyph.uv.yMax;

                            if (_font.hasChannel)
                            {
                                //对于由BMFont生成的字体，使用这个特殊的设置告诉着色器告诉用的是哪个通道
                                u0.x = 10 * glyph.channel + u0.x;
                                u1.x = 10 * glyph.channel + u1.x;
                            }
                            else if (_font.canLight && format.bold)
                            {
                                //对于动态字体，使用这个特殊的设置告诉着色器这个文字不需要点亮（粗体亮度足够，不需要）
                                u0.x = 10 + u0.x;
                                u1.x = 10 + u1.x;
                            }

                            float cx = (u0.x + u1.x) * 0.5f;
                            float cy = (u0.y + u1.y) * 0.5f;

                            uvList.Add(new Vector2(cx, cy));
                            uvList.Add(new Vector2(cx, cy));
                            uvList.Add(new Vector2(cx, cy));
                            uvList.Add(new Vector2(cx, cy));

                            v0.y = -line.y - charIndent + glyph.vert.yMin - 1;
                            v1.y = -line.y - charIndent + glyph.vert.yMax - 1;

                            vertList.Add(new Vector3(tmpX, v0.y));
                            vertList.Add(new Vector3(tmpX, v1.y));
                            vertList.Add(new Vector3(charX, v1.y));
                            vertList.Add(new Vector3(charX, v0.y));
                            line.quadCount++;

                            colList.Add(color);
                            colList.Add(color);
                            colList.Add(color);
                            colList.Add(color);
                        }
                    }
                    else //font.GetCharacterInfo
                    {
                        v0.x = charX;
                        v0.y = -line.y;
                        v1.x = v0.x;
                        v1.y = v0.y - 1;

                        u0.x = 0;
                        u0.y = 0;
                        u1.x = 0;
                        u1.y = 0;

                        uvList.Add(u0);
                        uvList.Add(new Vector2(u0.x, u1.y));
                        uvList.Add(u1);
                        uvList.Add(new Vector2(u1.x, u0.y));

                        vertList.Add(v0);
                        vertList.Add(new Vector3(v0.x, v1.y));
                        vertList.Add(v1);
                        vertList.Add(new Vector3(v1.x, v0.y));
                        line.quadCount++;

                        for (int k = 0; k < 4; ++k)
                            colList.Add(color);

                        charX += letterSpacing;
                    }
                }//text loop
            }//line loop

            if (!_input && _stroke && _font.canOutline)
            {
                int count = vertList.Count;
                Vector3[] vertBuf = new Vector3[count * 5];
                vertList.CopyTo(0, vertBuf, count * 4, count);
                Vector2[] uvBuf = new Vector2[count * 5];
                uvList.CopyTo(0, uvBuf, count * 4, count);
                Color32[] colBuf = new Color32[count * 5];
                colList.CopyTo(0, colBuf, count * 4, count);

                Color32 col = _strokeColor;
                int offset;
                for (int j = 0; j < 4; j++)
                {
                    offset = j * count;
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 vert = vertList[i];
                        Vector2 u = uvList[i];

                        //使用这个特殊的设置告诉着色器这个是描边
                        if (_font.canOutline)
                            u.y = 10 + u.y;
                        uvBuf[offset] = u;
                        vertBuf[offset] = new Vector3(vert.x + STROKE_OFFSET[j * 2], vert.y + STROKE_OFFSET[j * 2 + 1], 0);
                        colBuf[offset] = col;
                        offset++;
                    }
                }
                quadBatch.Fill(vertBuf, uvBuf, colBuf);
            }
            else
                quadBatch.Fill(vertList.ToArray(), uvList.ToArray(), colList.ToArray());

            vertList.Clear();
            uvList.Clear();
            colList.Clear();

            if (hasImage)  //因为这里处于Update，在Update里不允许修改显示列表，所以要延迟
            {
                Stage.inst.onPostUpdate.Add(__AddImages);
            }

            if (_caret != null)
            {
                if (_caretPosition > _text.Length)
                    _caretPosition = _text.Length;

                CharPosition cp = GetCharPosition(_caretPosition);
                AdjustCaret(cp);
            }
        }

        void __AddImages()
        {
            Stage.inst.onPostUpdate.Remove(__AddImages);

            int count = _elements.Count;
            for (int i = 0; i < count; i++)
            {
                HtmlElement_Img img = _elements[i] as HtmlElement_Img;
                if (img != null && img.obj != null)
                {
                    objectContainer.AddChild(img.obj);
                }
            }
        }

        void Cleanup()
        {
            if (objectContainer != null)
            {
                int count = _elements.Count;
                for (int i = 0; i < count; i++)
                {
                    HtmlElement element = _elements[i];
                    if (element is HtmlElement_Img)
                    {
                        DisplayObject obj = ((HtmlElement_Img)element).obj;
                        if (obj != null)
                        {
                            if (obj.parent != null)
                                objectContainer.RemoveChild(obj);
                            RichTextField.objectFactory.FreeObject(obj);
                        }
                    }
                }
            }

            HtmlParser.ClearList(_elements);
            LineInfo.Return(_lines);
            _lines.Clear();
            _textWidth = 0;
            _textHeight = 0;
        }

        CharPosition GetCharPosition(int charIndex)
        {
            CharPosition cp;
            cp.charIndex = charIndex;

            LineInfo line;
            int lineCount = _lines.Count;
            int i;
            int len;
            for (i = 0; i < lineCount; i++)
            {
                line = _lines[i];
                len = line.text.Length;
                if (charIndex - len < 0)
                    break;

                charIndex -= len;
            }
            if (i == lineCount)
                i = lineCount - 1;

            cp.lineIndex = i;
            return cp;
        }

        CharPosition GetCharPosition(Vector3 location)
        {
            CharPosition result;
            int lineCount = _lines.Count;
            int charIndex = 0;
            LineInfo line;
            int last = 0;
            int i;
            for (i = 0; i < lineCount; i++)
            {
                line = _lines[i];
                charIndex += last;

                if (line.y + line.height > location.y)
                    break;

                last = line.text.Length;
            }
            if (i == lineCount)
                i = lineCount - 1;

            result.lineIndex = i;
            line = _lines[i];
            int textLen = line.text.Length;
            Vector3 v;
            if (textLen > 0)
            {
                for (i = 0; i < textLen; i++)
                {
                    v = quadBatch.vertices[charIndex * 4 + 2];
                    if (v.x > location.x)
                        break;

                    charIndex++;
                }
                if (i == textLen && result.lineIndex != lineCount - 1)
                    charIndex--;
            }

            result.charIndex = charIndex;
            return result;
        }

        void ClearSelection()
        {
            if (_selectionStart != null)
            {
                _highlighter.Clear();
                _selectionStart = null;
            }
        }

        void DeleteSelection()
        {
            if (_selectionStart == null)
                return;

            CharPosition cp = (CharPosition)_selectionStart;
            if (cp.charIndex < _caretPosition)
            {
                this.text = _text.Substring(0, cp.charIndex) + _text.Substring(_caretPosition);
                _caretPosition = cp.charIndex;
            }
            else
                this.text = _text.Substring(0, _caretPosition) + _text.Substring(cp.charIndex);
            ClearSelection();
        }

        string GetSelection()
        {
            if (_selectionStart == null)
                return string.Empty;

            CharPosition cp = (CharPosition)_selectionStart;
            if (cp.charIndex < _caretPosition)
                return _text.Substring(cp.charIndex, _caretPosition - cp.charIndex);
            else
                return _text.Substring(_caretPosition, cp.charIndex - _caretPosition);
        }

        void InsertText(string value)
        {
            if (_selectionStart != null)
                DeleteSelection();
            this.text = _text.Substring(0, _caretPosition) + value + _text.Substring(_caretPosition);
            _caretPosition += value.Length;
            onChanged.Call();
        }

        void ApplyClip()
        {
            _rectchanged = false;
            if (_input)
            {
                //因为文字的顶点y值非常不可靠，所这里不使用y值来判断，而是改为用行
                Rect clipRect = new Rect(this.pivotX - 2, -10000000f, contentRect.width + 2, 20000000f);

                int cnt = _lines.Count;
                int quadIndex = 0;
                bool started = false;
                for (int i = 0; i < cnt; i++)
                {
                    LineInfo line = _lines[i];
                    if (line.y < this.pivotY
                        || line.y - this.pivotY > contentRect.height
                        || line.y + line.height - this.pivotY > contentRect.height && started)
                    {
                        quadBatch.SetQuadAlpha(quadIndex, line.quadCount, 0);
                        line.visible = false;
                    }
                    else
                    {
                        quadBatch.SetQuadAlpha(quadIndex, line.quadCount, clipRect, 1, 0);
                        line.visible = true;
                        started = true;
                    }
                    quadIndex += line.quadCount;
                }
            }
        }

        Vector2 GetCharLocation(CharPosition cp)
        {
            LineInfo line = _lines[cp.lineIndex];
            Vector2 pos;
            if (line.text.Length == 0)
                pos.x = GUTTER_X;
            else if (cp.charIndex == 0 || cp.charIndex < text.Length)
            {
                pos = quadBatch.vertices[cp.charIndex * 4];
                pos.x -= 1;
            }
            else
                pos = quadBatch.vertices[(cp.charIndex - 1) * 4 + 2];
            pos.y = line.y;
            return pos;
        }

        void AdjustCaret(CharPosition cp)
        {
            _caretPosition = cp.charIndex;
            Vector2 pos = GetCharLocation(cp);

            if (pos.x - this.pivotX < 5)
            {
                float move = pos.x - Math.Min(50, contentRect.width / 2);
                if (move < 0)
                    move = 0;
                else if (move + contentRect.width > _textWidth)
                    move = _textWidth - contentRect.width;
                if (this.pivotX != move)
                {
                    this.pivotX = move;
                    _rectchanged = true;
                }
            }
            else if (pos.x - this.pivotX > contentRect.width - 5)
            {
                float move = pos.x - Math.Min(50, contentRect.width / 2);
                if (move < 0)
                    move = 0;
                else if (move + contentRect.width > _textWidth)
                    move = _textWidth - contentRect.width;
                if (this.pivotX != move)
                {
                    this.pivotX = move;
                    _rectchanged = true;
                }
            }

            LineInfo line = _lines[cp.lineIndex];
            if (line.y - this.pivotY < 0)
            {
                float move = line.y - GUTTER_Y;
                if (this.pivotY != move)
                {
                    this.pivotY = move;
                    _rectchanged = true;
                }
            }
            else if (line.y + line.height - this.pivotY >= contentRect.height)
            {
                float move = line.y + line.height + GUTTER_Y - contentRect.height;
                if (move < 0)
                    move = 0;
                if (this.pivotY != move)
                {
                    this.pivotY = move;
                    _rectchanged = true;
                }
            }

            _caret.SetPosition(pos);

            if (_selectionStart != null)
            {
                ApplyClip();
                UpdateHighlighter(cp);
            }
        }

        void UpdateHighlighter(CharPosition cp)
        {
            CharPosition start = (CharPosition)_selectionStart;
            if (start.charIndex > cp.charIndex)
            {
                CharPosition tmp = start;
                start = cp;
                cp = tmp;
            }

            LineInfo line1;
            LineInfo line2;
            Vector2 v1, v2;
            line1 = _lines[start.lineIndex];
            line2 = _lines[cp.lineIndex];
            v1 = GetCharLocation(start);
            v2 = GetCharLocation(cp);

            _highlighter.BeginUpdate(start.lineIndex);
            if (start.lineIndex == cp.lineIndex)
            {
                if (line1.visible)
                {
                    Rect r = Rect.MinMaxRect(v1.x, line1.y, v2.x, line1.y + line1.height);
                    if (r.xMin < this.pivotX)
                        r.xMin = this.pivotX;
                    else if (r.xMax > this.pivotX + contentRect.width)
                        r.xMax = this.pivotX + contentRect.width;
                    _highlighter.AddRect(r);
                }
            }
            else
            {
                Rect r;

                if (line1.visible)
                {
                    r = Rect.MinMaxRect(v1.x, line1.y, contentRect.width - GUTTER_X * 2, line1.y + line1.height);
                    if (r.xMin < this.pivotX)
                        r.xMin = this.pivotX;
                    else if (r.xMax > this.pivotX + contentRect.width)
                        r.xMax = this.pivotX + contentRect.width;
                    _highlighter.AddRect(r);
                }

                for (int i = start.lineIndex + 1; i < cp.lineIndex; i++)
                {
                    LineInfo line = _lines[i];
                    if (line.visible)
                    {
                        r = Rect.MinMaxRect(GUTTER_X, line.y, contentRect.width - GUTTER_X * 2, line.y + line.height);
                        if (i == start.lineIndex)
                            r.yMin = line1.y + line1.height;
                        if (i == cp.lineIndex - 1)
                            r.yMax = line2.y;
                        if (r.xMin < this.pivotX)
                            r.xMin = this.pivotX;
                        else if (r.xMax > this.pivotX + contentRect.width)
                            r.xMax = this.pivotX + contentRect.width;

                        _highlighter.AddRect(r);
                    }
                }

                if (line2.visible)
                {
                    r = Rect.MinMaxRect(GUTTER_X, line2.y, v2.x, line2.y + line2.height);
                    if (r.xMin < this.pivotX)
                        r.xMin = this.pivotX;
                    else if (r.xMax > this.pivotX + contentRect.width)
                        r.xMax = this.pivotX + contentRect.width;
                    _highlighter.AddRect(r);
                }
            }
            _highlighter.EndUpdate();
        }

        internal HtmlElement_A GetLink(Vector2 pos)
        {
            if (_elements == null)
                return null;

            pos.x += this.pivotX;
            pos.y += this.pivotY;

            if (!contentRect.Contains(pos))
                return null;

            Vector3[] verts = quadBatch.vertices;
            int count = quadBatch.quadCount;
            pos.y = -pos.y;
            int i;
            for (i = 0; i < count; i++)
            {
                Vector3 vertBottomLeft = verts[i * 4];
                Vector3 vertTopRight = verts[i * 4 + 2];
                if (pos.y > vertBottomLeft.y && pos.y <= vertTopRight.y && vertTopRight.x > pos.x)
                    break;
            }
            if (i == count)
                return null;

            int quadIndex = i;
            count = _elements.Count;
            for (i = 0; i < count; i++)
            {
                HtmlElement_A element = _elements[i] as HtmlElement_A;
                if (element != null)
                {
                    if (quadIndex >= element.quadStart && quadIndex < element.quadEnd)
                        return element;
                }
            }

            return null;
        }

        void OpenKeyboard()
        {
            _mobileInputAdapter.OpenKeyboard(text, 0, false, displayAsPassword ? false : !_singleLine, displayAsPassword, false, null);
        }

        void __focusIn(EventContext context)
        {
            if (_input)
            {
                if (_mobileInputAdapter != null)
                {
                    OpenKeyboard();
                    onMouseDown.AddCapture(__mouseDown2);
                }
                else
                {
                    _caret = Stage.inst.inputCaret;
                    _caret.SetParent(cachedTransform);
                    _caret.SetSizeAndColor(Mathf.FloorToInt(_textFormat.size * _fontScale), _textFormat.color);

                    _highlighter = Stage.inst.highlighter;
                    _highlighter.SetParent(cachedTransform);

                    onKeyDown.AddCapture(__keydown);
                    onMouseDown.AddCapture(__mouseDown);
                }
            }
        }

        void __focusOut(EventContext contxt)
        {
            if (_mobileInputAdapter != null)
            {
                _mobileInputAdapter.CloseKeyboard();
                onMouseDown.RemoveCapture(__mouseDown2);
            }

            if (_caret != null)
            {
                _caret.SetParent(null);
                _caret = null;
                _highlighter.SetParent(null);
                _highlighter = null;
                onKeyDown.RemoveCapture(__keydown);
                onMouseDown.RemoveCapture(__mouseDown);
            }
        }

        void __keydown(EventContext context)
        {
            if (context.isDefaultPrevented)
                return;

            InputEvent evt = context.inputEvent;

            switch (evt.keyCode)
            {
                case KeyCode.Backspace:
                    {
                        context.PreventDefault();
                        if (_selectionStart != null)
                        {
                            DeleteSelection();
                            onChanged.Call();
                        }
                        else if (_caretPosition > 0)
                        {
                            int tmp = _caretPosition; //this.text 会修改_caretPosition
                            _caretPosition--;
                            this.text = _text.Substring(0, tmp - 1) + _text.Substring(tmp);
                            onChanged.Call();
                        }

                        break;
                    }

                case KeyCode.Delete:
                    {
                        context.PreventDefault();
                        if (_selectionStart != null)
                        {
                            DeleteSelection();
                            onChanged.Call();
                        }
                        else if (_caretPosition < _text.Length)
                        {
                            this.text = _text.Substring(0, _caretPosition) + _text.Substring(_caretPosition + 1);
                            onChanged.Call();
                        }

                        break;
                    }

                case KeyCode.LeftArrow:
                    {
                        context.PreventDefault();
                        if (evt.shift)
                        {
                            if (_selectionStart == null)
                                _selectionStart = GetCharPosition(_caretPosition);
                        }
                        else
                            ClearSelection();
                        if (_caretPosition > 0)
                        {
                            CharPosition cp = GetCharPosition(_caretPosition - 1);

                            AdjustCaret(cp);
                        }
                        break;
                    }

                case KeyCode.RightArrow:
                    {
                        context.PreventDefault();
                        if (evt.shift)
                        {
                            if (_selectionStart == null)
                                _selectionStart = GetCharPosition(_caretPosition);
                        }
                        else
                            ClearSelection();
                        if (_caretPosition < _text.Length)
                        {
                            CharPosition cp = GetCharPosition(_caretPosition + 1);
                            AdjustCaret(cp);
                        }
                        break;
                    }

                case KeyCode.UpArrow:
                    {
                        context.PreventDefault();
                        if (evt.shift)
                        {
                            if (_selectionStart == null)
                                _selectionStart = GetCharPosition(_caretPosition);
                        }
                        else
                            ClearSelection();

                        CharPosition cp = GetCharPosition(_caretPosition);
                        if (cp.lineIndex == 0)
                            return;

                        LineInfo line = _lines[cp.lineIndex - 1];
                        cp = GetCharPosition(new Vector3(_caret.cachedTransform.localPosition.x + this.pivotX, line.y, 0));
                        AdjustCaret(cp);
                        break;
                    }


                case KeyCode.DownArrow:
                    {
                        context.PreventDefault();
                        if (evt.shift)
                        {
                            if (_selectionStart == null)
                                _selectionStart = GetCharPosition(_caretPosition);
                        }
                        else
                            ClearSelection();

                        CharPosition cp = GetCharPosition(_caretPosition);
                        if (cp.lineIndex == _lines.Count - 1)
                            return;

                        LineInfo line = _lines[cp.lineIndex + 1];
                        cp = GetCharPosition(new Vector3(_caret.cachedTransform.localPosition.x + this.pivotX, line.y, 0));
                        AdjustCaret(cp);
                        break;
                    }

                case KeyCode.PageUp:
                    {
                        context.PreventDefault();
                        ClearSelection();

                        break;
                    }

                case KeyCode.PageDown:
                    {
                        context.PreventDefault();
                        ClearSelection();

                        break;
                    }

                case KeyCode.Home:
                    {
                        context.PreventDefault();
                        ClearSelection();

                        CharPosition cp = GetCharPosition(_caretPosition);
                        LineInfo line = _lines[cp.lineIndex];
                        cp = GetCharPosition(new Vector3(int.MinValue, line.y, 0));
                        AdjustCaret(cp);
                        break;
                    }

                case KeyCode.End:
                    {
                        context.PreventDefault();
                        ClearSelection();

                        CharPosition cp = GetCharPosition(_caretPosition);
                        LineInfo line = _lines[cp.lineIndex];
                        cp = GetCharPosition(new Vector3(int.MaxValue, line.y, 0));
                        AdjustCaret(cp);

                        break;
                    }

                //Select All
                case KeyCode.A:
                    {
                        if (evt.ctrl)
                        {
                            context.PreventDefault();
                            _selectionStart = GetCharPosition(0);
                            AdjustCaret(GetCharPosition(_text.Length));
                        }
                        break;
                    }

                // Copy
                case KeyCode.C:
                    {
                        if (evt.ctrl && !_displayAsPassword)
                        {
                            context.PreventDefault();
                            string s = GetSelection();
                            if (!string.IsNullOrEmpty(s))
                                ToolSet.clipboard = s;
                        }
                        break;
                    }

                // Paste
                case KeyCode.V:
                    {
                        if (evt.ctrl)
                        {
                            context.PreventDefault();
                            string s = ToolSet.clipboard;
                            if (!string.IsNullOrEmpty(s))
                                InsertText(s);
                        }
                        break;
                    }

                // Cut
                case KeyCode.X:
                    {
                        if (evt.ctrl && !_displayAsPassword)
                        {
                            context.PreventDefault();
                            string s = GetSelection();
                            if (!string.IsNullOrEmpty(s))
                            {
                                ToolSet.clipboard = s;
                                DeleteSelection();
                                onChanged.Call();
                            }
                        }
                        break;
                    }

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    {
                        if (!evt.ctrl && !evt.shift)
                        {
                            context.PreventDefault();

                            if (!_singleLine)
                                InsertText("\n");
                        }
                        break;
                    }
            }
        }

        void __mouseDown(EventContext context)
        {
            if (_lines.Count == 0)
                return;

            ClearSelection();

            Vector3 v = new Vector3(Stage.mouseX, Stage.mouseY, 0);
            v = this.GlobalToLocal(v);
            v.x += this.pivotX;
            v.y += this.pivotY;
            CharPosition cp = GetCharPosition(v);
            AdjustCaret(cp);

            _selectionStart = cp;
            Stage.inst.onMouseMove.AddCapture(__mouseMove);
            Stage.inst.onMouseUp.AddCapture(__mouseUp);
        }

        void __mouseDown2(EventContext context)
        {
            OpenKeyboard();
        }

        void __mouseMove(EventContext context)
        {
            if (_selectionStart == null)
                return;

            Vector3 v = new Vector3(Stage.mouseX, Stage.mouseY, 0);
            v = this.GlobalToLocal(v);
            v.x += this.pivotX;
            v.y += this.pivotY;
            CharPosition cp = GetCharPosition(v);
            if (cp.charIndex != _caretPosition)
                AdjustCaret(cp);
        }

        void __mouseUp(EventContext context)
        {
            if (_selectionStart != null && ((CharPosition)_selectionStart).charIndex == _caretPosition)
                _selectionStart = null;
            Stage.inst.onMouseMove.RemoveCapture(__mouseMove);
            Stage.inst.onMouseUp.RemoveCapture(__mouseUp);
        }

        class LineInfo
        {
            public int width;
            public int height;
            public int textHeight;
            public string text;
            public int y;
            public int quadCount;
            public bool visible;

            static Stack<LineInfo> pool = new Stack<LineInfo>();

            public static LineInfo Borrow()
            {
                if (pool.Count > 0)
                {
                    LineInfo ret = pool.Pop();
                    ret.width = 0;
                    ret.height = 0;
                    ret.textHeight = 0;
                    ret.text = null;
                    ret.y = 0;
                    ret.quadCount = 0;
                    ret.visible = false;
                    return ret;
                }
                else
                    return new LineInfo();
            }

            public static void Return(LineInfo value)
            {
                pool.Push(value);
            }

            public static void Return(List<LineInfo> values)
            {
                foreach (LineInfo value in values)
                    pool.Push(value);
            }
        }

        struct CharPosition
        {
            public int charIndex;
            public int lineIndex;
        }
    }

}
