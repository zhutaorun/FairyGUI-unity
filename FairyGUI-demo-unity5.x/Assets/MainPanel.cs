using System.Collections.Generic;
using FairyGUI;

public class MainPanel
{
	private GComponent _view;
	private GObject _backBtn;
	private GComponent _demoContainer;
	private Controller _cc;

	private Dictionary<string, GComponent> _demoObjects;

	public MainPanel()
	{
		_view = UIPackage.CreateObject("Demo", "Demo").asCom;
		_view.fairyBatching = true;//优化drawcall，可以切换这条语句看效果
		_view.SetSize(GRoot.inst.width, GRoot.inst.height);
		_view.AddRelation(GRoot.inst, RelationType.Size);
		GRoot.inst.AddChild(_view);

		_backBtn = _view.GetChild("btn_Back");
		_backBtn.visible = false;
		_backBtn.onClick.Add(onClickBack);

		_demoContainer = _view.GetChild("container").asCom;
		_cc = _view.GetController("c1");

		int cnt = _view.numChildren;
		for (int i = 0; i < cnt; i++)
		{
			GObject obj = _view.GetChildAt(i);
			if (obj.group != null && obj.group.name == "btns")
				obj.onClick.Add(runDemo);
		}

		_demoObjects = new Dictionary<string, GComponent>();
	}

	private void runDemo(EventContext context)
	{
		string type = ((GObject)(context.sender)).name.Substring(4);
		GComponent obj;
		if (!_demoObjects.TryGetValue(type, out obj))
		{
			obj = UIPackage.CreateObject("Demo", "Demo_" + type).asCom;
			_demoObjects[type] = obj;
		}

		_demoContainer.RemoveChildren();
		_demoContainer.AddChild(obj);
		_cc.selectedIndex = 1;
		_backBtn.visible = true;

		switch (type)
		{
			case "Button":
				PlayButton();
				break;

			case "Text":
				PlayText();
				break;

			case "Transition":
				PlayTransition();
				break;

			case "Window":
				PlayWindow();
				break;

			case "PopupMenu":
				PlayPopupMenu();
				break;

			case "Drag&Drop":
				PlayDragDrop();
				break;
		}
	}

	private void onClickBack()
	{
		_cc.selectedIndex = 0;
		_backBtn.visible = false;
	}

	//-----------------------------
	private void PlayButton()
	{
		GComponent obj = _demoObjects["Button"];
		obj.GetChild("n34").onClick.Add(() => { UnityEngine.Debug.Log("click button"); });
	}

	//------------------------------
	private void PlayText()
	{
		GComponent obj = _demoObjects["Text"];
		//!!注意这里是fairygui.event.TextEvent而不是flash.events.TextEvent
		obj.GetChild("n12").asRichTextField.onClickLink.Add((EventContext context) =>
		{
			GRichTextField t = context.sender as GRichTextField;
			t.text = "[img]ui://9leh0eyft9fj5f[/img][color=#FF0000]你点击了链接[/color]：" + context.data;
		});
	}

	//------------------------------
	private void PlayTransition()
	{
		GComponent obj = _demoObjects["Transition"];
		obj.GetChild("n2").asCom.GetTransition("t0").Play(int.MaxValue, 0, null);
		obj.GetChild("n3").asCom.GetTransition("peng").Play(int.MaxValue, 0, null);

		obj.onAddedToStage.Add(() =>
		{
			obj.GetChild("n2").asCom.GetTransition("t0").Stop();
			obj.GetChild("n3").asCom.GetTransition("peng").Stop();
		});
	}

	//------------------------------
	private Window _winA;
	private Window _winB;
	private void PlayWindow()
	{
		GComponent obj = _demoObjects["Window"];
		obj.GetChild("n0").onClick.Add(() =>
		{
			if (_winA == null)
				_winA = new WindowA();
			_winA.Show();
		});

		obj.GetChild("n1").onClick.Add(() =>
		{
			if (_winB == null)
				_winB = new WindowB();
			_winB.Show();
		});
	}

	//------------------------------
	private PopupMenu _pm;
	private void PlayPopupMenu()
	{
		if (_pm == null)
		{
			_pm = new PopupMenu();
			_pm.AddItem("Item 1", __clickMenu);
			_pm.AddItem("Item 2", __clickMenu);
			_pm.AddItem("Item 3", __clickMenu);
			_pm.AddItem("Item 4", __clickMenu);
		}

		GComponent obj = _demoObjects["PopupMenu"];
		GObject btn = obj.GetChild("n0");
		btn.onClick.Add(() =>
		{
			_pm.Show(btn, true);
		});

		obj.onRightClick.Add(() =>
		{
			_pm.Show();
		});
	}

	private void __clickMenu(EventContext context)
	{
		GObject itemObject = (GObject)context.data;
		UnityEngine.Debug.Log("click " + itemObject.text);
	}

	//------------------------------
	private void PlayDragDrop()
	{
		GComponent obj = _demoObjects["Drag&Drop"];
		obj.GetChild("n0").draggable = true;

		GButton btn1 = obj.GetChild("n1").asButton;
		btn1.draggable = true;
		btn1.onDragStart.Add((EventContext context) =>
		{
			//取消对原目标的拖动，换成一个替代品
			context.PreventDefault();

			DragManager.inst.StartDrag(btn1, btn1.icon, btn1.icon, (int)context.data);
		});

		GButton btn2 = obj.GetChild("n2").asButton;
		btn2.icon = null;
		btn2.AddEventListener(DragManager.DROP_EVENT, (EventContext context) =>
		{
			btn2.icon = (string)context.data;
		});
	}
}
