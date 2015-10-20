using UnityEngine;
using FairyGUI.Utils;
using DG.Tweening;

namespace FairyGUI
{
	public class GObject : EventDispatcher
	{
		public string id { get; internal set; }
		public string name;
		public object data;

		public int sourceWidth { get; internal set; }
		public int sourceHeight { get; internal set; }
		public int initWidth { get; internal set; }
		public int initHeight { get; internal set; }

		public Relations relations { get; private set; }
		public GGroup group;
		public Rect? dragBounds;
		public GComponent parent { get; internal set; }
		public DisplayObject displayObject { get; protected set; }

		public GearDisplay gearDisplay { get; private set; }
		public GearXY gearXY { get; private set; }
		public GearSize gearSize { get; private set; }
		public GearLook gearLook { get; private set; }

		public EventListener onClick { get; private set; }
		public EventListener onRightClick { get; private set; }
		public EventListener onMouseDown { get; private set; }
		public EventListener onMouseUp { get; private set; }
		public EventListener onRollOver { get; private set; }
		public EventListener onRollOut { get; private set; }
		public EventListener onAddedToStage { get; private set; }
		public EventListener onRemovedFromStage { get; private set; }
		public EventListener onKeyDown { get; private set; }
		public EventListener onClickLink { get; private set; }

		public EventListener onXYChanged { get; private set; }
		public EventListener onSizeChanged { get; private set; }
		public EventListener onDragStart { get; private set; }
		public EventListener onDragEnd { get; private set; }

		float _x;
		float _y;
		float _width;
		float _height;
		float _pivotX;
		float _pivotY;
		float _alpha;
		float _rotation;
		bool _visible;
		int _internalVisible;
		bool _touchable;
		bool _grayed;
		bool _draggable;
		float _scaleX;
		float _scaleY;
		int _alwaysOnTop;
		bool _focusable;
		string _tooltips;
		float _pivotOffsetX;
		float _pivotOffsetY;

		internal PackageItem _packageItem;
		internal protected bool underConstruct;
		internal XML constructingData;
		internal float _rawWidth;
		internal float _rawHeight;
		internal bool _gearLocked;

		internal static uint _gInstanceCounter;

		public GObject()
		{
			_width = 0;
			_height = 0;
			_alpha = 1;
			_rotation = 0;
			_visible = true;
			_touchable = true;
			_scaleX = 1;
			_scaleY = 1;
			_internalVisible = 1;
			id = "_n" + _gInstanceCounter++;
			name = string.Empty;

			CreateDisplayObject();

			relations = new Relations(this);

			gearDisplay = new GearDisplay(this);
			gearXY = new GearXY(this);
			gearSize = new GearSize(this);
			gearLook = new GearLook(this);

			onClick = new EventListener(this, "onClick");
			onRightClick = new EventListener(this, "onRightClick");
			onMouseDown = new EventListener(this, "onMouseDown");
			onMouseUp = new EventListener(this, "onMouseUp");
			onRollOver = new EventListener(this, "onRollOver");
			onRollOut = new EventListener(this, "onRollOut");
			onAddedToStage = new EventListener(this, "onAddedToStage");
			onRemovedFromStage = new EventListener(this, "onRemovedFromStage");
			onKeyDown = new EventListener(this, "onKeyDown");
			onClickLink = new EventListener(this, "onClickLink");

			onXYChanged = new EventListener(this, "onXYChanged");
			onSizeChanged = new EventListener(this, "onSizeChanged");
			onDragStart = new EventListener(this, "onDragStart");
			onDragEnd = new EventListener(this, "onDragEnd");
		}

		public float x
		{
			get { return _x; }
			set
			{
				SetXY(value, _y);
			}
		}

		public float y
		{
			get { return _y; }
			set
			{
				SetXY(_x, value);
			}
		}

		public Vector2 xy
		{
			get { return new Vector2(_x, _y); }
			set { SetXY(value.x, value.y); }
		}

		public void SetXY(float xv, float yv)
		{
			if (_x != xv || _y != yv)
			{
				float dx = xv - _x;
				float dy = yv - _y;
				_x = xv;
				_y = yv;

				HandleXYChanged();

				if (this is GGroup)
					((GGroup)this).MoveChildren(dx, dy);

				if (gearXY.controller != null)
					gearXY.UpdateState();

				if (parent != null)
				{
					parent.SetBoundsChangedFlag();
					onXYChanged.Call();
				}
			}
		}

