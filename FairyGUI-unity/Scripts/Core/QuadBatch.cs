using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
	public enum QuickIndices
	{
		None,
		Quad, //4个顶点组成的四边形
		Grid9 //16个顶点组成的九宫格
	}

	public class QuadBatch
	{
		public Vector3[] vertices { get; private set; }
		public Vector2[] uv { get; private set; }
		public Color32[] colors { get; private set; }
		public int[] triangles { get; private set; }
		public byte[] quadAlpha { get; private set; }
		public int quadCount { get; private set; }

		GameObject gameObject;
		MeshFilter meshFilter;
		MeshRenderer meshRenderer;
		Mesh mesh;

		float _alpha;
		NTexture _texture;
		string _shader;
		Material _material;
		MaterialManager _manager;
		bool _colorChanged;

		static int[] TRIANGLES = new int[] { 0, 1, 2, 2, 3, 0 };
		static int[] TRIANGLES_9_GRID = new int[] { 
			4,0,1,1,5,4,
			5,1,2,2,6,5,
			6,2,3,3,7,6,
			8,4,5,5,9,8,
			9,5,6,6,10,9,
			10,6,7,7,11,10,
			12,8,9,9,13,12,
			13,9,10,10,14,13,
			14,10,11,
			11,15,14
        };

		static public List<Vector3> sCachedVerts = new List<Vector3>();
		static public List<Vector2> sCachedUVs = new List<Vector2>();
		static public List<Color32> sCachedCols = new List<Color32>();

		public QuadBatch(GameObject gameObject)
		{
			_alpha = 1f;
			this.gameObject = gameObject;
			_shader = ShaderConfig.imageShader;
			meshFilter = gameObject.AddComponent<MeshFilter>();
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
#if UNITY_5_0_DOWNWARDS
            meshRenderer.castShadows = false;
#else
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#endif
			meshRenderer.receiveShadows = false;
			mesh = new Mesh();
			mesh.hideFlags = HideFlags.DontSave;
			mesh.MarkDynamic();
		}

		public NTexture texture
		{
			get { return _texture; }
			set
			{
				if (_texture != value)
				{
					_texture = value;
					if (_texture != null)
					{
						_manager = MaterialManager.GetInstance(_texture, _shader);
						if (_material != null)
							_material.mainTexture = _texture.nativeTexture;
					}
					else
					{
						if (_material != null)
							_material.mainTexture = null;
						_manager = null;
					}
				}
			}
		}

		public string shader
		{
			get { return _shader; }
			set
			{
				_shader = value;
				if (_texture != null)
					_manager = MaterialManager.GetInstance(_texture, _shader);
			}
		}

		public Material material
		{
			get
			{
				if (_material != null)
					return _material;
				else if (_manager != null)
					return _manager.sharedMaterial;
				else
					return null;
			}
			set
			{
				_material = value;
				if (_material != null)
				{
					if (_texture != null)
						_material.mainTexture = _texture.nativeTexture;
				}
			}
		}

		public bool enabled
		{
			get
			{
				return meshRenderer.enabled;
			}
			set
			{
				meshRenderer.enabled = value;
			}
		}

		public void Dispose()
		{
			if (mesh != null)
			{
				Mesh.Destroy(mesh);
				mesh = null;
			}
			_manager = null;
			_material = null;
			meshRenderer = null;
			meshFilter = null;
		}

		virtual public void Update(UpdateContext context, float alpha)
		{
			if ((System.Object)meshFilter == null || _manager == null || (System.Object)mesh == null)
				return;

			if (_colorChanged || _alpha != alpha)
			{
				_colorChanged = false;
				_alpha = alpha;

				Color32[] cols = mesh.colors32;
				int count = cols.Length;
				for (int i = 0; i < count; i++)
				{
					Color32 col = cols[i];
					int j = i / 4;
					col.a = (byte)(_alpha * quadAlpha[j]);
					cols[i] = col;
				}
				mesh.colors32 = cols;
			}

			Material mat;
			if ((System.Object)_material != null)
				mat = _material;
			else
				mat = _manager.GetContextMaterial(context);
			if ((System.Object)mat != (System.Object)meshRenderer.sharedMaterial && (System.Object)mat.mainTexture != null)
				meshRenderer.sharedMaterial = mat;
		}

		public void Fill(Vector3[] verts, Vector2[] uvs, Color color)
		{
			Fill(verts, uvs, color, QuickIndices.None);
		}

		public void Fill(Vector3[] verts, Vector2[] uvs, Color color, QuickIndices indicesType)
		{
			int vertCount = verts.Length;
			Color32[] cols = new Color32[vertCount];
			for (int i = 0; i < vertCount; i++)
				cols[i] = color;

			Fill(verts, uvs, cols, indicesType);
		}

		public void Fill(Rect drawRect, float scaleX, float scaleY, Rect uvRect, Color color)
		{
			Vector3[] verts = new Vector3[4];
			Vector2[] uv = new Vector2[4];
			FillVertsOfQuad(verts, 0, drawRect, scaleX, scaleY);
			FillUVOfQuad(uv, 0, uvRect);
			Fill(verts, uv, color, QuickIndices.Quad);
		}

		public void Fill(Vector3[] vertices, Vector2[] uv, Color32[] colors)
		{
			Fill(vertices, uv, colors, QuickIndices.None);
		}

		public void Fill(Vector3[] vertices, Vector2[] uv, Color32[] colors, QuickIndices indicesType)
		{
			this.vertices = vertices;
			this.uv = uv;
			this.colors = colors;

			mesh.Clear();

			int vertCount = vertices.Length;
			quadCount = vertCount / 4;
			if (quadAlpha == null || quadAlpha.Length != quadCount)
				quadAlpha = new byte[quadCount];

			for (int i = 0; i < vertCount; i++)
			{
				Color32 col = colors[i];
				int j = i / 4;
				quadAlpha[j] = col.a;
				col.a = (byte)(col.a * _alpha);
				colors[i] = col;
			}

			int triangleCount = (vertCount >> 1) * 3;

			if (indicesType == QuickIndices.Quad)
				triangles = TRIANGLES;
			else if (indicesType == QuickIndices.Grid9)
				triangles = TRIANGLES_9_GRID;
			else
			{
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
			}

			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.triangles = triangles;
			mesh.colors32 = colors;

			meshFilter.mesh = mesh;
		}

		public void Clear()
		{
			quadCount = 0;

			mesh.Clear();
			meshFilter.mesh = mesh;
		}

		public void Tint(Color value)
		{
			Color32[] cols = mesh.colors32;
			int count = cols.Length;
			for (int i = 0; i < count; i++)
			{
				Color32 col = cols[i];
				col.r = (byte)(value.r * 255);
				col.g = (byte)(value.g * 255);
				col.b = (byte)(value.b * 255);
				cols[i] = col;
			}
			mesh.colors32 = cols;
		}

		public void SetQuadAlpha(int from, int count, float value)
		{
			if (quadAlpha == null)
				return;

			int to = Mathf.Min(from + count, quadAlpha.Length);

			byte a = (byte)(value * 255);
			for (int i = from; i < to; i++)
				quadAlpha[i] = a;

			_colorChanged = true;
		}

		public void SetQuadAlpha(int from, int count, float value, float otherValue)
		{
			if (quadAlpha == null)
				return;

			int to = from + count;
			count = quadAlpha.Length;

			byte a = (byte)(value * 255);
			byte b = (byte)(otherValue * 255);
			for (int i = 0; i < count; i++)
			{
				if (i >= from && i < to)
					quadAlpha[i] = a;
				else
					quadAlpha[i] = b;
			}

			_colorChanged = true;
		}

		public void SetQuadAlpha(int from, int count, Rect rect, float value, float otherValue)
		{
			if (quadAlpha == null)
				return;

			float yMin = -rect.yMax;
			float yMax = -rect.yMin;

			byte a = (byte)(value * 255);
			byte b = (byte)(otherValue * 255);
			int to = from + count;
			count = quadAlpha.Length;

			for (int i = 0; i < count; i++)
			{
				if (i >= from && i < to)
				{
					Vector3 vertBottomLeft = vertices[i * 4];
					Vector3 vertTopRight = vertices[i * 4 + 2];
					if (vertBottomLeft.x >= rect.xMin && vertTopRight.x <= rect.xMax
						&& vertBottomLeft.y >= yMin && vertTopRight.y <= yMax)
						quadAlpha[i] = a;
					else
						quadAlpha[i] = b;
				}
			}

			_colorChanged = true;
		}

		public static void FillVertsOfQuad(Vector3[] verts, int index, Rect rect,
			float scaleX, float scaleY)
		{
			verts[index] = new Vector3(rect.xMin * scaleX, -rect.yMax * scaleY, 0f);
			verts[index + 1] = new Vector3(rect.xMin * scaleX, -rect.yMin * scaleY, 0f);
			verts[index + 2] = new Vector3(rect.xMax * scaleX, -rect.yMin * scaleY, 0f);
			verts[index + 3] = new Vector3(rect.xMax * scaleX, -rect.yMax * scaleY, 0f);
		}

		public static void FillVertsOfQuad(Vector3[] verts, int index, Rect rect)
		{
			verts[index] = new Vector3(rect.xMin, -rect.yMax, 0f);
			verts[index + 1] = new Vector3(rect.xMin, -rect.yMin, 0f);
			verts[index + 2] = new Vector3(rect.xMax, -rect.yMin, 0f);
			verts[index + 3] = new Vector3(rect.xMax, -rect.yMax, 0f);
		}

		public static void FillUVOfQuad(Vector2[] uv, int index, Rect rect)
		{
			uv[index] = new Vector2(rect.xMin, rect.yMin);
			uv[index + 1] = new Vector2(rect.xMin, rect.yMax);
			uv[index + 2] = new Vector2(rect.xMax, rect.yMax);
			uv[index + 3] = new Vector2(rect.xMax, rect.yMin);
		}
	}
}
