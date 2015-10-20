using UnityEngine;
using FairyGUI.Utils;
using System;

namespace FairyGUI
{
	public enum FlipType
	{
		None,
		Horizontal,
		Vertical,
		Both
	}

	public class Image : DisplayObject
	{
		protected NTexture _texture;
		protected Color _color;
		protected FlipType _flip;
		protected Rect? _scale9Grid;
		protected bool _scaleByTile;
		protected bool _needRebuild;

		public Image()
		{
			Create(null);
		}

		public Image(NTexture texture)
			: base()
		{
			Create(texture);
		}

		public void Create(NTexture texture)
		{
			quadBatch = new QuadBatch(gameObject);
			quadBatch.shader = ShaderConfig.imageShader;

			_texture = texture;
			_color = Color.white;
			if (_texture != null)
			{
				contentRect.width = _texture.width;
				contentRect.height = _texture.height;
				UpdateTexture();
			}
			optimizeNotTouchable = true;
			scaleOverrided = true;
		}

		public NTexture texture
		{
			get { return _texture; }
			set
			{
				if (_texture != value)
				{
					_texture = value;
					UpdateTexture();
				}
			}
		}

		public Color color
		{
			get { return _color; }
			set
			{
				if (!_color.Equals(value))
				{
					_color = value;
					quadBatch.Tint(_color);
				}
			}
		}

		public FlipType flip
		{
			get { return _flip; }
			set
			{
				if (_flip != value)
				{
					_flip = value;
					_needRebuild = true;
				}
			}
		}

		public Rect? scale9Grid
		{
			get { return _scale9Grid; }
			set
			{
				if (_scale9Grid != value)
				{
					_scale9Grid = value;
					_needRebuild = true;
				}
			}
		}

		public bool scaleByTile
		{
			get { return _scaleByTile; }
			set
			{
				if (_scaleByTile != value)
				{
					_scaleByTile = value;
					_needRebuild = true;
				}
			}
		}

		public override void Update(UpdateContext context, float parentAlpha)
		{
			if (_needRebuild)
				Rebuild();

			quadBatch.Update(context, parentAlpha * alpha);
		}

		public override void Dispose()
		{
			base.Dispose();
			quadBatch.Dispose();
		}

		override protected void OverridedScale()
		{
			_needRebuild = true;
		}

		virtual protected void UpdateTexture()
		{
			if (_texture != null)
			{
				contentRect.width = _texture.width;
				contentRect.height = _texture.height;

				if (_texture.root.alphaTexture != null)
				{
					quadBatch.shader = ShaderConfig.GetGrayedVersion(ShaderConfig.combinedImageShader,
						ShaderConfig.IsGrayedVersion(quadBatch.shader));
				}
				else
				{
					quadBatch.shader = ShaderConfig.GetGrayedVersion(ShaderConfig.imageShader,
						ShaderConfig.IsGrayedVersion(quadBatch.shader));
				}
			}
			else
			{
				contentRect.width = 0;
				contentRect.height = 0;
			}
			_needRebuild = true;
			quadBatch.texture = _texture;
			InvalidateBatchingState();
		}