		public void Center()
		{
			Center(false);
		}

		public void Center(bool restraint)
		{
			GComponent r;
			if (parent != null)
				r = parent;
			else
			{
				r = this.root;
				if (r == null)
					r = GRoot.inst;
			}

			this.SetXY((int)((r.width - this.width) / 2), (int)((r.height - this.height) / 2));
			if (restraint)
			{
				this.AddRelation(r, RelationType.Center_Center);
				this.AddRelation(r, RelationType.Middle_Middle);
			}
		}

		public float width
		{
			get
			{
				EnsureSizeCorrect();
				if (relations.sizeDirty)
					relations.EnsureRelationsSizeCorrect();
				return _width;
			}
			set
			{
				SetSize(value, _rawHeight);
			}
		}

		public float height
		{
			get
			{
				EnsureSizeCorrect();
				if (relations.sizeDirty)
					relations.EnsureRelationsSizeCorrect();
				return _height;
			}
			set
			{
				SetSize(_rawWidth, value);
			}
		}

		public Vector2 size
		{
			get { return new Vector2(width, height); }
			set { SetSize(value.x, value.y); }
		}

		public float actualWidth
		{
			get { return this.width * _scaleX; }
		}

		public float actualHeight
		{
			get { return this.height * _scaleY; }
		}

		virtual public void EnsureSizeCorrect()
		{
		}

		public void SetSize(float wv, float hv)
		{
			SetSize(wv, hv, false);
		}

		public void SetSize(float wv, float hv, bool ignorePivot)
		{
			if (_rawWidth != wv || _rawHeight != hv)
			{
				_rawWidth = wv;
				_rawHeight = hv;
				if (wv < 0)
					wv = 0;
				if (hv < 0)
					hv = 0;
				float dWidth = wv - _width;
				float dHeight = hv - _height;
				_width = wv;
				_height = hv;

				if ((_pivotX != 0 || _pivotY != 0) && sourceWidth != 0 && sourceHeight != 0)
				{
					if (!ignorePivot)
						this.SetXY(this.x - _pivotX * dWidth / sourceWidth,
							this.y - _pivotY * dHeight / sourceHeight);
					ApplyPivot();
				}

				HandleSizeChanged();

				if (gearSize.controller != null)
					gearSize.UpdateState();

				if (parent != null)
				{
					relations.OnOwnerSizeChanged(dWidth, dHeight);
					parent.SetBoundsChangedFlag();
				}

				onSizeChanged.Call();
			}
		}

		public float scaleX
		{
			get { return _scaleX; }
			set
			{
				SetScale(value, _scaleY);
			}
		}

		public float scaleY
		{
			get { return _scaleY; }
			set
			{
				SetScale(_scaleX, value);
			}
		}

		public Vector2 scale
		{
			get { return new Vector2(_scaleX, _scaleY); }
			set { SetScale(value.x, value.y); }
		}

		public void SetScale(float wv, float hv)
		{
			if (_scaleX != wv || _scaleY != hv)
			{
				_scaleX = wv;
				_scaleY = hv;
				ApplyPivot();
				HandleSizeChanged();

				if (gearSize.controller != null)
					gearSize.UpdateState();
			}
		}

		public float pivotX
		{
			get { return _pivotX; }
			set
			{
				SetPivot(value, _pivotY);
			}
		}

		public float pivotY
		{
			get { return _pivotY; }
			set
			{
				SetPivot(_pivotX, value);
			}
		}

		public Vector2 pivot
		{
			get { return new Vector2(_pivotX, _pivotY); }
			set { SetPivot(value.x, value.y); }
		}

		public void SetPivot(float xv, float yv)
		{
			if (_pivotX != xv || _pivotY != yv)
			{
				_pivotX = xv;
				_pivotY = yv;

				ApplyPivot();
			}
		}

		void ApplyPivot()
		{
			float ox = _pivotOffsetX;
			float oy = _pivotOffsetY;
			if (_pivotX != 0 || _pivotY != 0)
			{
				if (_rotation != 0 || _scaleX != 1 || _scaleY != 1)
				{
					float rotInRad = _rotation * Mathf.Deg2Rad;
					float cos = Mathf.Cos(rotInRad);
					float sin = Mathf.Sin(rotInRad);
					float a = _scaleX * cos;
					float b = _scaleX * sin;
					float c = _scaleY * -sin;
					float d = _scaleY * cos;
					float sx = sourceWidth != 0 ? (_width / sourceWidth) : 1;
					float sy = sourceHeight != 0 ? (_height / sourceHeight) : 1;
					float px = _pivotX * sx;
					float py = _pivotY * sy;
					_pivotOffsetX = px - (a * px + c * py);
					_pivotOffsetY = py - (d * py + b * px);
				}
				else
				{
					_pivotOffsetX = 0;
					_pivotOffsetY = 0;
				}
			}
			else
			{
				_pivotOffsetX = 0;
				_pivotOffsetY = 0;
			}
			if (ox != _pivotOffsetX || oy != _pivotOffsetY)
				HandleXYChanged();
		}

