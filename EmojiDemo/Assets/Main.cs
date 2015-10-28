using UnityEngine;
using FairyGUI;

public class Main : MonoBehaviour
{
	GComponent _mainView;
	GList _list;
	GTextInput _input;
	GComponent _emojiSelectUI;

	string _itemURL1;
	string _itemURL2;

	void Start()
	{
		Application.targetFrameRate = 60;
		DontDestroyOnLoad(this.gameObject);

		Stage.Instantiate(5);
		Stage.inst.AddChild(new GRoot().displayObject);
		Stage.inst.camera.clearFlags = CameraClearFlags.Depth;
		Stage.inst.onKeyDown.Add(OnKeyDown);

		GRoot.inst.SetContentScaleFactor(1136, 640);

		UIPackage.AddPackage("UI/EmojiDemo");

		UIConfig.verticalScrollBar = UIPackage.GetItemURL("Demo", "ScrollBar_VT");
		UIConfig.defaultScrollBarDisplay = ScrollBarDisplayType.Auto;

		_mainView = UIPackage.CreateObject("Demo", "Main").asCom;
		_mainView.SetSize(GRoot.inst.width, GRoot.inst.height);
		_mainView.AddRelation(GRoot.inst, RelationType.Size);
		GRoot.inst.AddChild(_mainView);

		_list = _mainView.GetChild("list").asList;
		_list.RemoveChildrenToPool();
		_input = _mainView.GetChild("input").asTextInput;
		_input.onKeyDown.Add(__inputKeyDown);

		_itemURL1 = UIPackage.GetItemURL("Demo", "chatLeft");
		_itemURL2 = UIPackage.GetItemURL("Demo", "chatRight");

		_mainView.GetChild("btnSend").onClick.Add(__clickSendBtn);
		_mainView.GetChild("btnEmoji").onClick.Add(__clickEmojiBtn);

		_emojiSelectUI = UIPackage.CreateObject("Demo", "EmojiSelectUI").asCom;
		_emojiSelectUI.GetChild("list").asList.onClickItem.Add(__clickEmoji);
	}

	void AddMsg(string sender, string senderIcon, string msg, bool fromMe)
	{
		bool isScrollBottom = _list.scrollPane.isBottomMost;

		GButton item = _list.AddItemFromPool(fromMe ? _itemURL2 : _itemURL1).asButton;
		if(!fromMe)
			item.GetChild("name").text = sender;
		item.icon = UIPackage.GetItemURL("Demo", senderIcon);

		//因为文本没有设置为自动宽度，所以需要进行额外的处理进行宽度的调整
		GRichTextField tf = item.GetChild("msg").asRichTextField;
		tf.width = tf.initWidth; //先恢复到设计时的宽度
		tf.text = EmojiParser.inst.Parse(msg);
		tf.width = tf.textWidth; //根据实际宽度调整

		if (fromMe)
		{
			if (_list.numChildren==1 || Random.Range(0f, 1f) < 0.5f)
			{
				AddMsg("FairyGUI", "r1", "Today is a good day. [:gz]", false);
			}
		}

		if (_list.numChildren > 30)
			_list.RemoveChildrenToPool(0, _list.numChildren - 30);

		if (isScrollBottom)
			_list.scrollPane.ScrollBottom(true);
	}

	void __clickSendBtn()
	{
		string msg = _input.text;
		if (msg.Length == 0)
			return;

		AddMsg("Unity", "r0", msg, true);
		_input.text = "";
	}

	void __clickEmojiBtn(EventContext context)
	{
		GRoot.inst.ShowPopup(_emojiSelectUI, (GObject)context.sender, false);
	}

	void __clickEmoji(EventContext context)
	{
		GButton item = (GButton)context.data;
		_input.ReplaceSelection("[:" + item.text + "]");
	}

	void __inputKeyDown(EventContext context)
	{
		if (context.inputEvent.keyCode == KeyCode.Return)
			__clickSendBtn();
	}

	void OnKeyDown(EventContext context)
	{
		if (context.inputEvent.keyCode == KeyCode.Escape)
		{
			Application.Quit();
		}
	}
}