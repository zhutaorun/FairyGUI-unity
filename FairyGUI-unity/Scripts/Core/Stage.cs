using System.Collections.Generic;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
	public class Stage : Container
	{
		public static Stage inst { get; internal set; }
		public static int defaultLayer { get; private set; }
		public static bool touchScreen { get; private set; }
		public static float mouseX { get; private set; }
		public static float mouseY { get; private set; }
		internal static bool shiftDown { get; private set; }

		public Camera camera { get; private set; }
		public int stageHeight { get; private set; }
		public int stageWidth { get; private set; }
		public float soundVolume { get; set; }

		public EventListener onStageResized { get; private set; }
		public EventListener onMouseMove { get; private set; }
		public EventListener onPostUpdate { get; private set; }

		internal InputCaret inputCaret { get; private set; }
		internal Highlighter highlighter { get; private set; }

		DisplayObject _objectUnderMouse;
		DisplayObject _focused;
		GameObject _cameraObject;
		bool _halfPixelOffset;
		UpdateContext _updateContext;
		List<DisplayObject> _rollOutChain;
		List<DisplayObject> _rollOverChain;
		TouchInfo[] _touches;
		AudioSource _audio;
		StageEngine _engine;

		public static void Instantiate()
		{
			Instantiate(11, 1);
		}

		public static void Instantiate(int layer)
		{
			Instantiate(layer, 1);
		}

		public static void Instantiate(int layer, int cameraDepth)
		{
			if (inst != null)
				throw new System.Exception("Stage already Instantiate");

			new Stage(layer, cameraDepth);
		}

		public Stage(int layer, int cameraDepth)
			: base()
		{
			inst = this;
			defaultLayer = layer;
			soundVolume = 1;

			_updateContext = new UpdateContext();
			stageWidth = Screen.width;
			stageHeight = Screen.height;

			gameObject.name = "Stage";
			gameObject.hideFlags = HideFlags.None;
			gameObject.SetActive(true);
			_engine = gameObject.AddComponent<StageEngine>();
			Object.DontDestroyOnLoad(gameObject);

			_cameraObject = new GameObject("Camera");
			_cameraObject.hideFlags = HideFlags.None;
			_cameraObject.layer = defaultLayer;
			Object.DontDestroyOnLoad(_cameraObject);

			camera = _cameraObject.AddComponent<Camera>();
			camera.nearClipPlane = -8;
			camera.farClipPlane = 1;
			camera.depth = cameraDepth;
			camera.cullingMask = 1 << defaultLayer;
			camera.clearFlags = CameraClearFlags.Depth;
			camera.orthographic = true;
			camera.orthographicSize = 1;
			camera.transform.parent = cachedTransform;

			_halfPixelOffset = (Application.platform == RuntimePlatform.WindowsPlayer ||
				Application.platform == RuntimePlatform.XBOX360 ||
				Application.platform == RuntimePlatform.WindowsWebPlayer ||
				Application.platform == RuntimePlatform.WindowsEditor);

			// Only DirectX 9 needs the half-pixel offset
			if (_halfPixelOffset)
				_halfPixelOffset = (SystemInfo.graphicsShaderLevel < 40);

			if (Application.platform == RuntimePlatform.IPhonePlayer
				|| Application.platform == RuntimePlatform.Android
				|| Application.platform == RuntimePlatform.WP8Player)
				touchScreen = true;

			AdjustCamera();
			EnableSound();

			inputCaret = new InputCaret();
			highlighter = new Highlighter();

			_touches = new TouchInfo[5];
			for (int i = 0; i < _touches.Length; i++)
				_touches[i] = new TouchInfo();

			if (!touchScreen)
				_touches[0].touchId = 0;

			_rollOutChain = new List<DisplayObject>();
			_rollOverChain = new List<DisplayObject>();

			onStageResized = new EventListener(this, "onStageResized");
			onMouseMove = new EventListener(this, "onMouseMove");
			onPostUpdate = new EventListener(this, "onPostUpdate");
		}

		public DisplayObject objectUnderMouse
		{
			get
			{
				if (_objectUnderMouse == this)
					return null;
				else
					return _objectUnderMouse;
			}
		}

		public DisplayObject focus
		{
			get
			{
				if (_focused != null && _focused.isDisposed)
					_focused = null;
				return _focused;
			}
			set
			{
				if (_focused == value)
					return;

				if (_focused != null)
				{
					if ((_focused is TextField))
						((TextField)_focused).onFocusOut.Call();
				}

				_focused = value;
				if (_focused == this)
					_focused = null;
				if (_focused != null)
				{
					if ((_focused is TextField))
						((TextField)_focused).onFocusIn.Call();
				}
			}
		}

		internal void ValidateFocus(DisplayObject removing)
		{
			if (_focused != null)
			{
				if (_focused == removing)
					this.focus = null;
				else
				{
					DisplayObject currentObject = _focused.parent;
					while (currentObject != null)
					{
						if (currentObject == removing)
						{
							this.focus = null;
							break;
						}
						currentObject = currentObject.parent;
					}
				}
			}
		}

		public Vector2 GetTouchPosition(int touchId)
		{
			if (touchId < 0)
				return new Vector2(mouseX, mouseY);

			for (int j = 0; j < 5; j++)
			{
				TouchInfo touch = _touches[j];
				if (touch.touchId == touchId)
					return new Vector2(touch.x, touch.y);
			}

			return new Vector2(mouseX, mouseY);
		}

		public void ResetInputState()
		{
			for (int j = 0; j < 5; j++)
			{
				TouchInfo touch = _touches[j];
				touch.Reset();
			}

			if (!touchScreen)
				_touches[0].touchId = 0;
		}

		public void CancelClick(int touchId)
		{
			for (int j = 0; j < 5; j++)
			{
				TouchInfo touch = _touches[j];
				if (touch.touchId == touchId)
					touch.clickCancelled = true;
			}
		}

		public void EnableSound()
		{
			if (_audio == null)
			{
				_audio = gameObject.AddComponent<AudioSource>();
				_audio.bypassEffects = true;
			}
		}

		public void DisableSound()
		{
			if (_audio != null)
			{
				Object.DestroyObject(_audio);
				_audio = null;
			}
		}

		public void PlayOneShotSound(AudioClip clip, float volumeScale)
		{
			if (_audio != null && this.soundVolume > 0)
				_audio.PlayOneShot(clip, volumeScale * this.soundVolume);
		}

		public void PlayOneShotSound(AudioClip clip)
		{
			if (_audio != null && this.soundVolume > 0)
				_audio.PlayOneShot(clip, this.soundVolume);
		}

		void AdjustCamera()
		{
			Debug.Log("FairyGUI: screen size=" + stageWidth + "x" + stageHeight);

			bool widthEven = (stageWidth & 1) == 0;
			bool heightEven = (stageHeight & 1) == 0;
			Vector3 v = new Vector3((float)stageWidth / 2.0f, (float)-stageHeight / 2.0f, 0f);
			if (_halfPixelOffset)
			{
				if (!widthEven)
					v.x -= 0.5f;
				else
					v.x += 0.5f;
				if (heightEven)
					v.y -= 0.5f;
				else
					v.y += 0.5f;
				camera.transform.localPosition = v;
			}
			else
				camera.transform.localPosition = v;

			float size = 2f / stageHeight;
			Vector3 ls = cachedTransform.localScale;
			if (!(Mathf.Abs(ls.x - size) <= float.Epsilon) ||
				!(Mathf.Abs(ls.y - size) <= float.Epsilon) ||
				!(Mathf.Abs(ls.z - size) <= float.Epsilon))
			{
				cachedTransform.localScale = new Vector3(size, size, size);
			}
		}

		internal void Update()
		{
			int w, h;
			w = Screen.width;
			h = Screen.height;
			if (w != stageWidth || h != stageHeight)
			{
				stageWidth = w;
				stageHeight = h;
				AdjustCamera();

				onStageResized.Call();
			}

			if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
				shiftDown = false;
			else if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
				shiftDown = true;

			if (touchScreen)
				HandleTouchEvents();
			else
				HandleMouseEvents();

			_updateContext.Reset();
			Update(_updateContext, 1f);
			_engine.ObjectTotal = (int)DisplayObject._gInstanceCounter;
			_engine.ObjectOnStage = (int)_updateContext.counter;

			onPostUpdate.Call();

			if (inputCaret.active)
			{
				inputCaret.quadBatch.Update(_updateContext, 1f);
				inputCaret.Blink();
			}

			if (highlighter.active)
				highlighter.quadBatch.Update(_updateContext, 1f);
		}

		internal void HandleGUIEvents(Event evt)
		{
			if (evt.rawType == EventType.KeyDown && evt.keyCode != KeyCode.None)
			{
				TouchInfo touch = _touches[0];
				touch.keyCode = evt.keyCode;
				touch.modifiers = evt.modifiers;

				touch.UpdateEvent();
				DisplayObject f = this.focus;
				if (f != null)
					f.onKeyDown.BubbleCall(touch.evt);
				else
					this.onKeyDown.Call(touch.evt);
			}
			else if (evt.rawType == EventType.KeyUp)
			{
				TouchInfo touch = _touches[0];
				touch.modifiers = evt.modifiers;
			}
			else if (evt.type == EventType.scrollWheel)
			{
				if (_objectUnderMouse != null)
				{
					TouchInfo touch = _touches[0];
					touch.mouseWheelDelta = (int)evt.delta.y;
					touch.UpdateEvent();
					_objectUnderMouse.onMouseWheel.BubbleCall(touch.evt);
				}
			}
		}

		void HandleMouseEvents()
		{
			bool hitTested = false;
			Vector2 mousePosition = Input.mousePosition;
			mousePosition.y = stageHeight - mousePosition.y;
			TouchInfo touch = _touches[0];

			if (mousePosition.x >= 0 && mousePosition.y >= 0)
			{
				if (touch.x != mousePosition.x || touch.y != mousePosition.y)
				{
					mouseX = mousePosition.x;
					mouseY = mousePosition.y;
					touch.x = mouseX;
					touch.y = mouseY;

					_objectUnderMouse = HitTest(mousePosition, true);
					hitTested = true;
					touch.target = _objectUnderMouse;

					touch.UpdateEvent();
					onMouseMove.Call(touch.evt);

					if (touch.lastRollOver != _objectUnderMouse)
						HandleRollOver(touch);
				}
			}
			else
				mousePosition = new Vector2(mouseX, mouseY);

			if (Input.GetMouseButtonDown(0))
			{
				if (!touch.began)
				{
					touch.began = true;
					touch.clickCancelled = false;
					touch.downX = touch.x;
					touch.downY = touch.y;

					if (!hitTested)
					{
						_objectUnderMouse = HitTest(mousePosition, true);
						hitTested = true;
						touch.target = _objectUnderMouse;
					}

					this.focus = _objectUnderMouse;

					if (_objectUnderMouse != null)
					{
						touch.UpdateEvent();
						_objectUnderMouse.onMouseDown.BubbleCall(touch.evt);
					}
				}
			}
			if (Input.GetMouseButtonUp(0))
			{
				if (touch.began)
				{
					touch.began = false;

					if (!hitTested)
					{
						_objectUnderMouse = HitTest(mousePosition, true);
						hitTested = true;
						touch.target = _objectUnderMouse;
					}

					if (_objectUnderMouse != null)
					{
						touch.UpdateEvent();
						_objectUnderMouse.onMouseUp.BubbleCall(touch.evt);

						if (!touch.clickCancelled && Mathf.Abs(touch.x - touch.downX) < 10 && Mathf.Abs(touch.y - touch.downY) < 10)
						{
							if (Time.realtimeSinceStartup - touch.lastClickTime < 0.35f)
							{
								if (touch.clickCount == 2)
									touch.clickCount = 1;
								else
									touch.clickCount++;
							}
							else
								touch.clickCount = 1;
							touch.lastClickTime = Time.realtimeSinceStartup;
							touch.UpdateEvent();
							_objectUnderMouse.onClick.BubbleCall(touch.evt);
						}
					}
				}
			}
			if (Input.GetMouseButtonUp(1))
			{
				if (!hitTested)
				{
					_objectUnderMouse = HitTest(mousePosition, true);
					hitTested = true;
					touch.target = _objectUnderMouse;
				}

				if (_objectUnderMouse != null)
				{
					touch.UpdateEvent();
					_objectUnderMouse.onRightClick.BubbleCall(touch.evt);
				}
			}
		}

		void HandleTouchEvents()
		{
			for (int i = 0; i < Input.touchCount; ++i)
			{
				Touch uTouch = Input.GetTouch(i);

				if (uTouch.phase == TouchPhase.Stationary)
					continue;

				bool hitTested = false;
				Vector2 touchPosition = uTouch.position;
				touchPosition.y = stageHeight - touchPosition.y;
				TouchInfo touch = null;
				for (int j = 0; j < 5; j++)
				{
					if (_touches[j].touchId == uTouch.fingerId)
					{
						touch = _touches[j];
						break;
					}

					if (_touches[j].touchId == -1)
						touch = _touches[j];
				}
				if (touch == null)
					continue;

				touch.touchId = uTouch.fingerId;
				mouseX = touchPosition.x;
				mouseY = touchPosition.y;

				if (touch.x != mouseX || touch.y != mouseY)
				{
					touch.x = mouseX;
					touch.y = mouseY;

					_objectUnderMouse = HitTest(touchPosition, true);
					hitTested = true;
					touch.target = _objectUnderMouse;

					touch.UpdateEvent();
					onMouseMove.Call(touch.evt);

					//no rollover/rollout on mobile
					//if (evt.lastRollOver != _objectUnderMouse)
					//HandleRollOver(evt);
				}

				if (uTouch.phase == TouchPhase.Began)
				{
					if (!touch.began)
					{
						touch.began = true;
						touch.clickCancelled = false;
						touch.downX = touch.x;
						touch.downY = touch.y;

						if (!hitTested)
						{
							_objectUnderMouse = HitTest(touchPosition, true);
							hitTested = true;
							touch.target = _objectUnderMouse;
						}

						this.focus = _objectUnderMouse;

						if (_objectUnderMouse != null)
						{
							touch.UpdateEvent();
							_objectUnderMouse.onMouseDown.BubbleCall(touch.evt);
						}
					}
				}
				else if (uTouch.phase == TouchPhase.Canceled || uTouch.phase == TouchPhase.Ended)
				{
					if (touch.began)
					{
						touch.began = false;

						if (!hitTested)
						{
							_objectUnderMouse = HitTest(touchPosition, true);
							hitTested = true;
							touch.target = _objectUnderMouse;
						}

						if (_objectUnderMouse != null)
						{
							touch.UpdateEvent();
							_objectUnderMouse.onMouseUp.BubbleCall(touch.evt);

							if (!touch.clickCancelled && Mathf.Abs(touch.x - touch.downX) < 50 && Mathf.Abs(touch.y - touch.downY) < 50)
							{
								touch.clickCount = uTouch.tapCount;
								touch.UpdateEvent();
								_objectUnderMouse.onClick.BubbleCall(touch.evt);
							}
						}
					}

					touch.Reset();
				}
			}
		}

		void HandleRollOver(TouchInfo touch)
		{
			DisplayObject element;
			element = touch.lastRollOver;
			while (element != null)
			{
				_rollOutChain.Add(element);
				element = element.parent;
			}

			touch.lastRollOver = touch.target;

			element = touch.target;
			int i;
			while (element != null)
			{
				i = _rollOutChain.IndexOf(element);
				if (i != -1)
				{
					_rollOutChain.RemoveRange(i, _rollOutChain.Count - i);
					break;
				}
				_rollOverChain.Add(element);

				element = element.parent;
			}

			int cnt = _rollOutChain.Count;
			if (cnt > 0)
			{
				for (i = 0; i < cnt; i++)
				{
					element = _rollOutChain[i];
					if (element.stage != null)
						element.onRollOut.Call();
				}
				_rollOutChain.Clear();
			}

			cnt = _rollOverChain.Count;
			if (cnt > 0)
			{
				for (i = 0; i < cnt; i++)
				{
					element = _rollOverChain[i];
					if (element.stage != null)
						element.onRollOver.Call();
				}
				_rollOverChain.Clear();
			}
		}

		public override DisplayObject HitTest(Vector2 localPoint, bool forTouch)
		{
			DisplayObject ret = base.HitTest(localPoint, forTouch);
			if (ret != null)
				return ret;
			else
				return this;
		}
	}

	public class StageEngine : MonoBehaviour
	{
		public int ObjectTotal;
		public int ObjectOnStage;

		Stage _stage;

		void Awake()
		{
			_stage = Stage.inst;
		}

		void LateUpdate()
		{
			_stage.Update();
		}

		void OnDisable()
		{
			_stage.Dispose();
		}

		void OnGUI()
		{
			_stage.HandleGUIEvents(Event.current);
		}
	}

	class TouchInfo
	{
		public float x;
		public float y;
		public int touchId;
		public int clickCount;
		public KeyCode keyCode;
		public EventModifiers modifiers;
		public int mouseWheelDelta;

		public float downX;
		public float downY;
		public bool began;
		public bool clickCancelled;
		public float lastClickTime;
		public DisplayObject target;
		public DisplayObject lastRollOver;

		public InputEvent evt;

		public TouchInfo()
		{
			evt = new InputEvent();
			Reset();
		}

		public void Reset()
		{
			touchId = -1;
			x = 0;
			y = 0;
			clickCount = 0;
			keyCode = KeyCode.None;
			modifiers = 0;
			mouseWheelDelta = 0;
			lastClickTime = 0;
			began = false;
			target = null;
			lastRollOver = null;
			clickCancelled = false;
		}

		public void UpdateEvent()
		{
			evt.touchId = this.touchId;
			evt.x = this.x;
			evt.y = this.y;
			evt.clickCount = this.clickCount;
			evt.keyCode = this.keyCode;
			evt.modifiers = this.modifiers;
			evt.mouseWheelDelta = this.mouseWheelDelta;
		}
	}
}