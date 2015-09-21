using System;
using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    public class DynamicFont : BaseFont
    {
        protected Font _font;
        protected Dictionary<int, float> _cachedBaseline;

        float _lastBaseLine;
        int _lastFontSize;
        bool _callingValidate;
        FontStyle _style;

        static CharacterInfo sTempChar;
        static GlyphInfo glyhInfo = new GlyphInfo();

        public DynamicFont(string name)
        {
            this.name = name;
            this.canTint = true;
            this.canOutline = true;
            this.hasChannel = false;

            bool desktop = Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.WindowsWebPlayer ||
                Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.OSXWebPlayer ||
                Application.platform == RuntimePlatform.OSXEditor;
            if (UIConfig.renderingTextBrighterOnDesktop && desktop)
            {
                this.shader = ShaderConfig.textBrighterShader;
                this.canLight = true;
            }
            else
                this.shader = ShaderConfig.textShader;
            if (name.ToLower().IndexOf("bold") == -1)
                this.customBold = !desktop;
            _cachedBaseline = new Dictionary<int, float>();

            LoadFont();
        }

        virtual protected void LoadFont()
        {
            _font = (Font)Resources.Load(name, typeof(Font));
            if (_font == null)
                _font = (Font)Resources.Load("Fonts/" + name, typeof(Font));
            if (_font == null)
                _font = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            if (_font == null)
            {
                if (name != UIConfig.defaultFont)
                {
                    DynamicFont bf = FontManager.GetFont(UIConfig.defaultFont) as DynamicFont;
                    if (bf != null)
                        _font = bf._font;
                }
                if (_font == null)
                    throw new Exception("Cant load font '" + name + "'");
            }

            _font.textureRebuildCallback += textureRebuildCallback;
            this.mainTexture = new NTexture(_font.material.mainTexture);
        }

        override public void SetFontStyle(TextFormat format)
        {
            if (format.bold && !customBold)
            {
                if (format.italic)
                    _style = FontStyle.BoldAndItalic;
                else
                    _style = FontStyle.Bold;
            }
            else
            {
                if (format.italic)
                    _style = FontStyle.Italic;
                else
                    _style = FontStyle.Normal;
            }
        }

        override public void PrepareCharacters(string text)
        {
            int realSize;
            if (scale == 1)
                realSize = size;
            else
                realSize = Mathf.FloorToInt(size * scale) - 1;
            _font.RequestCharactersInTexture(text, realSize, _style);
        }

        override public bool GetGlyphSize(char ch, out int width, out int height)
        {
            int realSize;
            if (scale == 1)
                realSize = size;
            else
                realSize = Mathf.FloorToInt(size * scale) - 1;
            if (_font.GetCharacterInfo(ch, out sTempChar, realSize, _style))
            {
                width = Mathf.RoundToInt(sTempChar.width);
                height = Mathf.RoundToInt(sTempChar.size);
                if (customBold)
                    width++;
                return true;
            }
            else
            {
                width = 0;
                height = 0;
                return false;
            }
        }

        override public GlyphInfo GetGlyph(char ch)
        {
            int realSize;
            if (scale == 1)
                realSize = size;
            else
                realSize = Mathf.FloorToInt(size * scale) - 1;
            if (_font.GetCharacterInfo(ch, out sTempChar, realSize, _style))
            {
                float baseline;
                if (_lastFontSize == realSize)
                    baseline = _lastBaseLine;
                else
                {
                    _lastFontSize = realSize;
                    baseline = GetBaseLine(realSize);
                    _lastBaseLine = baseline;
                }
                glyhInfo.vert.xMin = sTempChar.vert.xMin;
                glyhInfo.vert.yMin = sTempChar.vert.yMax - baseline;
                glyhInfo.vert.xMax = sTempChar.vert.xMax;
                if (sTempChar.vert.width == 0) //zero width, space etc
                    glyhInfo.vert.xMax = glyhInfo.vert.xMin + realSize / 2;
                glyhInfo.vert.yMax = sTempChar.vert.yMin - baseline;
                glyhInfo.uv = sTempChar.uv;

                glyhInfo.width = Mathf.RoundToInt(sTempChar.width);
                glyhInfo.height = Mathf.RoundToInt(sTempChar.size);
                if (customBold)
                    glyhInfo.width++;
                glyhInfo.flipped = sTempChar.flipped;

                return glyhInfo;
            }
            else
                return null;
        }

        void textureRebuildCallback()
        {
            mainTexture = new NTexture(_font.material.mainTexture);

            if (!_callingValidate)
                Stage.inst.onPostUpdate.Add(ValidateTextFields);
            //Debug.Log("Font texture rebuild: " + name + "," + mainTexture.width + "," + mainTexture.height);
        }

        void ValidateTextFields()
        {
            Stage.inst.onPostUpdate.Remove(ValidateTextFields);
            _callingValidate = true;
            Stage.inst.OnFontRebuild(this);
            _callingValidate = false;
        }

        float GetBaseLine(int size)
        {
            float result;
            if (!_cachedBaseline.TryGetValue(size, out result))
            {
                CharacterInfo charInfo;
                _font.RequestCharactersInTexture("f|体_j", size, FontStyle.Normal);

                //find the most top position
                float y0 = float.MinValue;
                if (_font.GetCharacterInfo('f', out charInfo, size, FontStyle.Normal))
                    y0 = Mathf.Max(y0, charInfo.vert.yMin);
                if (_font.GetCharacterInfo('|', out charInfo, size, FontStyle.Normal))
                    y0 = Mathf.Max(y0, charInfo.vert.yMin);
                if (_font.GetCharacterInfo('体', out charInfo, size, FontStyle.Normal))
                    y0 = Mathf.Max(y0, charInfo.vert.yMin);

                //find the most bottom position
                float y1 = float.MaxValue;
                if (_font.GetCharacterInfo('_', out charInfo, size, FontStyle.Normal))
                    y1 = Mathf.Min(y1, charInfo.vert.yMax);
                if (_font.GetCharacterInfo('|', out charInfo, size, FontStyle.Normal))
                    y1 = Mathf.Min(y1, charInfo.vert.yMax);
                if (_font.GetCharacterInfo('j', out charInfo, size, FontStyle.Normal))
                    y1 = Mathf.Min(y1, charInfo.vert.yMax);

                result = y0 + (y0 - y1 - size) * 0.5f;
                _cachedBaseline.Add(size, result);
            }

            return result;
        }
    }
}
