using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
	public class GButton : GComponent
	{
		public PageOption pageOption { get; private set; }
		public AudioClip sound;
		public float soundVolumeScale;
		public bool changeStateOnClick;
		public GObject linkedPopup;

		public EventListener onChanged { get; private set; }

		protected GObject _titleObject;
		protected GObject _iconObject;
		protected Controller _relatedController;

		ButtonMode _mode;
		bool _selected;
		string _title;
		string _icon;
		string _selectedTitle;
		string _selectedIcon;
		Controller _buttonController;
		bool _menuItemGrayed;

		bool _down;
		bool _over;

		public const string UP = "up";
		public const string DOWN = "down";
		public const string OVER = "over";
		public const string SELECTED_OVER = "selectedOver";

		public GButton()
		{
			pageOption = new PageOption();

			sound = UIConfig.buttonSound;
			soundVolumeScale = UIConfig.buttonSoundVolumeScale;
			changeStateOnClick = true;

			onChanged = new EventListener(this, "onChanged");
		}

		public string icon
		{
			get
			{
				return _icon;
			}
			set
			{
				_icon = value;
				value = (_selected && _selectedIcon != null) ? _selectedIcon : _icon;
				if (_iconObject is GLoader)
					((GLoader)_iconObject).url = value;
				else if (_iconObject is GLabel)
					((GLabel)_iconObject).icon = value;
				else if (_iconObject is GButton)
					((GButton)_iconObject).icon = value;
			}
		}

		public string title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;
				if (_titleObject != null)
					_titleObject.text = (_selected && _selectedTitle != null) ? _selectedTitle : _title;
			}
		}

		override public string text
		{
			get { return this.title; }
			set { this.title = value; }
		}

		public string selectedIcon
		{
			get
			{
				return _selectedIcon;
			}
			set
			{
				_selectedIcon = value;
				value = (_selected && _selectedIcon != null) ? _selectedIcon : _icon;
				if (_iconObject is GLoader)
					((GLoader)_iconObject).url = value;
				else if (_iconObject is GLabel)
					((GLabel)_iconObject).icon = value;
				else if (_iconObject is GButton)
					((GButton)_iconObject).icon = value;
			}
		}

		public string selectedTitle
		{
			get
			{
				return _selectedTitle;
			}
			set
			{
				_selectedTitle = value;
				if (_titleObject != null)
					_titleObject.text = (_selected && _selectedTitle != null) ? _selectedTitle : _title; ;
			}
		}

		public Color titleColor
		{
			get
			{
				if (_titleObject is GTextField)
					return ((GTextField)_titleObject).color;
				else if (_titleObject is GLabel)
					return ((GLabel)_titleObject).titleColor;
				else if (_titleObject is GButton)
					return ((GButton)_titleObject).titleColor;
				else
					return Color.black;
			}
			set
			{
				if (_titleObject is GTextField)
					((GTextField)_titleObject).color = value;
				else if (_titleObject is GLabel)
					((GLabel)_titleObject).titleColor = value;
				else if (_titleObject is GButton)
					((GButton)_titleObject).titleColor = value;
			}
		}

		public bool selected
		{
			get
			{
				return _selected;
			}

			set
			{
				if (_mode == ButtonMode.Common)
					return;

				if (_selected != value)
				{
					_selected = value;
					if (_selected)
						SetState(_over ? SELECTED_OVER : DOWN);
					else
						SetState(_over ? OVER : UP);
					if (_selectedTitle != null && _titleObject != null)
						_titleObject.text = _selected ? _selectedTitle : _title;
					if (_selectedIcon != null)
					{
						string str = _selected ? _selectedIcon : _icon;
						if (_iconObject is GLoader)
							((GLoader)_iconObject).url = str;
						else if (_iconObject is GLabel)
							((GLabel)_iconObject).icon = str;
						else if (_iconObject is GButton)
							((GButton)_iconObject).icon = str;
					}
					if (_relatedController != null
						&& parent != null
						&& !parent._buildingDisplayList)
					{
						if (_selected)
							_relatedController.selectedPageId = pageOption.id;
						else if (_mode == ButtonMode.Check && _relatedController.selectedPageId == pageOption.id)
							_relatedController.oppositePageId = pageOption.id;
					}
				}

			}
		}

		public ButtonMode mode
		{
			get
			{
				return _mode;
			}
			set
			{
				if (_mode != value)
				{
					if (value == ButtonMode.Common)
						this.selected = false;
					_mode = value;
				}
			}
		}

		public Controller relatedController
		{
			get
			{
				return _relatedController;
			}
			set
			{
				if (value != _relatedController)
				{
					_relatedController = value;
					pageOption.controller = value;
					pageOption.Clear();
				}
			}
		}

		public bool menuItemGrayed
		{
			get { return _menuItemGrayed; }
			set
			{
				if (_menuItemGrayed != value)
				{
					_menuItemGrayed = value;
					if (_titleObject != null)
						_titleObject.grayed = _menuItemGrayed;
					if (_iconObject != null)
						_iconObject.grayed = _menuItemGrayed;
				}
			}
		}

		public void fireClick(bool downEffect)
		{
			if (downEffect && _mode == ButtonMode.Common)
			{
				SetState(OVER);
				Timers.inst.Add(0.1f, 1, __SetState, DOWN);
				Timers.inst.Add(0.2f, 1, __SetState, UP);
			}
			__click();
		}

		private void __SetState(object val)
		{
			SetState(val.ToString());
		}

		private void SetState(string val)
		{
			if (_buttonController != null)
				_buttonController.selectedPage = val;
		}

		override public void HandleControllerChanged(Controller c)
		{
			base.HandleControllerChanged(c);

			if (_relatedController == c)
				this.selected = pageOption.id == c.selectedPageId;
		}

		override public void ConstructFromXML(XML cxml)
		{
			base.ConstructFromXML(cxml);

			XML xml = cxml.GetNode("Button");

			string str;
			str = xml.GetAttribute("mode");
			if (str != null)
				_mode = FieldTypes.ParseButtonMode(str);
			else
				_mode = ButtonMode.Common;

			str = xml.GetAttribute("sound");
			if (str != null)
				sound = UIPackage.GetItemAssetByURL(str) as AudioClip;

			str = xml.GetAttribute("volume");
			if (str != null)
				soundVolumeScale = float.Parse(str) / 100f;

			_buttonController = GetController("button");
			_titleObject = GetChild("title");
			_iconObject = GetChild("icon");

			if (_mode == ButtonMode.Common)
				SetState(UP);

			displayObject.onRollOver.Add(__rollover);
			displayObject.onRollOut.Add(__rollout);
			displayObject.onMouseDown.Add(__mousedown);
			displayObject.onRemovedFromStage.Add(__removedFromStage);
			displayObject.onClick.Add(__click);
		}

		override public void Setup_AfterAdd(XML cxml)
		{
			base.Setup_AfterAdd(cxml);

			XML xml = cxml.GetNode("Button");
			if (xml == null)
			{
				this.title = string.Empty;
				this.icon = null;
				return;
			}

			string str;

			this.title = xml.GetAttribute("title");
			this.icon = xml.GetAttribute("icon");
			str = xml.GetAttribute("selectedTitle");
			if (str != null)
				this.selectedTitle = str;
			str = xml.GetAttribute("selectedIcon");
			if (str != null)
				this.selectedIcon = str;

			str = xml.GetAttribute("titleColor");
			if (str != null)
				this.titleColor = ToolSet.ConvertFromHtmlColor(str);
			str = xml.GetAttribute("controller");
			if (str != null)
				_relatedController = parent.GetController(str);
			pageOption.id = xml.GetAttribute("page");
			this.selected = xml.GetAttributeBool("checked", false);

			str = xml.GetAttribute("sound");
			if (str != null)
				sound = UIPackage.GetItemAssetByURL(str) as AudioClip;

			str = xml.GetAttribute("volume");
			if (str != null)
				soundVolumeScale = float.Parse(str) / 100f;
		}

		private void __rollover()
		{
			if (_menuItemGrayed)
				return;

			_over = true;
			if (_down)
				return;

			SetState(_selected ? SELECTED_OVER : OVER);
		}

		private void __rollout()
		{
			_over = false;
			if (_down)
				return;

			SetState(_selected ? DOWN : UP);
		}

		private void __mousedown()
		{
			_down = true;
			Stage.inst.onMouseUp.Add(__mouseup);

			if (_mode == ButtonMode.Common)
				SetState(DOWN);

			if (linkedPopup != null)
			{
				if (linkedPopup is Window)
					((Window)linkedPopup).ToggleStatus();
				else
				{
					GRoot r = this.root;
					if (r != null)
						r.TogglePopup(linkedPopup, this);
				}
			}
		}

		private void __mouseup()
		{
			if (_down)
			{
				Stage.inst.onMouseUp.Remove(__mouseup);
				_down = false;

				if (_over)
					SetState(_selected ? SELECTED_OVER : OVER);
				else
					SetState(_selected ? DOWN : UP);
			}
		}

		private void __removedFromStage()
		{
			if (_over)
				__rollout();
		}

		private void __click()
		{
			if (sound != null)
				Stage.inst.PlayOneShotSound(sound, soundVolumeScale);

			if (!changeStateOnClick || _menuItemGrayed)
				return;

			if (_mode == ButtonMode.Check)
			{
				this.selected = !_selected;
				onChanged.Call();
			}
			else if (_mode == ButtonMode.Radio)
			{
				if (!_selected)
				{
					this.selected = true;
					onChanged.Call();
				}
			}
		}
	}
}
