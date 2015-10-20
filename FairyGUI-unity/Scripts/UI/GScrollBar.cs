using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
	public class GScrollBar : GComponent
	{
		GObject _grip;
		GObject _arrowButton1;
		GObject _arrowButton2;
		GObject _bar;
		ScrollPane _target;

		bool _vertical;
		float _scrollPerc;

		bool _dragging;
		Vector2 _dragOffset;
		int _touchId;

		public GScrollBar()
		{
			_scrollPerc = 0;
		}

		public void SetScrollPane(ScrollPane target, bool vertical)
		{
			_target = target;
			_vertical = vertical;
		}

		public float displayPerc
		{
			set
			{
				if (_vertical)
				{
					_grip.height = Mathf.FloorToInt(value * _bar.height);
					_grip.y = Mathf.RoundToInt(_bar.y + (_bar.height - _grip.height) * _scrollPerc);
				}
				else
				{
					_grip.width = Mathf.FloorToInt(value * _bar.width);
					_grip.x = Mathf.RoundToInt(_bar.x + (_bar.width - _grip.width) * _scrollPerc);
				}
			}
		}

		public float scrollPerc
		{
			set
			{
				_scrollPerc = value;
				if (_vertical)
					_grip.y = Mathf.RoundToInt(_bar.y + (_bar.height - _grip.height) * _scrollPerc);
				else
					_grip.x = Mathf.RoundToInt(_bar.x + (_bar.width - _grip.width) * _scrollPerc);
			}
		}

		public float minSize
		{
			get
			{
				if (_vertical)
					return (_arrowButton1 != null ? _arrowButton1.height : 0) + (_arrowButton2 != null ? _arrowButton2.height : 0);
				else
					return (_arrowButton1 != null ? _arrowButton1.width : 0) + (_arrowButton2 != null ? _arrowButton2.width : 0);
			}
		}

		override public void ConstructFromXML(XML xml)
		{
			base.ConstructFromXML(xml);

			_grip = GetChild("grip");
			if (_grip == null)
			{
				Debug.LogWarning("FairyGUI: " + this.resourceURL + " should define grip");
				return;
			}

			_bar = GetChild("bar");
			if (_bar == null)
			{
				Debug.LogWarning("FairyGUI: " + this.resourceURL + " should define bar");
				return;
			}

			_arrowButton1 = GetChild("arrow1");
			_arrowButton2 = GetChild("arrow2");

			_grip.onMouseDown.Add(__gripMouseDown);
			if (_arrowButton1 != null)
				_arrowButton1.onClick.Add(__arrowButton1Click);
			if (_arrowButton2 != null)
				_arrowButton2.onClick.Add(__arrowButton2Click);
		}

		void __gripMouseDown(EventContext context)
		{
			if (_bar == null)
				return;

			context.StopPropagation();
			InputEvent evt = context.inputEvent;
			_touchId = evt.touchId;

			_dragOffset.x = evt.x / GRoot.contentScaleFactor - _grip.x;
			_dragOffset.y = evt.y / GRoot.contentScaleFactor - _grip.y;
			_dragging = true;

			Stage.inst.onMouseMove.Add(__stageMouseMove);
			Stage.inst.onMouseUp.Add(__stageMouseUp);
		}

		void __stageMouseMove(EventContext context)
		{
			InputEvent evt = context.inputEvent;
			if (_touchId != evt.touchId)
				return;

			if (_vertical)
			{
				float curY = evt.y / GRoot.contentScaleFactor - _dragOffset.y;
				float diff = _bar.height - _grip.height;
				if (diff == 0)
					_target.percY = 0;
				else
					_target.percY = (curY - _bar.y) / diff;
			}
			else
			{
				float curX = evt.x / GRoot.contentScaleFactor - _dragOffset.x;
				float diff = _bar.width - _grip.width;
				if (diff == 0)
					_target.percX = 0;
				else
					_target.percX = (curX - _bar.x) / diff;
			}
		}

		void __stageMouseUp(EventContext context)
		{
			InputEvent evt = context.inputEvent;
			if (_touchId != evt.touchId)
				return;

			if (_dragging)
			{
				_dragging = false;

				Stage.inst.onMouseMove.Remove(__stageMouseMove);
				Stage.inst.onMouseUp.Remove(__stageMouseUp);
			}
		}

		void __arrowButton1Click(EventContext context)
		{
			context.StopPropagation();

			if (_vertical)
				_target.ScrollUp();
			else
				_target.ScrollLeft();
		}

		void __arrowButton2Click(EventContext context)
		{
			context.StopPropagation();

			if (_vertical)
				_target.ScrollDown();
			else
				_target.ScrollRight();
		}

		void __barMouseDown(EventContext context)
		{
			InputEvent evt = context.inputEvent;
			Vector2 pt = _grip.displayObject.GlobalToLocal(new Vector2(evt.x, evt.y));
			if (_vertical)
			{
				if (pt.y < 0)
					_target.ScrollUp(4, false);
				else
					_target.ScrollDown(4, false);
			}
			else
			{
				if (pt.x < 0)
					_target.ScrollLeft(4, false);
				else
					_target.ScrollRight(4, false);
			}
		}
	}
}