		virtual protected void Rebuild()
		{
			_needRebuild = false;
			if (_texture == null || scaleX == 0 || scaleY == 0)
			{
				quadBatch.enabled = false;
				return;
			}

			quadBatch.enabled = true;

			Rect uvRect = _texture.uvRect;
			if (_flip != FlipType.None)
				ToolSet.flipRect(ref uvRect, _flip);

			if (_scaleByTile)
			{
				float rx = this.scaleX / GRoot.contentScaleFactor;
				float ry = this.scaleY / GRoot.contentScaleFactor;

				int hc = Mathf.CeilToInt(rx);
				int vc = Mathf.CeilToInt(ry);
				float partWidth = contentRect.width * (rx - (hc - 1));
				float partHeight = contentRect.height * (ry - (vc - 1));
				Vector3[] verts = new Vector3[hc * vc * 4];
				Vector2[] uv = new Vector2[verts.Length];

				int k = 0;
				for (int i = 0; i < hc; i++)
				{
					for (int j = 0; j < vc; j++)
					{
						QuadBatch.FillVertsOfQuad(verts, k,
							new Rect(i * contentRect.width, j * contentRect.height,
								i == (hc - 1) ? partWidth : contentRect.width, j == (vc - 1) ? partHeight : contentRect.height),
								GRoot.contentScaleFactor, GRoot.contentScaleFactor);
						Rect uvTmp = uvRect;
						if (i == hc - 1)
							uvTmp.xMax = Mathf.Lerp(uvRect.xMin, uvRect.xMax, partWidth / contentRect.width);
						if (j == vc - 1)
							uvTmp.yMin = Mathf.Lerp(uvRect.yMin, uvRect.yMax, 1 - partHeight / contentRect.height);
						QuadBatch.FillUVOfQuad(uv, k, uvTmp);
						k += 4;
					}
				}

				quadBatch.Fill(verts, uv, _color);
			}
			else if (_scale9Grid == null || (this.scaleX == 1 && this.scaleY == 1))
			{
				quadBatch.Fill(contentRect, this.scaleX, this.scaleY, uvRect, _color);
			}
			else
			{
				float scale9Width = contentRect.width * this.scaleX / GRoot.contentScaleFactor;
				float scale9Height = contentRect.height * this.scaleY / GRoot.contentScaleFactor;

				float[] rows;
				float[] cols;
				float[] dRows;
				float[] dCols;
				Rect gridRect = (Rect)_scale9Grid;

				rows = new float[] { 0, gridRect.yMin, gridRect.yMax, contentRect.height };
				cols = new float[] { 0, gridRect.xMin, gridRect.xMax, contentRect.width };

				if (scale9Height >= (contentRect.height - gridRect.height))
					dRows = new float[] { 0, gridRect.yMin, scale9Height - (contentRect.height - gridRect.yMax), scale9Height };
				else
				{
					float tmp = gridRect.yMin / (contentRect.height - gridRect.yMax);
					tmp = scale9Height * tmp / (1 + tmp);
					dRows = new float[] { 0, tmp, tmp, scale9Height };
				}

				if (scale9Width >= (contentRect.width - gridRect.width))
					dCols = new float[] { 0, gridRect.xMin, scale9Width - (contentRect.width - gridRect.xMax), scale9Width };
				else
				{
					float tmp = gridRect.xMin / (contentRect.width - gridRect.xMax);
					tmp = scale9Width * tmp / (1 + tmp);
					dCols = new float[] { 0, tmp, tmp, scale9Width };
				}

				Vector3[] verts = new Vector3[16];
				Vector2[] uv = new Vector2[16];

				int k = 0;
				for (int cy = 0; cy < 4; cy++)
				{
					for (int cx = 0; cx < 4; cx++)
					{
						Vector2 subTextCoords;
						subTextCoords.x = uvRect.x + cols[cx] / contentRect.width * uvRect.width;
						subTextCoords.y = uvRect.y + (1 - rows[cy] / contentRect.height) * uvRect.height;
						uv[k] = subTextCoords;

						Vector3 drawCoords;
						drawCoords.x = dCols[cx] * GRoot.contentScaleFactor;
						drawCoords.y = -dRows[cy] * GRoot.contentScaleFactor;
						drawCoords.z = 0;
						verts[k] = drawCoords;

						k++;
					}
				}

				quadBatch.Fill(verts, uv, _color, QuickIndices.Grid9);
			}
		}

