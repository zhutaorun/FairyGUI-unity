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
#if UNITY_5
			if (_font == null)
			{
				if (name.IndexOf(",") != -1)
				{
					string[] arr = name.Split(new char[] { ',' });
					int cnt = arr.Length;
					for (int i = 0; i < cnt; i++)
						arr[i] = arr[i].Trim();
					_font = Font.CreateDynamicFontFromOSFont(arr, 16);
				}
				else
					_font = Font.CreateDynamicFontFromOSFont(name, 16);
			}
#endif
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

#if UNITY_5
			Font.textureRebuilt += textureRebuildCallback;

#else
			_font.textureRebuildCallback += textureRebuildCallback;
#endif
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
#if UNITY_5
				width = Mathf.RoundToInt(sTempChar.advance);
#else
				width = Mathf.RoundToInt(sTempChar.width);
#endif
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
#if UNITY_5
				glyhInfo.vert.xMin = sTempChar.minX;
				glyhInfo.vert.yMin = sTempChar.minY - baseline;
				glyhInfo.vert.xMax = sTempChar.maxX;
				if (sTempChar.glyphWidth == 0) //zero width, space etc
					glyhInfo.vert.xMax = glyhInfo.vert.xMin + realSize / 2;
				glyhInfo.vert.yMax = sTempChar.maxY - baseline;
				glyhInfo.uvTopLeft = sTempChar.uvTopLeft;
				glyhInfo.uvBottomLeft = sTempChar.uvBottomLeft;
				glyhInfo.uvTopRight = sTempChar.uvTopRight;
				glyhInfo.uvBottomRight = sTempChar.uvBottomRight;

				glyhInfo.width = Mathf.RoundToInt(sTempChar.advance);
				glyhInfo.height = Mathf.RoundToInt(sTempChar.size);
				if (customBold)
					glyhInfo.width++;
#else
				glyhInfo.vert.xMin = sTempChar.vert.xMin;
				glyhInfo.vert.yMin = sTempChar.vert.yMax - baseline;
				glyhInfo.vert.xMax = sTempChar.vert.xMax;
				if (sTempChar.vert.width == 0) //zero width, space etc
					glyhInfo.vert.xMax = glyhInfo.vert.xMin + realSize / 2;
				glyhInfo.vert.yMax = sTempChar.vert.yMin - baseline;
				if (!sTempChar.flipped)
				{
					glyhInfo.uvBottomLeft = new Vector2(sTempChar.uv.xMin, sTempChar.uv.yMin);
					glyhInfo.uvTopLeft = new Vector2(sTempChar.uv.xMin, sTempChar.uv.yMax);
					glyhInfo.uvTopRight = new Vector2(sTempChar.uv.xMax, sTempChar.uv.yMax);
					glyhInfo.uvBottomRight = new Vector2(sTempChar.uv.xMax, sTempChar.uv.yMin);
				}
				else
				{
					glyhInfo.uvBottomLeft = new Vector2(sTempChar.uv.xMin, sTempChar.uv.yMin);
					glyhInfo.uvTopLeft = new Vector2(sTempChar.uv.xMax, sTempChar.uv.yMin);
					glyhInfo.uvTopRight = new Vector2(sTempChar.uv.xMax, sTempChar.uv.yMax);
					glyhInfo.uvBottomRight = new Vector2(sTempChar.uv.xMin, sTempChar.uv.yMax);
				}

				glyhInfo.width = Mathf.RoundToInt(sTempChar.width);
				glyhInfo.height = Mathf.RoundToInt(sTempChar.size);
				if (customBold)
					glyhInfo.width++;
#endif
				return glyhInfo;
			}
			else
				return null;
		}

#if UNITY_5
		void textureRebuildCallback(Font targetFont)
		{
			if (_font != targetFont)
				return;
			mainTexture = new NTexture(_font.material.mainTexture);

			if (!_callingValidate)
				Stage.inst.onPostUpdate.Add(ValidateTextFields);
			//Debug.Log("Font texture rebuild: " + name + "," + mainTexture.width + "," + mainTexture.height);
		}
#else
		void textureRebuildCallback()
		{
			mainTexture = new NTexture(_font.material.mainTexture);

			if (!_callingValidate)
				Stage.inst.onPostUpdate.Add(ValidateTextFields);
			//Debug.Log("Font texture rebuild: " + name + "," + mainTexture.width + "," + mainTexture.height);
		}
#endif

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
#if UNITY_5
				float y0 = float.MinValue;
				if (_font.GetCharacterInfo('f', out charInfo, size, FontStyle.Normal))
					y0 = Mathf.Max(y0, charInfo.maxY);
				if (_font.GetCharacterInfo('|', out charInfo, size, FontStyle.Normal))
					y0 = Mathf.Max(y0, charInfo.maxY);
				if (_font.GetCharacterInfo('体', out charInfo, size, FontStyle.Normal))
					y0 = Mathf.Max(y0, charInfo.maxY);

				//find the most bottom position
				float y1 = float.MaxValue;
				if (_font.GetCharacterInfo('_', out charInfo, size, FontStyle.Normal))
					y1 = Mathf.Min(y1, charInfo.minY);
				if (_font.GetCharacterInfo('|', out charInfo, size, FontStyle.Normal))
					y1 = Mathf.Min(y1, charInfo.minY);
				if (_font.GetCharacterInfo('j', out charInfo, size, FontStyle.Normal))
					y1 = Mathf.Min(y1, charInfo.minY);
#else
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
#endif

				result = y0 + (y0 - y1 - size) * 0.5f;
				_cachedBaseline.Add(size, result);
			}

			return result;
		}
	}
}
