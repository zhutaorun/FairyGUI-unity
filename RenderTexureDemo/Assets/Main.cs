using UnityEngine;
using FairyGUI;

public class Main : MonoBehaviour
{
	GComponent _mainView;
	TestWindow _testWindow;

	void Start()
	{
		Application.targetFrameRate = 60;
		DontDestroyOnLoad(this.gameObject);

		Stage.Instantiate(5);
		Stage.inst.AddChild(new GRoot().displayObject);
		Stage.inst.camera.clearFlags = CameraClearFlags.Depth;
		Stage.inst.onKeyDown.Add(OnKeyDown);

		GRoot.inst.SetContentScaleFactor(1136, 640);

		UIPackage.AddPackage("UI/RenderTextureDemo");

		_mainView = UIPackage.CreateObject("Demo", "Main").asCom;
		_mainView.SetSize(GRoot.inst.width, GRoot.inst.height);
		_mainView.AddRelation(GRoot.inst, RelationType.Size);
		GRoot.inst.AddChild(_mainView);

		_testWindow = new TestWindow();

		_mainView.GetChild("bagBtn").onClick.Add(() => { _testWindow.Show(); });
	}

	void OnKeyDown(EventContext context)
	{
		if (context.inputEvent.keyCode == KeyCode.Escape)
		{
			Application.Quit();
		}
	}
}