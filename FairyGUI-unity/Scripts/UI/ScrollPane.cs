using System;
using UnityEngine;
using DG.Tweening;

namespace FairyGUI
{
	public class ScrollPane
	{
		float _maskWidth;
		float _maskHeight;
		float _contentWidth;
		float _contentHeight;
		ScrollType _scrollType;
		float _scrollSpeed;
		float _mouseWheelSpeed;
		Margin _margin;
		Margin _scrollBarMargin;
		bool _bouncebackEffect;
		bool _touchEffect;
		bool _scrollBarDisplayAuto;
		bool _vScrollNone;
		bool _hScrollNone;

		bool _displayOnLeft;
		bool _snapToItem;
		bool _displayInDemand;
		bool _mouseWheelEnabled;
		Vector2? _clipSoftness;

		float _yPerc;
		float _xPerc;
		bool _vScroll;
		bool _hScroll;

		float _time1, _time2;
		float _y1, _y2;
		float _xOverlap, _yOverlap;
		float _x1, _x2;
		float _xOffset, _yOffset;
		bool _isMouseMoved;
		Vector2 _holdAreaPoint;
		bool _isHoldAreaDone;
		int _holdArea;
		bool _aniFlag;
		bool _scrollBarVisible;
		int _touchId;

		ThrowTween _throwTween;
		Tweener _tweener;

		GComponent _owner;
		Container _container;
		Container _maskHolder;
		Container _maskContentHolder;
		GScrollBar _hzScrollBar;
		GScrollBar _vtScrollBar;

		public ScrollPane(GComponent owner,
									ScrollType scrollType,
									Margin margin,
									Margin scrollBarMargin,
									ScrollBarDisplayType scrollBarDisplay,
									int flags)
		{
			_throwTween = new ThrowTween();

			_owner = owner;
			_container = _owner.rootContainer;

			_maskHolder = new Container();
			_container.AddChild(_maskHolder);

			_maskContentHolder = _owner.container;
			_maskContentHolder.x = 0;
			_maskContentHolder.y = 0;
			_maskHolder.AddChild(_maskContentHolder);

			if (Stage.touchScreen)
				_holdArea = 20;
			else
				_holdArea = 5;
			_holdAreaPoint = new Vector2();
			_margin = margin;
			_scrollBarMargin = scrollBarMargin;
			_bouncebackEffect = UIConfig.defaultScrollBounceEffect;
			_touchEffect = UIConfig.defaultScrollTouchEffect;
			_xPerc = 0;
			_yPerc = 0;
			_aniFlag = true;
			_scrollBarVisible = true;
			_scrollSpeed = UIConfig.defaultScrollSpeed;
			_mouseWheelSpeed = _scrollSpeed * 2;
			_displayOnLeft = (flags & 1) != 0;
			_snapToItem = (flags & 2) != 0;
			_displayInDemand = (flags & 4) != 0;
			_scrollType = scrollType;
			_mouseWheelEnabled = true;

			if (scrollBarDisplay == ScrollBarDisplayType.Default)
				scrollBarDisplay = UIConfig.defaultScrollBarDisplay;

			if (scrollBarDisplay != ScrollBarDisplayType.Hidden)
			{
				if (_scrollType == ScrollType.Both || _scrollType == ScrollType.Vertical)
				{
					if (UIConfig.verticalScrollBar != null)
					{
						_vtScrollBar = UIPackage.CreateObjectFromURL(UIConfig.verticalScrollBar) as GScrollBar;
						if (_vtScrollBar == null)
							Debug.LogError("FairyGUI: cannot create scrollbar from " + UIConfig.verticalScrollBar);
						else
						{
							_vtScrollBar.SetScrollPane(this, true);
							_container.AddChild(_vtScrollBar.displayObject);
						}
					}
				}
				if (_scrollType == ScrollType.Both || _scrollType == ScrollType.Horizontal)
				{
					if (UIConfig.horizontalScrollBar != null)
					{
						_hzScrollBar = UIPackage.CreateObjectFromURL(UIConfig.horizontalScrollBar) as GScrollBar;
						if (_hzScrollBar == null)
							Debug.LogError("FairyGUI: cannot create scrollbar from " + UIConfig.horizontalScrollBar);
						else
						{
							_hzScrollBar.SetScrollPane(this, false);
							_container.AddChild(_hzScrollBar.displayObject);
						}
					}
				}

				_scrollBarDisplayAuto = scrollBarDisplay == ScrollBarDisplayType.Auto;
				if (_scrollBarDisplayAuto)
				{
					if (_vtScrollBar != null)
						_vtScrollBar.displayObject.visible = false;
					if (_hzScrollBar != null)
						_hzScrollBar.displayObject.visible = false;
					_scrollBarVisible = false;

					_container.onRollOver.Add(__rollOver);
					_container.onRollOut.Add(__rollOut);
				}
			}
			else
				_mouseWheelEnabled = false;

			if (_displayOnLeft && _vtScrollBar != null)
				_maskHolder.x = Mathf.FloorToInt((_margin.left + _vtScrollBar.width) * GRoot.contentScaleFactor);
			else
				_maskHolder.x = Mathf.FloorToInt(_margin.left * GRoot.contentScaleFactor);
			_maskHolder.y = Mathf.FloorToInt(_margin.top * GRoot.contentScaleFactor);

			SetSize(owner.width, owner.height);
			SetContentSize(owner.Bounds.width, owner.Bounds.height);

			_container.onMouseWheel.Add(__mouseWheel);
			_container.onMouseDown.Add(__mouseDown);
		}

