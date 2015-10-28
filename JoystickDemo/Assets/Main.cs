using UnityEngine;
using FairyGUI;

public class Main : MonoBehaviour
{
	GComponent _mainView;
	JoystickModule _joystick;
	Transform _npc;

	void Start()
	{
		Application.targetFrameRate = 60;
		DontDestroyOnLoad(this.gameObject);

		Stage.Instantiate(5);
		Stage.inst.AddChild(new GRoot().displayObject);
		Stage.inst.camera.clearFlags = CameraClearFlags.Depth;
		Stage.inst.onKeyDown.Add(OnKeyDown);

		GRoot.inst.SetContentScaleFactor(1136, 640);

		UIPackage.AddPackage("UI/JoystickDemo");

		_mainView = UIPackage.CreateObject("Demo", "Main").asCom;
		_mainView.SetSize(GRoot.inst.width, GRoot.inst.height);
		_mainView.AddRelation(GRoot.inst, RelationType.Size);
		GRoot.inst.AddChild(_mainView);

		_joystick = new JoystickModule(_mainView);
		_joystick.onMove.Add(__joystickMove);
		_joystick.onEnd.Add(__joystickEnd);

		_npc = GameObject.Find("npc").transform;
	}

	void OnKeyDown(EventContext context)
	{
		if (context.inputEvent.keyCode == KeyCode.Escape)
		{
			Application.Quit();
		}
	}

	void __joystickMove(EventContext context)
	{
		float degree = (float)context.data;
		//-90:up 0:right 90:down 180/-180:left
		if (degree > -90 && degree < 90) //right
			_npc.Translate(0.01f, 0, 0, Space.World);
		else
			_npc.Translate(-0.01f, 0, 0, Space.World);
	}

	void __joystickEnd()
	{
	}
}