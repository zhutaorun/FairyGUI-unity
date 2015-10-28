using UnityEngine;
using FairyGUI;

public class Main : MonoBehaviour
{
	GComponent _mainView;

	GButton _btn0;
	Material _mat0;
	float _time1;

	GButton _btn1;
	Material _mat1;
	float _time2;

	void Start()
	{
		Application.targetFrameRate = 60;
		DontDestroyOnLoad(this.gameObject);

		Stage.Instantiate(5);
		Stage.inst.AddChild(new GRoot().displayObject);
		Stage.inst.camera.clearFlags = CameraClearFlags.Depth;
		Stage.inst.onKeyDown.Add(OnKeyDown);

		GRoot.inst.SetContentScaleFactor(1136, 640);

		UIPackage.AddPackage("UI/CustomMaterialDemo");

		_mainView = UIPackage.CreateObject("Demo", "Main").asCom;
		_mainView.SetSize(GRoot.inst.width, GRoot.inst.height);
		_mainView.AddRelation(GRoot.inst, RelationType.Size);
		GRoot.inst.AddChild(_mainView);

		_btn0 = _mainView.GetChild("b0").asButton;
		_btn0.icon = "k0";
		_time1 = 5;
		_mat0 = new Material(Shader.Find("Cooldown mask"));
		_mat0.SetFloat("_Progress", 0f);
		_mat0.SetTexture("_MaskTex", (Texture)Resources.Load("CooldownMask"));
		_btn0.GetChild("icon").asLoader.material = _mat0;

		_btn1 = _mainView.GetChild("b1").asButton;
		_btn1.icon = "k1";
		_time2 = 10;
		_mat1 = new Material(Shader.Find("Cooldown mask"));
		_mat1.SetFloat("_Progress", 0f);
		_mat1.SetTexture("_MaskTex", (Texture)Resources.Load("CooldownMask"));
		_btn1.GetChild("icon").asLoader.material = _mat1;
	}

	void OnKeyDown(EventContext context)
	{
		if (context.inputEvent.keyCode == KeyCode.Escape)
		{
			Application.Quit();
		}
	}

	void Update()
	{
		_time1 -= Time.deltaTime;
		if (_time1 < 0)
			_time1 = 5;
		_mat0.SetFloat("_Progress", (5 - _time1) / 5);

		_time2 -= Time.deltaTime;
		if (_time2 < 0)
			_time2 = 10;
		_btn1.text = string.Empty + Mathf.RoundToInt(_time2);
		_mat1.SetFloat("_Progress", (10 - _time2) / 10);
	}
}