		public void Dispose()
		{
			_container.onMouseWheel.Remove(__mouseWheel);
			_container.onMouseDown.Remove(__mouseDown);
			_container.RemoveChildren();
			_container.AddChild(_maskContentHolder);
		}

		public GComponent owner
		{
			get { return _owner; }
		}

		public bool bouncebackEffect
		{
			get { return _bouncebackEffect; }
			set { _bouncebackEffect = value; }
		}

		public bool touchEffect
		{
			get { return _touchEffect; }
			set { _touchEffect = value; }
		}

		public float scrollSpeed
		{
			get { return _scrollSpeed; }
			set
			{
				_scrollSpeed = value;
				if (_scrollSpeed == 0)
					_scrollSpeed = UIConfig.defaultScrollSpeed;
				_mouseWheelSpeed = _scrollSpeed * 2;
			}
		}

		public bool snapToItem
		{
			get { return _snapToItem; }
			set { _snapToItem = value; }
		}

		public bool mouseWheelEnabled
		{
			get { return _mouseWheelEnabled; }
			set { _mouseWheelEnabled = value; }
		}

		public float percX
		{
			get { return _xPerc; }
			set { SetPercX(value, false); }
		}

		public void SetPercX(float value, bool ani)
		{
			if (value > 1)
				value = 1;
			else if (value < 0)
				value = 0;
			if (value != _xPerc)
			{
				_xPerc = value;
				PosChanged(ani);
			}
		}

		public float percY
		{
			get { return _yPerc; }
			set { SetPercY(value, false); }
		}

		public void SetPercY(float value, bool ani)
		{
			if (value > 1)
				value = 1;
			else if (value < 0)
				value = 0;
			if (value != _yPerc)
			{
				_yPerc = value;
				PosChanged(ani);
			}
		}

		public float posX
		{
			get { return _xPerc * Mathf.Max(0, _contentWidth - _maskWidth); }
			set { SetPosX(value, false); }
		}

		public void SetPosX(float value, bool ani)
		{
			if (_contentWidth > _maskWidth)
				this.SetPercX(value / (_contentWidth - _maskWidth), ani);
			else
				this.SetPercX(0, ani);
		}

		public float posY
		{
			get { return _yPerc * (_contentHeight - _maskHeight); }
			set { SetPosY(value, false); }
		}

		public void SetPosY(float value, bool ani)
		{
			if (_contentHeight > _maskHeight)
				this.SetPercY(value / (_contentHeight - _maskHeight), ani);
			else
				this.SetPercY(0, ani);
		}

		public bool isBottomMost
		{
			get { return _yPerc == 1 || _contentHeight <= _maskHeight; }
		}

		public bool isRightMost
		{
			get { return _xPerc == 1 || _contentWidth <= _maskWidth; }
		}

		public float contentWidth
		{
			get
			{
				_owner.EnsureBoundsCorrect();
				return _contentWidth;
			}
		}

		public float contentHeight
		{
			get
			{
				_owner.EnsureBoundsCorrect();
				return _contentHeight;
			}
		}

		public float viewWidth
		{
			get { return _maskWidth; }
			set
			{
				value = value + _margin.left + _margin.right;
				if (_vtScrollBar != null)
					value += _vtScrollBar.width;
				_owner.width = value;
			}
		}

