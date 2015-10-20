using System;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
	public class GComponent : GObject
	{
		public Container rootContainer { get; private set; }
		public Container container { get; private set; }
		public ScrollPane scrollPane { get; private set; }

		public EventListener onScroll { get; private set; }

		internal List<GObject> _children;
		internal List<Controller> _controllers;
		internal List<Transition> _transitions;
		internal bool _buildingDisplayList;

		protected Margin _margin;
		protected bool _trackBounds;

		bool _boundsChanged;
		Rect _bounds;
		Vector2 _clipSoftness;
		int _AOTChildCount;
		bool _opaque;

		public GObject this[string gName]
		{
			get { return this.GetChild(gName); }
		}

		public GComponent()
		{
			_children = new List<GObject>();
			_controllers = new List<Controller>();
			_transitions = new List<Transition>();
			_margin = new Margin();
			this.opaque = true;

			onScroll = new EventListener(this, "onScroll");
		}

		override protected void CreateDisplayObject()
		{
			rootContainer = new Container();
			rootContainer.gOwner = this;
			rootContainer.onUpdate = onUpdate;
			container = rootContainer;

			displayObject = rootContainer;
		}

		override public void Dispose()
		{
			int numChildren = _children.Count;
			for (int i = numChildren - 1; i >= 0; --i)
				_children[i].Dispose();

			if (scrollPane != null)
				scrollPane.Dispose();

			base.Dispose();
		}

		public bool fairyBatching
		{
			get
			{
				return rootContainer.fairyBatching;
			}

			set
			{
				rootContainer.fairyBatching = value;
			}
		}

		public bool opaque
		{
			get { return _opaque; }
			set
			{
				if (_opaque != value)
				{
					_opaque = value;
					if (_opaque)
						UpdateOpaque();
					else
						rootContainer.hitArea = null;
				}
			}
		}

		public GObject AddChild(GObject child)
		{
			AddChildAt(child, _children.Count);
			return child;
		}

		virtual public GObject AddChildAt(GObject child, int index)
		{
			int numChildren = _children.Count;

			if (index >= 0 && index <= numChildren)
			{
				if (child.parent == this)
				{
					SetChildIndex(child, index);
				}
				else
				{
					child.RemoveFromParent();
					child.parent = this;

					int cnt = _children.Count;
					if (child.alwaysOnTop != 0)
					{
						_AOTChildCount++;
						index = GetInsertPosForAOTChild(child);
					}
					else if (_AOTChildCount > 0)
					{
						if (index > (cnt - _AOTChildCount))
							index = cnt - _AOTChildCount;
					}

					if (index == cnt)
						_children.Add(child);
					else
						_children.Insert(index, child);

					ChildStateChanged(child);
					SetBoundsChangedFlag();
				}
				return child;
			}
			else
			{
				throw new Exception("Invalid child index: " + index + ">" + numChildren);
			}
		}

		int GetInsertPosForAOTChild(GObject target)
		{
			int cnt = _children.Count;
			int i;
			for (i = 0; i < cnt; i++)
			{
				GObject child = _children[i];
				if (child == target)
					continue;

				if (target.alwaysOnTop < child.alwaysOnTop)
					break;
			}
			return i;
		}

		public GObject RemoveChild(GObject child)
		{
			return RemoveChild(child, false);
		}

		public GObject RemoveChild(GObject child, bool dispose)
		{
			int childIndex = _children.IndexOf(child);
			if (childIndex != -1)
			{
				RemoveChildAt(childIndex, dispose);
			}
			return child;
		}

		public GObject RemoveChildAt(int index)
		{
			return RemoveChildAt(index, false);
		}

		virtual public GObject RemoveChildAt(int index, bool dispose)
		{
			if (index >= 0 && index < numChildren)
			{
				GObject child = _children[index];
				child.parent = null;

				if (child.alwaysOnTop != 0)
					_AOTChildCount--;

				_children.RemoveAt(index);
				if (child.inContainer)
					container.RemoveChild(child.displayObject);

				if (dispose)
					child.Dispose();

				SetBoundsChangedFlag();

				return child;
			}
			else
				throw new Exception("Invalid child index: " + index + ">" + numChildren);
		}

		public void RemoveChildren()
		{
			RemoveChildren(0, -1, false);
		}

		public void RemoveChildren(int beginIndex, int endIndex, bool dispose)
		{
			if (endIndex < 0 || endIndex >= numChildren)
				endIndex = numChildren - 1;

			for (int i = beginIndex; i <= endIndex; ++i)
				RemoveChildAt(beginIndex, dispose);
		}

		public GObject GetChildAt(int index)
		{
			if (index >= 0 && index < numChildren)
				return _children[index];
			else
				throw new Exception("Invalid child index: " + index + ">" + numChildren);
		}

		public GObject GetChild(string name)
		{
			int cnt = _children.Count;
			for (int i = 0; i < cnt; ++i)
			{
				if (_children[i].name == name)
					return _children[i];
			}

			return null;
		}

		public GObject GetVisibleChild(string name)
		{
			int cnt = _children.Count;
			for (int i = 0; i < cnt; ++i)
			{
				GObject child = _children[i];
				if (child.finalVisible && child.name == name)
					return child;
			}

			return null;
		}

		public GObject GetChildInGroup(GGroup group, string name)
		{
			int cnt = _children.Count;
			for (int i = 0; i < cnt; ++i)
			{
				GObject child = _children[i];
				if (child.group == group && child.name == name)
					return child;
			}

			return null;
		}

		internal GObject GetChildById(string id)
		{
			int cnt = _children.Count;
			for (int i = 0; i < cnt; ++i)
			{
				if (_children[i].id == id)
					return _children[i];
			}

			return null;
		}

		public GObject[] GetChildren()
		{
			return _children.ToArray();
		}

		public int GetChildIndex(GObject child)
		{
			return _children.IndexOf(child);
		}

		public void SetChildIndex(GObject child, int index)
		{
			int oldIndex = _children.IndexOf(child);
			if (oldIndex == -1)
				throw new ArgumentException("Not a child of this container");

			if (child.alwaysOnTop != 0) //no effect
				return;

			int cnt = _children.Count;
			if (_AOTChildCount > 0)
			{
				if (index > (cnt - _AOTChildCount - 1))
					index = cnt - _AOTChildCount - 1;
			}

			_SetChildIndex(child, oldIndex, index);
		}

		void _SetChildIndex(GObject child, int oldIndex, int index)
		{
			int cnt = _children.Count;
			if (index > cnt)
				index = cnt;

			if (oldIndex == index)
				return;

			_children.RemoveAt(oldIndex);
			if (index >= cnt)
				_children.Add(child);
			else
				_children.Insert(index, child);

			if (child.inContainer)
			{
				int displayIndex = 0;
				for (int i = 0; i < index; i++)
				{
					GObject g = _children[i];
					if (g.inContainer)
						displayIndex++;
				}
				container.SetChildIndex(child.displayObject, displayIndex);

				SetBoundsChangedFlag();
			}
		}

		public void SwapChildren(GObject child1, GObject child2)
		{
			int index1 = _children.IndexOf(child1);
			int index2 = _children.IndexOf(child2);
			if (index1 == -1 || index2 == -1)
				throw new Exception("Not a child of this container");
			SwapChildrenAt(index1, index2);
		}

		public void SwapChildrenAt(int index1, int index2)
		{
			GObject child1 = _children[index1];
			GObject child2 = _children[index2];

			SetChildIndex(child1, index2);
			SetChildIndex(child2, index1);
		}

		public int numChildren
		{
			get { return _children.Count; }
		}

		public void AddController(Controller controller)
		{
			_controllers.Add(controller);
			controller.parent = this;
			ApplyController(controller);
		}

		public Controller GetController(string name)
		{
			int cnt = _controllers.Count;
			for (int i = 0; i < cnt; ++i)
			{
				Controller c = _controllers[i];
				if (c.name == name)
					return c;
			}

			return null;
		}

		public void RemoveController(Controller c)
		{
			int index = _controllers.IndexOf(c);
			if (index == -1)
				throw new Exception("controller not exists: " + c.name);

			c.parent = null;
			_controllers.RemoveAt(index);

			foreach (GObject child in _children)
				child.HandleControllerChanged(c);
		}

		public List<Controller> Controllers
		{
			get { return _controllers; }
		}

		public Transition GetTransition(string name)
		{
			int cnt = _transitions.Count;
			for (int i = 0; i < cnt; ++i)
			{
				Transition trans = _transitions[i];
				if (trans.name == name)
					return trans;
			}

			return null;
		}

		public Transition AddTransition(string name)
		{
			int cnt = _transitions.Count;
			for (int i = 0; i < cnt; ++i)
			{
				Transition trans = _transitions[i];
				if (trans.name == name)
					return trans;
			}
			Transition trans2 = new Transition(this);
			_transitions.Add(trans2);

			return trans2;
		}

		public void ChildStateChanged(GObject child)
		{
			if (_buildingDisplayList)
				return;

			if (child is GGroup)
			{
				foreach (GObject g in _children)
				{
					if (g.group == child)
						ChildStateChanged(g);
				}
				return;
			}

			if (child.displayObject == null)
				return;

			if (child.finalVisible)
			{
				if (child.displayObject.parent == null)
				{
					int index = 0;
					foreach (GObject g in _children)
					{
						if (g == child)
							break;

						if (g.displayObject != null && g.displayObject.parent != null)
							index++;
					}
					container.AddChildAt(child.displayObject, index);
				}
			}
			else
			{
				if (child.displayObject.parent != null)
					container.RemoveChild(child.displayObject);
			}
		}

		public void ApplyController(Controller c)
		{
			foreach (GObject child in _children)
				child.HandleControllerChanged(c);
		}

		public void ApplyAllControllers()
		{
			foreach (Controller controller in _controllers)
				ApplyController(controller);
		}

		public Vector2 clipSoftness
		{
			get { return _clipSoftness; }
			set
			{
				_clipSoftness = value;
				if (scrollPane != null)
					scrollPane.SetClipSoftness(value);
				else if (_clipSoftness.x > 0 && _clipSoftness.y > 0)
					rootContainer.clipSoftness = new Vector4(value.x, value.y, value.x, value.y);
				else
					rootContainer.clipSoftness = null;
			}
		}

		public bool IsChildInView(GObject child)
		{
			if (rootContainer.clipRect != null)
			{
				return child.x + child.width >= 0 && child.x <= this.width
					&& child.y + child.height >= 0 && child.y <= this.height;
			}
			else if (scrollPane != null)
			{
				return scrollPane.IsChildInView(child);
			}
			else
				return true;
		}

		public ScrollPane ScrollPane
		{
			get { return scrollPane; }
		}

		virtual protected void UpdateOpaque()
		{
			rootContainer.hitArea = new Rect(0, 0,
				this.actualWidth * GRoot.contentScaleFactor, this.actualHeight * GRoot.contentScaleFactor);
		}

		virtual protected void UpdateMask()
		{
			float w = this.width - (_margin.left + _margin.right);
			float h = this.height - (_margin.top + _margin.bottom);
			rootContainer.clipRect = new Rect(_margin.left * GRoot.contentScaleFactor, _margin.top * GRoot.contentScaleFactor,
				w * GRoot.contentScaleFactor, h * GRoot.contentScaleFactor);
		}

		protected void SetupOverflowAndScroll(OverflowType overflow, Margin scrollBarMargin,
			ScrollType scroll, ScrollBarDisplayType scrollBarDisplay, int flags)
		{
			if (overflow == OverflowType.Hidden)
			{
				container = new Container();
				rootContainer.AddChild(container);
				UpdateMask();
				container.SetXY(_margin.left * GRoot.contentScaleFactor, _margin.top * GRoot.contentScaleFactor);
			}
			else if (overflow == OverflowType.Scroll)
			{
				container = new Container();
				rootContainer.AddChild(container);

				scrollPane = new ScrollPane(this, scroll, _margin, scrollBarMargin, scrollBarDisplay, flags);
			}
			else if (_margin.left != 0 || _margin.top != 0)
			{
				container = new Container();
				rootContainer.AddChild(container);

				container.SetXY(_margin.left * GRoot.contentScaleFactor, _margin.top * GRoot.contentScaleFactor);
			}

			SetBoundsChangedFlag();
		}

		override protected void HandleSizeChanged()
		{
			if (scrollPane != null)
				scrollPane.SetSize(this.width, this.height);
			else if (rootContainer.clipRect != null)
				UpdateMask();

			if (_opaque)
				UpdateOpaque();

			rootContainer.SetScale(this.scaleX, this.scaleY);
		}

		override protected void HandleGrayedChanged()
		{
			Controller cc = GetController("grayed");
			if (cc != null)
				cc.selectedIndex = this.grayed ? 1 : 0;
			else
			{
				base.HandleGrayedChanged();

				foreach (GObject child in _children)
					child.grayed = grayed;
			}
		}

		public void SetBoundsChangedFlag()
		{
			if (scrollPane == null && !_trackBounds)
				return;

			_boundsChanged = true;
		}

		public void EnsureBoundsCorrect()
		{
			if (_boundsChanged)
				UpdateBounds();
		}

		virtual protected void UpdateBounds()
		{
			float ax, ay, aw, ah;
			if (_children.Count > 0)
			{
				ax = int.MaxValue;
				ay = int.MaxValue;
				float ar = int.MinValue, ab = int.MinValue;
				float tmp;

				foreach (GObject child in _children)
				{
					child.EnsureSizeCorrect();
				}

				foreach (GObject child in _children)
				{
					tmp = child.x;
					if (tmp < ax)
						ax = tmp;
					tmp = child.y;
					if (tmp < ay)
						ay = tmp;
					tmp = child.x + child.actualWidth;
					if (tmp > ar)
						ar = tmp;
					tmp = child.y + child.actualHeight;
					if (tmp > ab)
						ab = tmp;
				}
				aw = ar - ax;
				ah = ab - ay;
			}
			else
			{
				ax = 0;
				ay = 0;
				aw = 0;
				ah = 0;
			}
			if (ax != _bounds.x || ay != _bounds.y || aw != _bounds.width || ah != _bounds.height)
				SetBounds(ax, ay, aw, ah);
			else
				_boundsChanged = false;
		}

		public void SetBounds(float ax, float ay, float aw, float ah)
		{
			_boundsChanged = false;
			_bounds.x = ax;
			_bounds.y = ay;
			_bounds.width = aw;
			_bounds.height = ah;

			if (scrollPane != null)
				scrollPane.SetContentSize(Mathf.RoundToInt(_bounds.xMax), Mathf.RoundToInt(_bounds.yMax));
		}

		public Rect Bounds
		{
			get { return _bounds; }
		}

		public float ViewWidth
		{
			get
			{
				if (scrollPane != null)
					return scrollPane.viewWidth;
				else
					return this.width - _margin.left - _margin.right;
			}

			set
			{
				if (scrollPane != null)
					scrollPane.viewWidth = value;
				else
					this.width = value + _margin.left + _margin.right;
			}
		}

		public float ViewHeight
		{
			get
			{
				if (scrollPane != null)
					return scrollPane.viewHeight;
				else
					return this.height - _margin.top - _margin.bottom;
			}

			set
			{
				if (scrollPane != null)
					scrollPane.viewHeight = value;
				else
					this.height = value + _margin.top + _margin.bottom;
			}
		}

		virtual protected internal void FindObjectNear(ref float xValue, ref float yValue)
		{
		}

		internal void NotifyChildAOTChanged(GObject child, int oldValue, int newValue)
		{
			if (newValue == 0)
			{
				_AOTChildCount--;
				SetChildIndex(child, _children.Count);
			}
			else
			{
				if (oldValue == 0)
					_AOTChildCount++;

				int oldIndex = _children.IndexOf(child);
				int index = GetInsertPosForAOTChild(child);
				if (oldIndex < index)
					_SetChildIndex(child, oldIndex, index - 1);
				else
					_SetChildIndex(child, oldIndex, index);
			}
		}

		virtual protected void onUpdate()
		{
			if (_boundsChanged)
				UpdateBounds();
		}

		override public void ConstructFromResource(PackageItem pkgItem)
		{
			_packageItem = pkgItem;
			_packageItem.Load();

			ConstructFromXML(_packageItem.componentData);
		}

		virtual public void ConstructFromXML(XML xml)
		{
			string str;
			string[] arr;

			underConstruct = true;

			arr = xml.GetAttributeArray("size");
			sourceWidth = int.Parse(arr[0]);
			sourceHeight = int.Parse(arr[1]);
			initWidth = sourceWidth;
			initHeight = sourceHeight;

			OverflowType overflow;
			str = xml.GetAttribute("overflow");
			if (str != null)
				overflow = FieldTypes.ParseOverflowType(str);
			else
				overflow = OverflowType.Visible;

			ScrollType scroll;
			str = xml.GetAttribute("scroll");
			if (str != null)
				scroll = FieldTypes.ParseScrollType(str);
			else
				scroll = ScrollType.Vertical;

			ScrollBarDisplayType scrollBarDisplay;
			str = xml.GetAttribute("scrollBar");
			if (str != null)
				scrollBarDisplay = FieldTypes.ParseScrollBarDisplayType(str);
			else
				scrollBarDisplay = ScrollBarDisplayType.Default;

			int scrollBarFlags = xml.GetAttributeInt("scrollBarFlags");

			Margin scrollBarMargin = new Margin();
			str = xml.GetAttribute("scrollBarMargin");
			if (str != null)
				scrollBarMargin.Parse(str);

			str = xml.GetAttribute("margin");
			if (str != null)
				_margin.Parse(str);

			SetSize(sourceWidth, sourceHeight);

			SetupOverflowAndScroll(overflow, scrollBarMargin, scroll, scrollBarDisplay, scrollBarFlags);

			arr = xml.GetAttributeArray("clipSoftness");
			if (arr != null)
				this.clipSoftness = new Vector2(int.Parse(arr[0]), int.Parse(arr[1]));

			_buildingDisplayList = true;

			XMLList col = xml.Elements("controller");
			Controller controller;
			foreach (XML cxml in col)
			{
				controller = new Controller();
				_controllers.Add(controller);
				controller.parent = this;
				controller.Setup(cxml);
			}

			XML listNode = xml.GetNode("displayList");
			if (listNode != null)
			{
				col = listNode.Elements();
				GObject u;
				foreach (XML cxml in col)
				{
					u = ConstructChild(cxml);
					if (u == null)
						continue;

					u.underConstruct = true;
					u.constructingData = cxml;
					u.Setup_BeforeAdd(cxml);
					AddChild(u);
				}
			}
			this.relations.Setup(xml);

			int cnt = _children.Count;
			for (int i = 0; i < cnt; i++)
			{
				GObject u = _children[i];
				u.relations.Setup(u.constructingData);
			}

			for (int i = 0; i < cnt; i++)
			{
				GObject u = _children[i];
				u.Setup_AfterAdd(u.constructingData);
				u.underConstruct = false;
				u.constructingData = null;
			}

			XMLList transCol = xml.Elements("transition");
			foreach (XML cxml in transCol)
			{
				Transition trans = new Transition(this);
				trans.Setup(cxml);
				_transitions.Add(trans);
			}

			ApplyAllControllers();

			_buildingDisplayList = false;
			underConstruct = false;

			//build real display list
			foreach (GObject child in _children)
			{
				if (child.displayObject != null && child.finalVisible)
					container.AddChild(child.displayObject);
			}
		}

		private GObject ConstructChild(XML xml)
		{
			string pkgId = xml.GetAttribute("pkg");
			UIPackage thisPkg = _packageItem.owner;
			UIPackage pkg;
			if (pkgId != null && pkgId != thisPkg.id)
			{
				pkg = UIPackage.GetById(pkgId);
				if (pkg == null)
					return null;
			}
			else
				pkg = thisPkg;

			string src = xml.GetAttribute("src");
			if (src != null)
			{
				PackageItem pi = pkg.GetItem(src);
				if (pi == null)
					return null;

				GObject g = pkg.CreateObject(pi, null);
				return g;
			}
			else
			{
				GObject g;
				if (xml.name == "text" && xml.GetAttributeBool("input", false))
					g = new GTextInput();
				else
					g = UIObjectFactory.NewObject(xml.name);
				return g;
			}
		}
	}
}
