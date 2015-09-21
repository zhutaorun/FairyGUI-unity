using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    public class BitmapFont : BaseFont
    {
        public class BMGlyph
        {
            public int x;
            public int y;
            public int offsetX;
            public int offsetY;
            public int width;
            public int height;
            public int advance;
            public int lineHeight;
            public Rect uvRect;
            public int channel;//0-n/a, 1-r,2-g,3-b,4-alpha
        }

        Dictionary<int, BMGlyph> _dict;
        public int lineHeight;

        static GlyphInfo glyhInfo = new GlyphInfo();

        public BitmapFont(string name)
        {
            this.name = name;
            this.canTint = true;
            this.canLight = false;
            this.canOutline = true;
            this.hasChannel = false;
            this.shader = ShaderConfig.bmFontShader;

            _dict = new Dictionary<int, BMGlyph>();
        }

        public void AddChar(char ch, BMGlyph glyph)
        {
            _dict[ch] = glyph;
        }

        override public void SetFontStyle(TextFormat format)
        {
        }

        override public void PrepareCharacters(string text)
        {
        }

        override public bool GetGlyphSize(char ch, out int width, out int height)
        {
            BMGlyph bg;
            if (ch == ' ')
            {
                width = Mathf.CeilToInt(size / 2 * scale);
                height = Mathf.CeilToInt(size * scale);
                return true;
            }
            else if (_dict.TryGetValue((int)ch, out bg))
            {
                width = Mathf.CeilToInt(bg.advance * scale);
                height = Mathf.CeilToInt(bg.lineHeight * scale);
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
            BMGlyph bg;
            if (ch == ' ')
            {
                glyhInfo.width = Mathf.CeilToInt(size / 2 * scale);
                glyhInfo.height = Mathf.CeilToInt(size * scale);
                glyhInfo.vert.xMin = 0;
                glyhInfo.vert.xMax = glyhInfo.width;
                glyhInfo.vert.yMin = glyhInfo.height;
                glyhInfo.vert.yMax = 0;
                glyhInfo.uv = new Rect(0, 0, 0, 0);
                glyhInfo.channel = 0;

                glyhInfo.flipped = false;
                return glyhInfo;
            }
            else if (_dict.TryGetValue((int)ch, out bg))
            {
                glyhInfo.width = Mathf.CeilToInt(bg.advance * scale);
                glyhInfo.height = Mathf.CeilToInt(bg.lineHeight * scale);
                glyhInfo.vert.xMin = bg.offsetX;
                glyhInfo.vert.xMax = bg.offsetX + bg.width * scale;
                glyhInfo.vert.yMin = -Mathf.CeilToInt(bg.height * scale);
                glyhInfo.vert.yMax = 0;
                glyhInfo.uv = bg.uvRect;
                glyhInfo.flipped = false;
                glyhInfo.channel = bg.channel;
                return glyhInfo;
            }
            else
                return null;
        }
    }
}