		public float viewHeight
		{
			get { return _maskHeight; }
			set
			{
				value = value + _margin.top + _margin.bottom;
				if (_hzScrollBar != null)
					value += _hzScrollBar.height;
				_owner.height = value;
			}
		}

		float GetDeltaX(float move)
		{
			return move / (_contentWidth - _maskWidth);
		}

		float GetDeltaY(float move)
		{
			return move / (_contentHeight - _maskHeight);
		}

		public void ScrollTop()
		{
			ScrollTop(false);
		}

		public void ScrollTop(bool ani)
		{
			this.SetPercY(0, ani);
		}

		public void ScrollBottom()
		{
			ScrollBottom(false);
		}

		public void ScrollBottom(bool ani)
		{
			this.SetPercY(1, ani);
		}

		public void ScrollUp()
		{
			ScrollUp(1, false);
		}

		public void ScrollUp(float speed, bool ani)
		{
			this.SetPercY(_yPerc - GetDeltaY(_scrollSpeed * speed), ani);
		}

		public void ScrollDown()
		{
			ScrollDown(1, false);
		}

		public void ScrollDown(float speed, bool ani)
		{
			this.SetPercY(_yPerc + GetDeltaY(_scrollSpeed * speed), ani);
		}

		public void ScrollLeft()
		{
			ScrollLeft(1, false);
		}

		public void ScrollLeft(float speed, bool ani)
		{
			this.SetPercX(_xPerc - GetDeltaX(_scrollSpeed * speed), ani);
		}

		public void ScrollRight()
		{
			ScrollRight(1, false);
		}

		public void ScrollRight(float speed, bool ani)
		{
			this.SetPercX(_xPerc + GetDeltaX(_scrollSpeed * speed), ani);
		}

		public void ScrollToView(GObject obj)
		{
			ScrollToView(obj, false);
		}

		public void ScrollToView(GObject obj, bool ani)
		{
			_owner.EnsureBoundsCorrect();
			if (Timers.inst.Exists(Refresh))
				Refresh(null);

			if (_vScroll)
			{
				float top = _yPerc * (_contentHeight - _maskHeight);
				float bottom = top + _maskHeight;
				if (obj.y < top)
					SetPosY(obj.y, ani);
				else if (obj.y + obj.height > bottom)
				{
					if (obj.y + obj.height * 2 >= top)
						SetPosY(obj.y + obj.height * 2 - _maskHeight, ani);
					else
						SetPosY(obj.y + obj.height - _maskHeight, ani);
				}
			}
			if (_hScroll)
			{
				float left = _xPerc * (_contentWidth - _maskWidth);
				float right = left + _maskWidth;
				if (obj.x < left)
					SetPosX(obj.x, ani);
				else if (obj.x + obj.width > right)
				{
					if (obj.x + obj.width * 2 >= left)
						SetPosX(obj.x + obj.width * 2 - _maskWidth, ani);
					else
						SetPosX(obj.x + obj.width - _maskWidth, ani);
				}
			}

			if (!ani && Timers.inst.Exists(Refresh))
				Refresh(null);
		}

		public bool IsChildInView(GObject obj)
		{
			if (_vScroll)
			{
				float top = _yPerc * (_contentHeight - _maskHeight);
				float bottom = top + _maskHeight;
				if (obj.y + obj.height < top || obj.y > bottom)
					return false;
			}
			if (_hScroll)
			{
				float left = _xPerc * (_contentWidth - _maskWidth);
				float right = left + _maskWidth;
				if (obj.x + obj.width < left || obj.x > right)
					return false;
			}

			return true;
		}

		internal void SetSize(float aWidth, float aHeight)
		{
			float w, h;
			w = aWidth;
			h = aHeight;
			if (_hzScrollBar != null)
			{
				if (!_hScrollNone)
					h -= _hzScrollBar.height;
				_hzScrollBar.y = h;
				if (_vtScrollBar != null && !_vScrollNone)
				{
					_hzScrollBar.width = w - _vtScrollBar.width - _scrollBarMargin.left - _scrollBarMargin.right;
					if (_displayOnLeft)
						_hzScrollBar.x = _scrollBarMargin.left + _vtScrollBar.width;
					else
						_hzScrollBar.x = _scrollBarMargin.left;
				}
				else
				{
					_hzScrollBar.width = w - _scrollBarMargin.left - _scrollBarMargin.right;
					_hzScrollBar.x = _scrollBarMargin.left;
				}
			}
			if (_vtScrollBar != null)
			{
				if (!_vScrollNone)
					w -= _vtScrollBar.width;
				if (!_displayOnLeft)
					_vtScrollBar.x = w;
				_vtScrollBar.height = h - _scrollBarMargin.top - _scrollBarMargin.bottom;
				_vtScrollBar.y = _scrollBarMargin.top;
			}
			w -= (_margin.left + _margin.right);
			h -= (_margin.top + _margin.bottom);

			_maskWidth = w;
			_maskHeight = h;

			HandleSizeChanged();
			PosChanged(false);
		}