		public bool touchable
		{

			get
			{
				return _touchable;
			}
			set
			{
				_touchable = value;
				if (displayObject != null)
					displayObject.touchable = _touchable;
			}
		}

		public bool grayed
		{
			get
			{
				return _grayed;
			}
			set
			{
				if (_grayed != value)
				{
					_grayed = value;
					HandleGrayedChanged();

					if (gearLook.controller != null)
						gearLook.UpdateState();
				}
			}
		}

		public bool enabled
		{
			get
			{
				return !_grayed && _touchable;
			}
			set
			{
				this.grayed = !value;
				this.touchable = value;
			}
		}

		public float rotation
		{
			get
			{
				return _rotation;
			}
			set
			{
				_rotation = value;
				ApplyPivot();
				if (displayObject != null)
					displayObject.rotation = _rotation;

				if (gearLook.controller != null)
					gearLook.UpdateState();
			}
		}

		public float alpha
		{

			get
			{
				return _alpha;
			}

			set
			{
				_alpha = value;
				if (displayObject != null)
					displayObject.alpha = _alpha;

				if (gearLook.controller != null)
					gearLook.UpdateState();
			}
		}

		public bool visible
		{
			get
			{
				return _visible;
			}

			set
			{
				if (_visible != value)
				{
					_visible = value;
					if (displayObject != null)
						displayObject.visible = _visible;
					if (parent != null)
						parent.ChildStateChanged(this);
				}
			}
		}

		internal int internalVisible
		{
			get { return _internalVisible; }
			set
			{
				if (value < 0)
					value = 0;
				bool oldValue = _internalVisible > 0;
				bool newValue = value > 0;
				_internalVisible = value;
				if (oldValue != newValue)
				{
					if (parent != null)
						parent.ChildStateChanged(this);
				}
			}
		}

		internal bool finalVisible
		{
			get
			{
				return _visible && _internalVisible > 0 && (group == null || group.finalVisible);
			}
		}

		public int alwaysOnTop
		{
			get { return _alwaysOnTop; }
			set
			{
				if (value < 0)
					value = 0;
				if (_alwaysOnTop != value)
				{
					int old = _alwaysOnTop;
					_alwaysOnTop = value;
					if (parent != null)
						parent.NotifyChildAOTChanged(this, old, _alwaysOnTop);
				}
			}
		}

		public bool focusable
		{
			get { return _focusable; }
			set { _focusable = value; }
		}

		public bool focused
		{
			get
			{
				GRoot r = this.root;
				if (r != null)
					return r.focus == this;
				else
					return false;
			}
		}

		public void RequestFocus()
		{
			GRoot r = this.root;
			if (r != null)
			{
				GObject p = this;
				while (p != null && !p._focusable)
					p = p.parent;
				if (p != null)
					r.focus = p;
			}
		}

		public string tooltips
		{
			get { return _tooltips; }
			set
			{
				if (!string.IsNullOrEmpty(_tooltips))
				{
					this.onRollOver.Remove(__rollOver);
					this.onRollOut.Remove(__rollOut);
				}

				_tooltips = value;
				if (!string.IsNullOrEmpty(_tooltips))
				{
					this.onRollOver.Add(__rollOver);
					this.onRollOut.Add(__rollOut);
				}
			}
		}

		private void __rollOver()
		{
			GRoot r = this.root;
			if (r != null)
				r.ShowTooltips(tooltips);
		}

		private void __rollOut()
		{
			GRoot r = this.root;
			if (r != null)
				r.HideTooltips();
		}

		public bool inContainer
		{
			get
			{
				return displayObject != null && displayObject.parent != null;
			}
		}

		public bool onStage
		{
			get
			{
				return displayObject != null && displayObject.stage != null;
			}
		}

