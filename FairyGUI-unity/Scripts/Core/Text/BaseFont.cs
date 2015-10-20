using UnityEngine;

namespace FairyGUI
{
	public class GlyphInfo
	{
		public Rect vert;
		public Vector2 uvBottomLeft;
		public Vector2 uvBottomRight;
		public Vector2 uvTopLeft;
		public Vector2 uvTopRight;
		public int width;
		public int height;
		public int channel;//use by bitmap font
	}

	abstract public class BaseFont
	{
		public string name { get; protected set; }
		public NTexture mainTexture;
		public int size;
		public float scale;

		//字体特性
		public bool canTint; //动态字体和BMFont生成的字体可以着色
		public bool canLight; //动态字体需要点亮
		public bool canOutline; //是否支持描边
		public bool hasChannel; //BMFont生成的字体支持通道
		public bool customBold; //如果为true，则不使用字体内置的效果，用额外的绘制实现

		public string shader;

		abstract public void PrepareCharacters(string text);
		abstract public bool GetGlyphSize(char ch, out int width, out int height);
		abstract public GlyphInfo GetGlyph(char ch);
		abstract public void SetFontStyle(TextFormat format);

		public BaseFont()
		{
			this.scale = GRoot.contentScaleFactor;
		}
	}
}