		internal void SetContentSize(float aWidth, float aHeight)
		{
			if (Mathf.Approximately(_contentWidth, aWidth) && Mathf.Approximately(_contentHeight, aHeight))
				return;

			_contentWidth = aWidth;
			_contentHeight = aHeight;
			HandleSizeChanged();
			_aniFlag = false;
			Refresh(null);
		}

		void HandleSizeChanged()
		{
			if (_displayInDemand)
			{
				if (_vtScrollBar != null)
				{
					if (_contentHeight <= _maskHeight)
					{
						if (!_vScrollNone)
						{
							_vScrollNone = true;
							_maskWidth += _vtScrollBar.width;
						}
					}
					else
					{
						if (_vScrollNone)
						{
							_vScrollNone = false;
							_maskWidth -= _vtScrollBar.width;
						}
					}
				}
				if (_hzScrollBar != null)
				{
					if (_contentWidth <= _maskWidth)
					{
						if (!_hScrollNone)
						{
							_hScrollNone = true;
							_maskHeight += _vtScrollBar.height;
						}
					}
					else
					{
						if (_hScrollNone)
						{
							_hScrollNone = false;
							_maskHeight -= _vtScrollBar.height;
						}
					}
				}
			}

			if (_vtScrollBar != null)
			{
				if (_maskHeight < _vtScrollBar.minSize)
					_vtScrollBar.displayObject.visible = false;
				else
				{
					_vtScrollBar.displayObject.visible = _scrollBarVisible && !_vScrollNone;
					if (_contentHeight == 0)
						_vtScrollBar.displayPerc = 0;
					else
						_vtScrollBar.displayPerc = Math.Min(1, _maskHeight / _contentHeight);
				}
			}
			if (_hzScrollBar != null)
			{
				if (_maskWidth < _hzScrollBar.minSize)
					_hzScrollBar.displayObject.visible = false;
				else
				{
					_hzScrollBar.displayObject.visible = _scrollBarVisible && !_hScrollNone;
					if (_contentWidth == 0)
						_hzScrollBar.displayPerc = 0;
					else
						_hzScrollBar.displayPerc = Math.Min(1, _maskWidth / _contentWidth);
				}
			}

			_maskHolder.clipRect = new Rect(0, 0, _maskWidth * GRoot.contentScaleFactor, _maskHeight * GRoot.contentScaleFactor);

			_xOverlap = Mathf.Ceil(Math.Max(0, _contentWidth - _maskWidth) * GRoot.contentScaleFactor);
			_yOverlap = Mathf.Ceil(Math.Max(0, _contentHeight - _maskHeight) * GRoot.contentScaleFactor);

			switch (_scrollType)
			{
				case ScrollType.Both:

					if (_contentWidth > _maskWidth && _contentHeight <= _maskHeight)
					{
						_hScroll = true;
						_vScroll = false;
					}
					else if (_contentWidth <= _maskWidth && _contentHeight > _maskHeight)
					{
						_hScroll = false;
						_vScroll = true;
					}
					else if (_contentWidth > _maskWidth && _contentHeight > _maskHeight)
					{
						_hScroll = true;
						_vScroll = true;
					}
					else
					{
						_hScroll = false;
						_vScroll = false;
					}
					break;

				case ScrollType.Vertical:

					if (_contentHeight > _maskHeight)
					{
						_hScroll = false;
						_vScroll = true;
					}
					else
					{
						_hScroll = false;
						_vScroll = false;
					}
					break;

				case ScrollType.Horizontal:

					if (_contentWidth > _maskWidth)
					{
						_hScroll = true;
						_vScroll = false;
					}
					else
					{
						_hScroll = false;
						_vScroll = false;
					}
					break;
			}
		}

