using UnityEngine;
using FairyGUI;

public class Main : MonoBehaviour
{
	GComponent _mainView;
	BagWindow _bagWindow;

	void Start()
	{
		Application.targetFrameRate = 60;
		DontDestroyOnLoad(this.gameObject);

		Stage.Instantiate(5);
		Stage.inst.AddChild(new GRoot().displayObject);
		Stage.inst.camera.clearFlags = CameraClearFlags.Depth;
		Stage.inst.onKeyDown.Add(OnKeyDown);

		//setup custom loader classs
		UIObjectFactory.SetLoaderExtension(typeof(MyGLoader));

		GRoot.inst.SetContentScaleFactor(1136, 640);

		UIPackage.AddPackage("BagDemo");

		_mainView = UIPackage.CreateObject("Bag", "Main").asCom;
		_mainView.SetSize(GRoot.inst.width, GRoot.inst.height);
		_mainView.AddRelation(GRoot.inst, RelationType.Size);
		GRoot.inst.AddChild(_mainView);

		_bagWindow = new BagWindow();

		_mainView.GetChild("bagBtn").onClick.Add(() => { _bagWindow.Show(); });
	}


	void OnKeyDown(EventContext context)
	{
		if (context.inputEvent.keyCode == KeyCode.Escape)
		{
			Application.Quit();
		}
	}
}