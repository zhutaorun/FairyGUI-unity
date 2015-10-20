using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
	class Highlighter
	{
		public Transform cachedTransform { get; private set; }
		public QuadBatch quadBatch { get; private set; }
		public GameObject gameObject { get; private set; }
		public bool active { get; private set; }

		Color _color;
		List<Rect> _rects;
		int _startLine;

		public Highlighter()
		{
			gameObject = new GameObject("Highlighter");
			gameObject.hideFlags = HideFlags.HideInHierarchy;
			gameObject.layer = Stage.defaultLayer;
			Object.DontDestroyOnLoad(gameObject);
			cachedTransform = gameObject.transform;

			quadBatch = new QuadBatch(gameObject);
			quadBatch.texture = NTexture.Empty;
			quadBatch.enabled = false;

			_color = new Color(1f, 223f / 255f, 141f / 255f, 0.5f);
			_rects = new List<Rect>();
		}

		public void SetParent(Transform parent)
		{
			if (parent != null)
			{
				active = true;
				cachedTransform.parent = parent;
				gameObject.layer = parent.gameObject.layer;
				cachedTransform.localPosition = new Vector3(0, 0, -0.00001f);
				cachedTransform.localScale = new Vector3(1, 1, 1);
			}
			else
			{
				active = false;
				cachedTransform.parent = null;
				quadBatch.enabled = false;
			}
		}

		public void BeginUpdate(int startLine)
		{
			_rects.Clear();
			_startLine = startLine;
		}

		public void AddRect(Rect rect)
		{
			_rects.Add(rect);
		}

		public void EndUpdate()
		{
			if (_rects.Count == 0)
			{
				quadBatch.enabled = false;
				return;
			}
			quadBatch.enabled = true;

			Vector3[] verts;
			Vector2[] uv;
			int count = _rects.Count * 4;
			verts = new Vector3[count];
			uv = new Vector2[count];
			Rect uvRect = new Rect(0, 0, 1, 1);
			for (int i = 0; i < count; i += 4)
			{
				QuadBatch.FillVertsOfQuad(verts, i, _rects[i / 4]);
				QuadBatch.FillUVOfQuad(uv, i, uvRect);
			}
			quadBatch.Fill(verts, uv, _color);
		}

		public void Clear()
		{
			quadBatch.enabled = false;
		}
	}
}