		internal void SetClipSoftness(Vector2 value)
		{
			if (value.x > 0 || value.y > 0)
				_clipSoftness = value;
			else
				_clipSoftness = null;

			if (_clipSoftness != null)
				UpdateClipSoft();
			else
				_maskHolder.clipSoftness = new Vector4(0, 0, 0, 0);
		}

		void UpdateClipSoft()
		{
			Vector2 softness = (Vector2)_clipSoftness;
			_maskHolder.clipSoftness = new Vector4(
				//左边缘和上边缘感觉不需要效果，所以注释掉
				/*_xPerc < 0.01 ? 0 : softness.x * GRoot.contentScaleFactor,
				_yPerc < 0.01 ? 0 : softness.y * GRoot.contentScaleFactor,*/
				0,
				0,
				(!_hScroll || _xPerc > 0.99) ? 0 : softness.x * GRoot.contentScaleFactor,
				(!_vScroll || _yPerc > 0.99) ? 0 : softness.y * GRoot.contentScaleFactor);
		}

		private void PosChanged(bool ani)
		{
			if (_aniFlag)
				_aniFlag = ani;
			Timers.inst.Add(0.001f, 1, Refresh);
		}

		private void Refresh(object obj)
		{
			if (_isMouseMoved)
			{
				Timers.inst.Add(0.001f, 1, Refresh);
				return;
			}
			Timers.inst.Remove(Refresh);
			float contentXLoc = 0f;
			float contentYLoc = 0f;

			if (_hScroll)
				contentXLoc = _xPerc * (_contentWidth - _maskWidth);
			if (_vScroll)
				contentYLoc = _yPerc * (_contentHeight - _maskHeight);

			if (_snapToItem)
			{
				float tmpX = _xPerc == 1 ? 0 : contentXLoc;
				float tmpY = _yPerc == 1 ? 0 : contentYLoc;
				_owner.FindObjectNear(ref tmpX, ref tmpY);
				if (_xPerc != 1 && !Mathf.Approximately(tmpX, contentXLoc))
				{
					_xPerc = tmpX / (_contentWidth - _maskWidth);
					if (_xPerc > 1)
						_xPerc = 1;
					contentXLoc = _xPerc * (_contentWidth - _maskWidth);
				}
				if (_yPerc != 1 && !Mathf.Approximately(tmpY, contentYLoc))
				{
					_yPerc = tmpY / (_contentHeight - _maskHeight);
					if (_yPerc > 1)
						_yPerc = 1;
					contentYLoc = _yPerc * (_contentHeight - _maskHeight);
				}
			}
			contentXLoc = (int)(contentXLoc * GRoot.contentScaleFactor);
			contentYLoc = (int)(contentYLoc * GRoot.contentScaleFactor);

			if (_aniFlag)
			{
				float toX = _maskContentHolder.x;
				float toY = _maskContentHolder.y;

				if (_vScroll)
				{
					toY = -contentYLoc;
				}
				else
				{
					if (_maskContentHolder.y != 0)
						_maskContentHolder.y = 0;
				}
				if (_hScroll)
				{
					toX = -contentXLoc;
				}
				else
				{
					if (_maskContentHolder.x != 0)
						_maskContentHolder.x = 0;
				}

				if (toX != _maskContentHolder.x || toY != _maskContentHolder.y)
				{
					_maskHolder.touchable = false;

					if (_tweener != null)
					{
						_tweener.Complete();
						_tweener = null;
					}
					_tweener = DOTween.To(() => _maskContentHolder.xy, v => _maskContentHolder.xy = v, new Vector2(toX, toY), 0.5f)
						.SetEase(Ease.OutCubic)
						.OnUpdate(__tweenUpdate)
						.OnComplete(__tweenComplete);
				}
			}
			else
			{
				if (_tweener != null)
				{
					_tweener.Complete();
					_tweener = null;
				}

				_maskContentHolder.y = -contentYLoc;
				_maskContentHolder.x = -contentXLoc;
				if (_vtScrollBar != null)
					_vtScrollBar.scrollPerc = _yPerc;
				if (_hzScrollBar != null)
					_hzScrollBar.scrollPerc = _xPerc;

				if (_clipSoftness != null)
					UpdateClipSoft();
			}

			_aniFlag = true;
		}

		private float CalcYPerc()
		{
			if (!_vScroll)
				return 0;

			float diff = _contentHeight - _maskHeight;
			float my = _maskContentHolder.y / GRoot.contentScaleFactor;
			float currY;
			if (my > 0)
				currY = 0;
			else if (-my > diff)
				currY = diff;
			else
				currY = -my;
			return currY / diff;
		}