		public string resourceURL
		{
			get
			{
				if (_packageItem != null)
					return UIPackage.URL_PREFIX + _packageItem.owner.id + _packageItem.id;
				else
					return null;
			}
		}

		public void InvalidateBatchingState()
		{
			if (displayObject != null)
				displayObject.InvalidateBatchingState();
		}

		virtual public void HandleControllerChanged(Controller c)
		{
			if (gearDisplay.controller == c)
				gearDisplay.Apply();
			if (gearXY.controller == c)
				gearXY.Apply();
			if (gearSize.controller == c)
				gearSize.Apply();
			if (gearLook.controller == c)
				gearLook.Apply();
		}

		public void AddRelation(GObject target, RelationType relationType)
		{
			AddRelation(target, relationType, false);
		}

		public void AddRelation(GObject target, RelationType relationType, bool usePercent)
		{
			relations.Add(target, relationType, usePercent);
		}

		public void RemoveRelation(GObject target, RelationType relationType)
		{
			relations.Remove(target, relationType);
		}

		public void RemoveFromParent()
		{
			if (parent != null)
				parent.RemoveChild(this);
		}

		public GRoot root
		{
			get
			{
				GObject p = parent;
				while (p != null)
				{
					if (p is GRoot)
						return (GRoot)p;
					p = p.parent;
				}
				return null;
			}
		}

		virtual public string text
		{
			get { return null; }
			set { }
		}

		public bool draggable
		{
			get { return _draggable; }
			set
			{
				if (_draggable != value)
				{
					_draggable = value;
					InitDrag();
				}
			}
		}

		public void StartDrag()
		{
			StartDrag(null, -1);
		}

		public void StartDrag(Rect? bounds)
		{
			StartDrag(bounds, -1);
		}

		public void StartDrag(Rect? bounds, int touchId)
		{
			if (displayObject.stage == null)
				return;

			DragBegin(touchId);
		}

		public void StopDrag()
		{
			DragEnd();
		}

		public bool dragging
		{
			get
			{
				return sDragging == this;
			}
		}

		public Vector2 LocalToGlobal(Vector2 pt)
		{
			pt.x *= GRoot.contentScaleFactor;
			pt.y *= GRoot.contentScaleFactor;
			pt = displayObject.LocalToGlobal(pt);
			pt.x /= GRoot.contentScaleFactor;
			pt.y /= GRoot.contentScaleFactor;
			return pt;
		}

		public Vector2 GlobalToLocal(Vector2 pt)
		{
			pt.x *= GRoot.contentScaleFactor;
			pt.y *= GRoot.contentScaleFactor;
			pt = displayObject.GlobalToLocal(pt);
			pt.x /= GRoot.contentScaleFactor;
			pt.y /= GRoot.contentScaleFactor;
			return pt;
		}

		public Rect GetGlobalRect()
		{
			Rect ret = new Rect();
			Vector2 v = this.LocalToGlobal(new Vector2(0, 0));
			ret.xMin = v.x;
			ret.yMin = v.y;
			v = this.LocalToGlobal(this.size);
			ret.xMax = v.x;
			ret.yMax = v.y;
			return ret;
		}

		virtual public void Dispose()
		{
			if (displayObject != null)
			{
				displayObject.Dispose();
				displayObject = null;
			}
			RemoveEventListeners();
			relations.Dispose();
		}

		public GComponent asCom
		{
			get { return this as GComponent; }
		}

		public GButton asButton
		{
			get { return this as GButton; }
		}

		public GLabel asLabel
		{
			get { return this as GLabel; }
		}

		public GProgressBar asProgress
		{
			get { return this as GProgressBar; }
		}

		public GSlider asSlider
		{
			get { return this as GSlider; }
		}

		public GComboBox asComboBox
		{
			get { return this as GComboBox; }
		}

		public GTextField asTextField
		{
			get { return this as GTextField; }
		}

		public GRichTextField asRichTextField
		{
			get { return this as GRichTextField; }
		}

		public GTextInput asTextInput
		{
			get { return this as GTextInput; }
		}

		public GLoader asLoader
		{
			get { return this as GLoader; }
		}

		public GList asList
		{
			get { return this as GList; }
		}

		public GGraph asGraph
		{
			get { return this as GGraph; }
		}

		public GGroup asGroup
		{
			get { return this as GGroup; }
		}

		public GMovieClip asMovieClip
		{
			get { return this as GMovieClip; }
		}

		virtual protected void CreateDisplayObject()
		{

		}

