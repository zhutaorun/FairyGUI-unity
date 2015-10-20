using UnityEngine;

namespace FairyGUI
{
	public class InputEvent
	{
		public float x { get; internal set; }
		public float y { get; internal set; }
		public KeyCode keyCode { get; internal set; }
		public EventModifiers modifiers { get; internal set; }
		public int mouseWheelDelta { get; internal set; }
		public int touchId { get; internal set; }

		internal int clickCount;

		internal void Reset()
		{
			touchId = -1;
			x = 0;
			y = 0;
			clickCount = 0;
			keyCode = KeyCode.None;
			modifiers = 0;
			mouseWheelDelta = 0;
		}

		public Vector2 position
		{
			get { return new Vector2(x, y); }
		}

		public bool isDoubleClick
		{
			get { return clickCount > 1; }
		}

		public bool ctrl
		{
			get
			{
				RuntimePlatform rp = Application.platform;
				bool isMac = (
					rp == RuntimePlatform.OSXEditor ||
					rp == RuntimePlatform.OSXPlayer ||
					rp == RuntimePlatform.OSXWebPlayer);

				return isMac ?
					((modifiers & EventModifiers.Command) != 0) :
					((modifiers & EventModifiers.Control) != 0);
			}
		}

		public bool shift
		{
			get
			{
				//return (modifiers & EventModifiers.Shift) != 0;
				return Stage.shiftDown;
			}
		}

		public bool alt
		{
			get
			{
				return (modifiers & EventModifiers.Alt) != 0;
			}
		}
	}
}