		private float CalcXPerc()
		{
			if (!_hScroll)
				return 0;

			float diff = _contentWidth - _maskWidth;
			float mx = _maskContentHolder.x / GRoot.contentScaleFactor;
			float currX;
			if (mx > 0)
				currX = 0;
			else if (-mx > diff)
				currX = diff;
			else
				currX = -mx;

			return currX / diff;
		}

		private void OnScrolling()
		{
			if (_vtScrollBar != null)
			{
				_vtScrollBar.scrollPerc = CalcYPerc();
				if (_scrollBarDisplayAuto)
					ShowScrollBar(true);
			}
			if (_hzScrollBar != null)
			{
				_hzScrollBar.scrollPerc = CalcXPerc();
				if (_scrollBarDisplayAuto)
					ShowScrollBar(true);
			}

			if (_clipSoftness != null)
				UpdateClipSoft();
		}

		private void OnScrollEnd()
		{
			if (_vtScrollBar != null)
			{
				if (_scrollBarDisplayAuto)
					ShowScrollBar(false);
			}
			if (_hzScrollBar != null)
			{
				if (_scrollBarDisplayAuto)
					ShowScrollBar(false);
			}

			if (_clipSoftness != null)
				UpdateClipSoft();

			_owner.onScroll.Call();
		}

		private void __mouseDown(EventContext context)
		{
			if (!_touchEffect)
				return;

			InputEvent evt = context.inputEvent;
			_touchId = evt.touchId;
			Vector2 pt = _owner.GlobalToLocal(new Vector2(evt.x, evt.y));
			if (_tweener != null)
			{
				_tweener.Complete();
				_tweener = null;
				Stage.inst.CancelClick(_touchId);
			}

			_y1 = _y2 = _maskContentHolder.y;
			_yOffset = pt.y - _maskContentHolder.y;

			_x1 = _x2 = _maskContentHolder.x;
			_xOffset = pt.x - _maskContentHolder.x;

			_time1 = _time2 = Time.time;
			_holdAreaPoint.x = pt.x;
			_holdAreaPoint.y = pt.y;
			_isHoldAreaDone = false;
			_isMouseMoved = false;

			_container.stage.onMouseMove.Add(__mouseMove);
			_container.stage.onMouseUp.Add(__mouseUp);
		}

		private void __mouseMove(EventContext context)
		{
			InputEvent evt = context.inputEvent;
			if (_touchId != evt.touchId)
				return;

			Vector2 pt = _owner.GlobalToLocal(new Vector2(evt.x, evt.y));

			float diff;
			bool sv = false, sh = false, st = false;

			if (_scrollType == ScrollType.Vertical)
			{
				if (!_isHoldAreaDone)
				{
					diff = Mathf.Abs(_holdAreaPoint.y - pt.y);
					if (diff < _holdArea)
						return;
				}

				sv = true;
			}
			else if (_scrollType == ScrollType.Horizontal)
			{
				if (!_isHoldAreaDone)
				{
					diff = Mathf.Abs(_holdAreaPoint.x - pt.x);
					if (diff < _holdArea)
						return;
				}

				sh = true;
			}
			else
			{
				if (!_isHoldAreaDone)
				{
					diff = Mathf.Abs(_holdAreaPoint.y - pt.y);
					if (diff < _holdArea)
					{
						diff = Mathf.Abs(_holdAreaPoint.x - pt.x);
						if (diff < _holdArea)
							return;
					}
				}

				sv = sh = true;
			}

			float t = Time.time;
			if (t - _time2 > 0.05f)
			{
				_time2 = _time1;
				_time1 = t;
				st = true;
			}

			if (sv)
			{
				float y = pt.y - _yOffset;
				if (y > 0)
				{
					if (!_bouncebackEffect)
						_maskContentHolder.y = 0;
					else
						_maskContentHolder.y = (int)(y * 0.5);
				}
				else if (y < -_yOverlap)
				{
					if (!_bouncebackEffect)
						_maskContentHolder.y = -(int)_yOverlap;
					else
						_maskContentHolder.y = (int)((y - _yOverlap) * 0.5);
				}
				else
				{
					_maskContentHolder.y = y;
				}

				if (st)
				{
					_y2 = _y1;
					_y1 = _maskContentHolder.y;
				}

				_yPerc = CalcYPerc();
			}

			if (sh)
			{
				float x = pt.x - _xOffset;
				if (x > 0)
				{
					if (!_bouncebackEffect)
						_maskContentHolder.x = 0;
					else
						_maskContentHolder.x = (int)(x * 0.5);
				}
				else if (x < 0 - _xOverlap)
				{
					if (!_bouncebackEffect)
						_maskContentHolder.x = -(int)_xOverlap;
					else
						_maskContentHolder.x = (int)((x - _xOverlap) * 0.5);
				}
				else
				{
					_maskContentHolder.x = x;
				}

				if (st)
				{
					_x2 = _x1;
					_x1 = _maskContentHolder.x;
				}

				_xPerc = CalcXPerc();
			}

			_maskHolder.touchable = false;
			_isHoldAreaDone = true;
			_isMouseMoved = true;
			OnScrolling();

			_owner.onScroll.Call();
		}