		virtual protected void HandleXYChanged()
		{
			if (displayObject != null)
			{
				displayObject.SetXY((_x + _pivotOffsetX) * GRoot.contentScaleFactor, (_y + _pivotOffsetY) * GRoot.contentScaleFactor);
			}
		}

		virtual protected void HandleSizeChanged()
		{
		}

		virtual protected void HandleGrayedChanged()
		{
			if (displayObject != null)
				displayObject.SetGrayed(_grayed);
		}

		virtual public void ConstructFromResource(PackageItem pkgItem)
		{
			_packageItem = pkgItem;
		}

		virtual public void Setup_BeforeAdd(XML xml)
		{
			string str;
			string[] arr;

			id = xml.GetAttribute("id");
			name = xml.GetAttribute("name");

			arr = xml.GetAttributeArray("xy");
			if (arr != null)
				this.SetXY(int.Parse(arr[0]), int.Parse(arr[1]));

			arr = xml.GetAttributeArray("size");
			if (arr != null)
			{
				initWidth = int.Parse(arr[0]);
				initHeight = int.Parse(arr[1]);
				SetSize(initWidth, initHeight);
			}

			arr = xml.GetAttributeArray("scale");
			if (arr != null)
				SetScale(float.Parse(arr[0]), float.Parse(arr[1]));

			str = xml.GetAttribute("rotation");
			if (str != null)
				this.rotation = int.Parse(str);

			arr = xml.GetAttributeArray("pivot");
			if (arr != null)
				this.SetPivot(int.Parse(arr[0]), int.Parse(arr[1]));

			str = xml.GetAttribute("alpha");
			if (str != null)
				this.alpha = float.Parse(str);

			this.touchable = xml.GetAttributeBool("touchable", true);
			this.visible = xml.GetAttributeBool("visible", true);
			this.grayed = xml.GetAttributeBool("grayed", false);
			str = xml.GetAttribute("tooltips");
			if (str != null)
				this.tooltips = str;
		}

		virtual public void Setup_AfterAdd(XML xml)
		{
			XML cxml = null;
			string str;

			str = xml.GetAttribute("group");
			if (str != null)
				group = parent.GetChildById(str) as GGroup;

			cxml = xml.GetNode("gearDisplay");
			if (cxml != null)
				gearDisplay.Setup(cxml);

			cxml = xml.GetNode("gearXY");
			if (cxml != null)
				gearXY.Setup(cxml);

			cxml = xml.GetNode("gearSize");
			if (cxml != null)
				gearSize.Setup(cxml);

			cxml = xml.GetNode("gearLook");
			if (cxml != null)
				gearLook.Setup(cxml);
		}

		//drag support
		int _dragTouchId;

		static GObject sDragging;
		static Vector2 sGlobalDragStart = new Vector2();
		static Rect sGlobalRect = new Rect();

		private void InitDrag()
		{
			if (_draggable)
				onMouseDown.Add(__mouseDown);
			else
				onMouseDown.Remove(__mouseDown);
		}

		private void DragBegin(int touchId)
		{
			if (sDragging != null)
				sDragging.StopDrag();

			_dragTouchId = touchId;
			Vector2 pos = Stage.inst.GetTouchPosition(touchId);
			sGlobalDragStart.x = pos.x / GRoot.contentScaleFactor;
			sGlobalDragStart.y = pos.y / GRoot.contentScaleFactor;
			sGlobalRect = this.GetGlobalRect();

			sDragging = this;
			Stage.inst.onMouseUp.Add(__mouseUp2);
			Stage.inst.onMouseMove.Add(__mouseMove2);
		}

		private void DragEnd()
		{
			if (sDragging == this)
			{
				Stage.inst.onMouseUp.Remove(__mouseUp2);
				Stage.inst.onMouseMove.Remove(__mouseMove2);
				sDragging = null;
			}
		}

		private void Reset()
		{
			Stage.inst.onMouseUp.Remove(__mouseUp);
			Stage.inst.onMouseMove.Remove(__mouseMove);
		}

		private void __mouseDown(EventContext context)
		{
			InputEvent evt = context.inputEvent;
			_dragTouchId = evt.touchId;

			Stage.inst.onMouseUp.Add(__mouseUp);
			Stage.inst.onMouseMove.Add(__mouseMove);
		}

		private void __mouseUp(EventContext context)
		{
			if (_dragTouchId != context.inputEvent.touchId)
				return;

			Reset();
		}