		public void PrintTo(Mesh mesh, Rect localRect)
		{
			if (_needRebuild)
				Rebuild();

			Rect uvRect = _texture.uvRect;
			if (_flip != FlipType.None)
				ToolSet.flipRect(ref uvRect, _flip);

			Vector3[] verts;
			Vector2[] uv;
			Color32[] colors;
			int[] triangles;
			int vertCount = 0;

			if (_scaleByTile)
			{
				verts = new Vector3[quadBatch.vertices.Length];
				uv = new Vector2[quadBatch.uv.Length];

				float rx = this.scaleX / GRoot.contentScaleFactor;
				float ry = this.scaleY / GRoot.contentScaleFactor;

				int hc = Mathf.CeilToInt(rx);
				int vc = Mathf.CeilToInt(ry);
				float partWidth = contentRect.width * (rx - (hc - 1));
				float partHeight = contentRect.height * (ry - (vc - 1));
				localRect.xMin *= rx;
				localRect.xMax *= rx;
				localRect.yMin *= ry;
				localRect.yMax *= ry;

				Vector2 offset = new Vector2(0, 0);
				for (int i = 0; i < hc; i++)
				{
					for (int j = 0; j < vc; j++)
					{
						Rect rect = new Rect(i * contentRect.width, j * contentRect.height,
								i == (hc - 1) ? partWidth : contentRect.width, j == (vc - 1) ? partHeight : contentRect.height);
						Rect uvTmp = uvRect;
						if (i == hc - 1)
							uvTmp.xMax = Mathf.Lerp(uvRect.xMin, uvRect.xMax, partWidth / contentRect.width);
						if (j == vc - 1)
							uvTmp.yMin = Mathf.Lerp(uvRect.yMin, uvRect.yMax, 1 - partHeight / contentRect.height);

						Rect bound = ToolSet.Intersection(ref rect, ref localRect);
						if (bound.xMax - bound.xMin >= 0 && bound.yMax - bound.yMin > 0)
						{
							float u0 = (bound.xMin - rect.x) / rect.width;
							float u1 = (bound.xMax - rect.x) / rect.width;
							float v0 = (rect.y + rect.height - bound.yMax) / rect.height;
							float v1 = (rect.y + rect.height - bound.yMin) / rect.height;
							u0 = Mathf.Lerp(uvTmp.xMin, uvTmp.xMax, u0);
							u1 = Mathf.Lerp(uvTmp.xMin, uvTmp.xMax, u1);
							v0 = Mathf.Lerp(uvTmp.yMin, uvTmp.yMax, v0);
							v1 = Mathf.Lerp(uvTmp.yMin, uvTmp.yMax, v1);
							QuadBatch.FillUVOfQuad(uv, vertCount, Rect.MinMaxRect(u0, v0, u1, v1));

							if (i == 0 && j == 0)
								offset = new Vector2(bound.x, bound.y);
							bound.x -= offset.x;
							bound.y -= offset.y;

							QuadBatch.FillVertsOfQuad(verts, vertCount, bound, GRoot.contentScaleFactor, GRoot.contentScaleFactor);

							vertCount += 4;
						}
					}
				}
			}
			else if (_scale9Grid == null || (this.scaleX == 1 && this.scaleY == 1))
			{
				verts = new Vector3[quadBatch.vertices.Length];
				uv = new Vector2[quadBatch.uv.Length];

				Rect bound = ToolSet.Intersection(ref contentRect, ref localRect);

				float u0 = bound.xMin / contentRect.width;
				float u1 = bound.xMax / contentRect.width;
				float v0 = (contentRect.height - bound.yMax) / contentRect.height;
				float v1 = (contentRect.height - bound.yMin) / contentRect.height;
				u0 = Mathf.Lerp(uvRect.xMin, uvRect.xMax, u0);
				u1 = Mathf.Lerp(uvRect.xMin, uvRect.xMax, u1);
				v0 = Mathf.Lerp(uvRect.yMin, uvRect.yMax, v0);
				v1 = Mathf.Lerp(uvRect.yMin, uvRect.yMax, v1);
				QuadBatch.FillUVOfQuad(uv, 0, Rect.MinMaxRect(u0, v0, u1, v1));

				bound.x = 0;
				bound.y = 0;
				QuadBatch.FillVertsOfQuad(verts, 0, bound, scaleX, scaleY);
				vertCount += 4;
			}
			else
			{
				verts = new Vector3[36];
				uv = new Vector2[36];

				localRect.xMin *= this.scaleX;
				localRect.xMax *= this.scaleX;
				localRect.yMin *= this.scaleY;
				localRect.yMax *= this.scaleY;

				float scale9Width = contentRect.width * this.scaleX / GRoot.contentScaleFactor;
				float scale9Height = contentRect.height * this.scaleY / GRoot.contentScaleFactor;

				float[] rows;
				float[] cols;
				float[] dRows;
				float[] dCols;
				Rect gridRect = (Rect)_scale9Grid;

				rows = new float[] { 0, gridRect.yMin, gridRect.yMax, contentRect.height };
				cols = new float[] { 0, gridRect.xMin, gridRect.xMax, contentRect.width };

				if (scale9Height >= (contentRect.height - gridRect.height))
					dRows = new float[] { 0, gridRect.yMin, scale9Height - (contentRect.height - gridRect.yMax), scale9Height };
				else
				{
					float tmp = gridRect.yMin / (contentRect.height - gridRect.yMax);
					tmp = scale9Height * tmp / (1 + tmp);
					dRows = new float[] { 0, tmp, tmp, scale9Height };
				}

				if (scale9Width >= (contentRect.width - gridRect.width))
					dCols = new float[] { 0, gridRect.xMin, scale9Width - (contentRect.width - gridRect.xMax), scale9Width };
				else
				{
					float tmp = gridRect.xMin / (contentRect.width - gridRect.xMax);
					tmp = scale9Width * tmp / (1 + tmp);
					dCols = new float[] { 0, tmp, tmp, scale9Width };
				}

				float scaleXLeft = contentRect.width * this.scaleX / scale9Width;
				float scaleYLeft = contentRect.height * this.scaleY / scale9Height;

				Vector2 offset = new Vector2();
				for (int cy = 0; cy < 3; cy++)
				{
					for (int cx = 0; cx < 3; cx++)
					{
						Rect rect = Rect.MinMaxRect(dCols[cx] * scaleXLeft, dRows[cy] * scaleYLeft,
							dCols[cx + 1] * scaleXLeft, dRows[cy + 1] * scaleYLeft);
						Rect bound = ToolSet.Intersection(ref rect, ref localRect);
						if (bound.xMax - bound.xMin >= 0 && bound.yMax - bound.yMin > 0)
						{
							Rect texBound = Rect.MinMaxRect(uvRect.x + cols[cx] / contentRect.width * uvRect.width,
								uvRect.y + (1 - rows[cy + 1] / contentRect.height) * uvRect.height,
								uvRect.x + cols[cx + 1] / contentRect.width * uvRect.width,
								uvRect.y + (1 - rows[cy] / contentRect.height) * uvRect.height);

							float u0 = (bound.xMin - rect.x) / rect.width;
							float u1 = (bound.xMax - rect.x) / rect.width;
							float v0 = (rect.y + rect.height - bound.yMax) / rect.height;
							float v1 = (rect.y + rect.height - bound.yMin) / rect.height;
							u0 = Mathf.Lerp(texBound.xMin, texBound.xMax, u0);
							u1 = Mathf.Lerp(texBound.xMin, texBound.xMax, u1);
							v0 = Mathf.Lerp(texBound.yMin, texBound.yMax, v0);
							v1 = Mathf.Lerp(texBound.yMin, texBound.yMax, v1);
							QuadBatch.FillUVOfQuad(uv, vertCount, Rect.MinMaxRect(u0, v0, u1, v1));

							if (vertCount == 0)
								offset = new Vector2(bound.x, bound.y);
							bound.x -= offset.x;
							bound.y -= offset.y;
							QuadBatch.FillVertsOfQuad(verts, vertCount, bound);

							vertCount += 4;
						}
					}
				}
			}

			if (vertCount != verts.Length)
			{
				Array.Resize(ref verts, vertCount);
				Array.Resize(ref uv, vertCount);
			}
			int triangleCount = (vertCount >> 1) * 3;
			triangles = new int[triangleCount];
			int k = 0;
			for (int i = 0; i < vertCount; i += 4)
			{
				triangles[k++] = i;
				triangles[k++] = i + 1;
				triangles[k++] = i + 2;

				triangles[k++] = i + 2;
				triangles[k++] = i + 3;
				triangles[k++] = i;
			}

			colors = new Color32[vertCount];
			for (int i = 0; i < vertCount; i++)
			{
				Color col = _color;
				col.a = this.alpha;
				colors[i] = col;
			}

			mesh.Clear();
			mesh.vertices = verts;
			mesh.uv = uv;
			mesh.triangles = triangles;
			mesh.colors32 = colors;
		}
	}
}