		private void __mouseUp(EventContext context)
		{
			if (!_touchEffect)
			{
				_isMouseMoved = false;
				return;
			}

			InputEvent evt = context.inputEvent;
			if (_touchId != evt.touchId)
				return;

			_container.stage.onMouseMove.Remove(__mouseMove);
			_container.stage.onMouseUp.Remove(__mouseUp);

			if (!_isMouseMoved)
				return;

			_isMouseMoved = false;

			float time = Time.time - _time2;
			if (time == 0)
				time = 0.001f;
			float yVelocity = (_maskContentHolder.y - _y2) / time;
			float xVelocity = (_maskContentHolder.x - _x2) / time;

			float minDuration = _bouncebackEffect ? 0.3f : 0;
			float maxDuration = 0.5f;
			int overShoot = _bouncebackEffect ? 1 : 0;

			float xMin = -_xOverlap, yMin = -_yOverlap;
			float xMax = 0, yMax = 0;

			float duration = 0f;

			if (_hScroll)
				ThrowTween.CalculateDuration(_maskContentHolder.x, xMin, xMax, xVelocity, overShoot, ref duration);
			if (_vScroll)
				ThrowTween.CalculateDuration(_maskContentHolder.y, yMin, yMax, yVelocity, overShoot, ref duration);

			if (duration > maxDuration)
				duration = maxDuration;
			else if (duration < minDuration)
				duration = minDuration;

			_throwTween.start.x = _maskContentHolder.x;
			_throwTween.start.y = _maskContentHolder.y;

			Vector2 change1, change2;
			float endX = 0, endY = 0;

			if (_scrollType == ScrollType.Both || _scrollType == ScrollType.Horizontal)
			{
				change1.x = ThrowTween.CalculateChange(xVelocity, duration);
				change2.x = 0;
				endX = _maskContentHolder.x + change1.x;
			}
			else
				change1.x = change2.x = 0;

			if (_scrollType == ScrollType.Both || _scrollType == ScrollType.Vertical)
			{
				change1.y = ThrowTween.CalculateChange(yVelocity, duration);
				change2.y = 0;
				endY = _maskContentHolder.y + change1.y;
			}
			else
				change1.y = change2.y = 0;

			if (_snapToItem)
			{
				endX = -endX / GRoot.contentScaleFactor;
				endY = -endY / GRoot.contentScaleFactor;
				_owner.FindObjectNear(ref endX, ref endY);
				endX = -endX * GRoot.contentScaleFactor;
				endY = -endY * GRoot.contentScaleFactor;
				change1.x = endX - _maskContentHolder.x;
				change1.y = endY - _maskContentHolder.y;
			}

			if (xMax < endX)
				change2.x = xMax - _maskContentHolder.x - change1.x;
			else if (xMin > endX)
				change2.x = xMin - _maskContentHolder.x - change1.x;

			if (yMax < endY)
				change2.y = yMax - _maskContentHolder.y - change1.y;
			else if (yMin > endY)
				change2.y = yMin - _maskContentHolder.y - change1.y;

			_throwTween.value = 0;
			_throwTween.change1 = change1;
			_throwTween.change2 = change2;

			if (_tweener != null)
				_tweener.Complete();

			_tweener = DOTween.To(() => _throwTween.value, v => _throwTween.value = v, 1, duration)
				.SetEase(Ease.OutCubic)
				.OnUpdate(__tweenUpdate2)
				.OnComplete(__tweenComplete2);
		}