		private void __mouseMove(EventContext context)
		{
			InputEvent evt = context.inputEvent;
			if (_dragTouchId != evt.touchId)
				return;

			Reset();

			if (!onDragStart.Call(_dragTouchId))
				DragBegin(evt.touchId);
		}

		private void __mouseUp2(EventContext context)
		{
			InputEvent evt = context.inputEvent;
			if (_dragTouchId != -1 && _dragTouchId != evt.touchId)
				return;

			if (sDragging == this)
			{
				StopDrag();
				onDragEnd.Call();
			}
		}

		private void __mouseMove2(EventContext context)
		{
			InputEvent evt = context.inputEvent;
			if (_dragTouchId != -1 && _dragTouchId != evt.touchId || this.parent == null)
				return;

			float xx = evt.x / GRoot.contentScaleFactor - sGlobalDragStart.x + sGlobalRect.x;
			float yy = evt.y / GRoot.contentScaleFactor - sGlobalDragStart.y + sGlobalRect.y;

			if (dragBounds != null)
			{
				Rect rect = (Rect)dragBounds;
				if (xx < rect.x)
					xx = rect.x;
				else if (xx + sGlobalRect.width > rect.xMax)
				{
					xx = rect.xMax - sGlobalRect.width;
					if (xx < rect.x)
						xx = rect.x;
				}

				if (yy < rect.y)
					yy = rect.y;
				else if (yy + sGlobalRect.height > rect.yMax)
				{
					yy = rect.yMax - sGlobalRect.height;
					if (yy < rect.y)
						yy = rect.y;
				}
			}

			Vector2 pt = this.parent.GlobalToLocal(new Vector2(xx, yy));
			this.SetXY(Mathf.RoundToInt(pt.x), Mathf.RoundToInt(pt.y));
		}

		//----DOTween Support---
		public Tweener TweenMove(Vector2 endValue, float duration, bool snapping)
		{
			return DOTween.To(() => this.xy, x => this.xy = x, endValue, duration)
				.SetOptions(snapping)
				.SetTarget(this);
		}

		public Tweener TweenMoveX(float endValue, float duration, bool snapping)
		{
			return DOTween.To(() => this.x, x => this.x = x, endValue, duration)
				.SetOptions(snapping)
				.SetTarget(this);
		}

		public Tweener TweenMoveY(float endValue, float duration, bool snapping)
		{
			return DOTween.To(() => this.y, x => this.y = x, endValue, duration)
				.SetOptions(snapping)
				.SetTarget(this);
		}

		public Tweener TweenMove(Vector2 endValue, float duration)
		{
			return DOTween.To(() => this.xy, x => this.xy = x, endValue, duration)
				.SetTarget(this);
		}

		public Tweener TweenMoveX(float endValue, float duration)
		{
			return DOTween.To(() => this.x, x => this.x = x, endValue, duration)
				.SetTarget(this);
		}

		public Tweener TweenMoveY(float endValue, float duration)
		{
			return DOTween.To(() => this.y, x => this.y = x, endValue, duration)
				.SetTarget(this);
		}

		public Tweener TweenScale(Vector2 endValue, float duration)
		{
			return DOTween.To(() => this.scale, x => this.scale = x, endValue, duration)
				.SetTarget(this);
		}

		public Tweener TweenScaleX(float endValue, float duration)
		{
			return DOTween.To(() => this.scaleX, x => this.scaleX = x, endValue, duration)
				.SetTarget(this);
		}

		public Tweener TweenScaleY(float endValue, float duration)
		{
			return DOTween.To(() => this.scaleY, x => this.scaleY = x, endValue, duration)
				.SetTarget(this);
		}

		public Tweener TweenResize(Vector2 endValue, float duration, bool snapping)
		{
			return DOTween.To(() => this.size, x => this.size = x, endValue, duration)
				.SetOptions(snapping)
				.SetTarget(this);
		}

		public Tweener TweenResize(Vector2 endValue, float duration)
		{
			return DOTween.To(() => this.size, x => this.size = x, endValue, duration)
				.SetTarget(this);
		}

		public Tweener TweenFade(float endValue, float duration)
		{
			return DOTween.To(() => this.alpha, x => this.alpha = x, endValue, duration)
				.SetTarget(this);
		}

		public Tweener TweenRotate(float endValue, float duration)
		{
			return DOTween.To(() => this.rotation, x => this.rotation = x, endValue, duration)
				.SetTarget(this);
		}
	}
}
