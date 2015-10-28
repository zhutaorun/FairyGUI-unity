using FairyGUI;
using FairyGUI.Utils;
using System.Collections;
using UnityEngine;

public class RenderImage
{
	Image _image;
	Transform _root;
	Transform _modelRoot;
	Transform _background;
	Transform _model;
	RenderTexture _renderTexture;
	int _width;
	int _height;
	bool _cacheTexture;
	float _rotating;

	static int _gid;
	static Camera _camera;
	static int _cameraRef;

	public RenderImage(GGraph holder)
	{
		_width = Mathf.RoundToInt(holder.width * GRoot.contentScaleFactor);
		_height = Mathf.RoundToInt(holder.height * GRoot.contentScaleFactor);
		_cacheTexture = true;

		this._image = new Image();
		holder.SetNativeObject(this._image);

		if (_camera == null)
			CreateCamera();

		this._root = new GameObject("render_image" + _gid++).transform;
		Object.DontDestroyOnLoad(this._root.gameObject);
		this._root.SetParent(_camera.transform, false);
		SetLayer(this._root.gameObject, "Hidden");

		this._modelRoot = new GameObject("model_root").transform;
		Object.DontDestroyOnLoad(this._modelRoot.gameObject);
		this._modelRoot.SetParent(this._root, false);

		this._background = new GameObject("background").transform;
		Object.DontDestroyOnLoad(this._background.gameObject);
		this._background.SetParent(this._root, false);

		this._image.onAddedToStage.Add(OnAddedToStage);
		this._image.onRemovedFromStage.Add(OnRemoveFromStage);

		if (this._image.stage != null)
			OnAddedToStage();
	}

	public void Dispose()
	{
		MeshFilter meshFilter = this._background.gameObject.GetComponent<MeshFilter>();
		MeshRenderer meshRenderer = this._background.gameObject.GetComponent<MeshRenderer>();
		if (meshFilter != null)
			Object.Destroy(meshFilter);
		if (meshRenderer != null)
			Object.Destroy(meshRenderer);
		Object.Destroy(this._background.gameObject);

		UnloadModel();

		Object.Destroy(this._root.gameObject);
		Object.Destroy(this._modelRoot.gameObject);
		DestroyTexture();

		this._image.Dispose();
		this._image = null;
	}

	public void SetBackground(GObject image)
	{
		SetBackground(image, null);
	}

	public void SetBackground(GObject image1, GObject image2)
	{
		Image source1 = (Image)image1.displayObject;
		Image source2 = image2 != null ? (Image)image2.displayObject : null;

		Vector3 pos = this._background.position;
		pos.z = _camera.farClipPlane;
		this._background.position = pos;

		Mesh mesh = new Mesh();
		Rect rect = this._image.TransformRect(new Rect(0, 0, this._width, this._height), source1);
		source1.PrintTo(mesh, rect);

		Vector2[] tmp = mesh.uv;
		if (source2 != null)
		{
			//如果两张图，那两张图都必须只有4个顶点(即要注意九宫格和平铺的影响），因为如果各自顶点数不同，我不知道该怎么办啊^_^
			rect = this._image.TransformRect(new Rect(0, 0, this._width, this._height), source2);
			source2.PrintTo(mesh, rect);

#if UNITY_5
			mesh.uv2 = mesh.uv;
#else
			mesh.uv1 = mesh.uv;
#endif
			mesh.uv = tmp;
		}

		Vector2[] tmp2 = new Vector2[tmp.Length];
		FairyGUI.Utils.ToolSet.uvLerp(tmp, tmp2, 0, 1);

		int cnt = tmp2.Length;
		Vector3[] verts = mesh.vertices;
		for (int i = 0; i < cnt; i++)
		{
			Vector2 v2 = tmp2[i];
			verts[i] = new Vector3(v2.x * 2 - 1, v2.y * 2 - 1);
		}
		mesh.vertices = verts;

		MeshFilter meshFilter = this._background.gameObject.GetComponent<MeshFilter>();
		if (meshFilter == null)
			meshFilter = this._background.gameObject.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;
		MeshRenderer meshRenderer = this._background.gameObject.GetComponent<MeshRenderer>();
		if (meshRenderer == null)
			meshRenderer = this._background.gameObject.AddComponent<MeshRenderer>();
#if UNITY_5
		meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#else
		meshRenderer.castShadows = false;
#endif
		meshRenderer.receiveShadows = false;
		Shader shader = Shader.Find("Game/FullScreen");
		Material mat = new Material(shader);
		mat.mainTexture = source1.texture.nativeTexture;
		if (source2 != null)
			mat.SetTexture("_Tex2", source2.texture.nativeTexture);
		meshRenderer.material = mat;
	}