		private void __mouseWheel(EventContext context)
		{
			if (!_mouseWheelEnabled)
				return;

			InputEvent evt = context.inputEvent;
			int delta = evt.mouseWheelDelta;
			if (_hScroll && !_vScroll)
			{
				if (delta < 0)
					this.SetPercX(_xPerc - GetDeltaX(_mouseWheelSpeed), false);
				else
					this.SetPercX(_xPerc + GetDeltaX(_mouseWheelSpeed), false);
			}
			else
			{
				if (delta < 0)
					this.SetPercY(_yPerc - GetDeltaY(_mouseWheelSpeed), false);
				else
					this.SetPercY(_yPerc + GetDeltaY(_mouseWheelSpeed), false);
			}
		}

		private void __rollOver(object e)
		{
			ShowScrollBar(true);
		}

		private void __rollOut(object e)
		{
			ShowScrollBar(false);
		}

		private void ShowScrollBar(bool val)
		{
			if (val)
			{
				__showScrollBar(true);
				Timers.inst.Remove(__showScrollBar);
			}
			else
				Timers.inst.Add(0.5f, 1, __showScrollBar, val);
		}

		private void __showScrollBar(object obj)
		{
			_scrollBarVisible = (bool)obj && _maskWidth > 0 && _maskHeight > 0;
			if (_vtScrollBar != null)
				_vtScrollBar.displayObject.visible = _scrollBarVisible && !_vScrollNone; ;
			if (_hzScrollBar != null)
				_hzScrollBar.displayObject.visible = _scrollBarVisible && !_hScrollNone; ;
		}

		private void __tweenUpdate()
		{
			OnScrolling();
		}

		private void __tweenComplete()
		{
			_tweener = null;
			_maskHolder.touchable = true;
			OnScrollEnd();
		}

		private void __tweenUpdate2()
		{
			_throwTween.Update(_maskContentHolder);

			if (_scrollType == ScrollType.Vertical)
				_yPerc = CalcYPerc();
			else if (_scrollType == ScrollType.Horizontal)
				_xPerc = CalcXPerc();
			else
			{
				_yPerc = CalcYPerc();
				_xPerc = CalcXPerc();
			}

			OnScrolling();
		}

		private void __tweenComplete2()
		{
			_tweener = null;

			if (_scrollType == ScrollType.Vertical)
				_yPerc = CalcYPerc();
			else if (_scrollType == ScrollType.Horizontal)
				_xPerc = CalcXPerc();
			else
			{
				_yPerc = CalcYPerc();
				_xPerc = CalcXPerc();
			}

			_isMouseMoved = false;
			_maskHolder.touchable = true;
			OnScrollEnd();
		}

		public class ThrowTween
		{
			public float value;
			public Vector2 start;
			public Vector2 change1, change2;

			const float resistance = 300;
			const float checkpoint = 0.05f;

			public void Update(DisplayObject obj)
			{
				obj.x = (int)(start.x + change1.x * value + change2.x * value * value);
				obj.y = (int)(start.y + change1.y * value + change2.y * value * value);
			}

			static public void CalculateDuration(float targetValue, float min, float max, float velocity,
				float overshootTolerance, ref float duration)
			{
				float curDuration = (velocity * resistance > 0) ? velocity / resistance : velocity / -resistance;
				float curClippedDuration = 0f;
				float clippedDuration = 9999999999f;

				float end = targetValue + CalculateChange(velocity, curDuration);
				if (end > max)
				{
					curClippedDuration = CalculateDuration(targetValue, max, velocity);
					if (curClippedDuration + overshootTolerance < clippedDuration)
						clippedDuration = curClippedDuration + overshootTolerance;
				}
				else if (end < min)
				{
					curClippedDuration = CalculateDuration(targetValue, min, velocity);
					if (curClippedDuration + overshootTolerance < clippedDuration)
						clippedDuration = curClippedDuration + overshootTolerance;
				}

				if (curClippedDuration > duration)
					duration = curClippedDuration;
				if (curDuration > duration)
					duration = curDuration;
				if (duration > clippedDuration)
					duration = clippedDuration;
			}

			static public float CalculateChange(float velocity, float duration)
			{
				return (duration * checkpoint * velocity) / easeOutCubic(checkpoint, 0, 1, 1);
			}

			static float CalculateDuration(float start, float end, float velocity)
			{
				return Mathf.Abs((end - start) * easeOutCubic(checkpoint, 0, 1, 1) / velocity / checkpoint);
			}

			static float easeOutCubic(float t, float b, float c, float d)
			{
				return c * ((t = t / d - 1) * t * t + 1) + b;
			}
		}
	}
}