	public void LoadModel(string model)
	{
		this.UnloadModel();

		Object prefab = Resources.Load(model);
		GameObject go = ((GameObject)Object.Instantiate(prefab));
		_model = go.transform;
		_model.SetParent(this._modelRoot, false);

		this._modelRoot.localPosition = new Vector3(0, -1.2f, 5f);
		this._modelRoot.localScale = new Vector3(1, 1, 1);
		this._modelRoot.localRotation = Quaternion.Euler(0, 120, 0);
	}

	public void UnloadModel()
	{
		if (_model != null)
		{
			Object.Destroy(_model.gameObject);
			_model = null;
		}
		_rotating = 0;
	}

	public void StartRotate(float delta)
	{
		_rotating = delta;
	}

	public void StopRotate()
	{
		_rotating = 0;
	}

	void CreateTexture()
	{
		if (_renderTexture != null)
			return;

		_renderTexture = new RenderTexture(_width, _height, 24, RenderTextureFormat.ARGB32)
		{
			antiAliasing = 1,
			filterMode = FilterMode.Bilinear,
			anisoLevel = 0,
			useMipMap = false
		};
		this._image.texture = new NTexture(_renderTexture);
		this._image.quadBatch.shader = "FairyGUI/Image (Opaque)";
	}

	void DestroyTexture()
	{
		if (_renderTexture != null)
		{
			Object.Destroy(_renderTexture);
			_renderTexture = null;
			this._image.texture = null;
		}
	}

	void OnAddedToStage()
	{
		if (_renderTexture == null)
			CreateTexture();

		AddRef();
		Timers.inst.Add(0.0001f, 0, this.Render);
		this._root.gameObject.SetActive(true);

		Render();
	}

	void OnRemoveFromStage()
	{
		if (!_cacheTexture)
			DestroyTexture();

		ReleaseRef();
		Timers.inst.Remove(this.Render);

		this._root.gameObject.SetActive(false);
	}

	void Render(object param = null)
	{
		if (_rotating != 0 && this._modelRoot != null)
		{
			Vector3 localRotation = this._modelRoot.localRotation.eulerAngles;
			localRotation.y += _rotating;
			this._modelRoot.localRotation = Quaternion.Euler(localRotation);
		}

		SetLayer(this._root.gameObject, "UICharacter");

		_camera.targetTexture = this._renderTexture;
		RenderTexture old = RenderTexture.active;
		RenderTexture.active = this._renderTexture;
		GL.Clear(true, true, Color.clear);
		_camera.Render();
		RenderTexture.active = old;

		SetLayer(this._root.gameObject, "Hidden");
	}

	void SetLayer(GameObject go, string name)
	{
		int nameToLayer = LayerMask.NameToLayer(name);
		Transform[] transforms = go.GetComponentsInChildren<Transform>(true);
		foreach (Transform t in transforms)
		{
			t.gameObject.layer = nameToLayer;
		}
	}

	static void CreateCamera()
	{
		Object prefab = Resources.Load("RenderImageCamera");
		GameObject go = (GameObject)Object.Instantiate(prefab);
		_camera = go.GetComponent<Camera>();
		_camera.transform.position = new Vector3(0, 1000, 0);
		_camera.enabled = false;
		Object.DontDestroyOnLoad(_camera.gameObject);
	}

	static void AddRef()
	{
		_cameraRef++;
		if (_cameraRef > 0 && _camera.gameObject.activeSelf == false)
			_camera.gameObject.SetActive(true);
	}

	static void ReleaseRef()
	{
		if (_cameraRef > 0)
		{
			_cameraRef--;
			if (_cameraRef == 0 && _camera.gameObject.activeSelf)
				_camera.gameObject.SetActive(false);
		}
	}
}