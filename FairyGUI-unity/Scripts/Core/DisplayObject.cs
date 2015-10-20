using UnityEngine;

namespace FairyGUI
{
	public class DisplayObject : EventDispatcher
	{
		public string name;
		public Container parent { get; internal set; }
		public GameObject gameObject { get; private set; }
		public Transform cachedTransform { get; private set; }
		public QuadBatch quadBatch { get; protected set; }

		public GObject gOwner;

		public EventListener onClick { get; private set; }
		public EventListener onRightClick { get; private set; }
		public EventListener onMouseDown { get; private set; }
		public EventListener onMouseUp { get; private set; }
		public EventListener onRollOver { get; private set; }
		public EventListener onRollOut { get; private set; }
		public EventListener onMouseWheel { get; private set; }
		public EventListener onAddedToStage { get; private set; }
		public EventListener onRemovedFromStage { get; private set; }
		public EventListener onKeyDown { get; private set; }
		public EventListener onClickLink { get; private set; }

		float _alpha;
		float _x;
		float _y;
		float _z;
		bool _visible;
		float _scaleX;
		float _scaleY;
		float _rotation;
		bool _touchable;
		float _pivotX;
		float _pivotY;

		protected Rect contentRect;
		protected internal bool optimizeNotTouchable;
		protected bool scaleOverrided;

		internal float tmpZ;
		internal Rect tmpBounds;
		internal uint internalIndex;

		internal static uint _gInstanceCounter;

		public DisplayObject()
		{
			_alpha = 1;
			_x = 0;
			_y = 0;
			_visible = true;
			_scaleX = 1;
			_scaleY = 1;
			_rotation = 0;
			_touchable = true;
			internalIndex = _gInstanceCounter++;

			gameObject = new GameObject(this.GetType().Name);
			gameObject.layer = Stage.defaultLayer;
			gameObject.hideFlags = HideFlags.HideInHierarchy;
			cachedTransform = gameObject.transform;
			gameObject.SetActive(false);
			UnityEngine.Object.DontDestroyOnLoad(gameObject);

			onClick = new EventListener(this, "onClick");
			onRightClick = new EventListener(this, "onRightClick");
			onMouseDown = new EventListener(this, "onMouseDown");
			onMouseUp = new EventListener(this, "onMouseUp");
			onRollOver = new EventListener(this, "onRollOver");
			onRollOut = new EventListener(this, "onRollOut");
			onMouseWheel = new EventListener(this, "onMouseWheel");
			onAddedToStage = new EventListener(this, "onAddedToStage");
			onRemovedFromStage = new EventListener(this, "onRemovedFromStage");
			onKeyDown = new EventListener(this, "onKeyDown");
			onClickLink = new EventListener(this, "onClickLink");
		}

		public float alpha
		{
			get { return _alpha; }
			set { _alpha = value; }
		}

		public bool visible
		{
			get { return _visible; }
			set
			{
				if (_visible != value)
				{
					_visible = value;
					SetGO_Visible();
				}
			}
		}

		public float x
		{
			get { return _x; }
			set
			{
				if (_x != value)
				{
					_x = value;
					SetGO_Position();
				}
			}
		}
		public float y
		{
			get { return _y; }
			set
			{
				if (_y != value)
				{
					_y = value;
					SetGO_Position();
				}
			}
		}
		public float z
		{
			get { return _z; }
			set
			{
				if (_z != value)
				{
					_z = value;
					SetGO_Position();
				}
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
				_x = xv;
				_y = yv;
				SetGO_Position();
			}
		}

		protected float pivotX
		{
			get { return _pivotX; }
			set
			{
				if (_pivotX != value)
				{
					_pivotX = value;
					SetGO_Position();
				}
			}
		}
		protected float pivotY
		{
			get { return _pivotY; }
			set
			{
				if (_pivotY != value)
				{
					_pivotY = value;
					SetGO_Position();
				}
			}
		}

		virtual public float width
		{
			get { return GetBounds(parent).width; }
			set
			{
				scaleX = 1;
				float actualWidth = width;
				if (actualWidth != 0.0) scaleX = value / actualWidth;
			}
		}
		virtual public float height
		{
			get { return GetBounds(parent).height; }
			set
			{
				scaleY = 1;
				float actualHeight = height;
				if (actualHeight != 0.0) scaleY = value / actualHeight;
			}
		}

		public Vector2 size
		{
			get { return GetBounds(parent).size; }
			set { SetSize(value.x, value.y); }
		}

		public void SetSize(float wv, float hv)
		{
			SetScale(1, 1);
			Rect bounds = GetBounds(parent);
			float nx = _scaleX;
			float ny = _scaleY;
			if (bounds.width != 0.0) nx = wv / bounds.width;
			if (bounds.height != 0.0) ny = hv / bounds.height;
			SetScale(nx, ny);
		}

		virtual public float scaleX
		{
			get { return _scaleX; }
			set
			{
				if (_scaleX != value)
				{
					_scaleX = value;
					if (!scaleOverrided)
						cachedTransform.localScale = new Vector3(_scaleX, _scaleY, 1);
					else
						OverridedScale();
				}
			}
		}
		virtual public float scaleY
		{
			get { return _scaleY; }
			set
			{
				if (_scaleY != value)
				{
					_scaleY = value;
					if (!scaleOverrided)
						cachedTransform.localScale = new Vector3(_scaleX, _scaleY, 1);
					else
						OverridedScale();
				}
			}
		}

		public void SetScale(float xv, float yv)
		{
			if (_scaleX != xv || _scaleY != yv)
			{
				_scaleX = xv;
				_scaleY = yv;
				if (!scaleOverrided)
					cachedTransform.localScale = new Vector3(_scaleX, _scaleY, 1);
				else
					OverridedScale();
			}
		}


		public Vector2 scale
		{
			get { return new Vector2(_scaleX, _scaleY); }
			set { SetScale(value.x, value.y); }
		}

		public float rotation
		{
			get { return _rotation; }
			set
			{
				if (_rotation != value)
				{
					_rotation = value;
					SetGO_Rotation();
				}
			}
		}

		public Material material
		{
			get
			{
				if (quadBatch != null)
					return quadBatch.material;
				else
					return null;
			}
			set
			{
				if (quadBatch != null)
					quadBatch.material = value;
			}
		}

		public bool isDisposed
		{
			get { return gameObject == null; }
		}

		public void SetGrayed(bool value)
		{
			if (quadBatch == null)
				return;

			string shader = quadBatch.shader;
			quadBatch.shader = ShaderConfig.GetGrayedVersion(shader, value);
		}

		internal void SetParent(Container value)
		{
			if (parent != value)
			{
				parent = value;
				SetGO_Visible();
			}
		}

		public DisplayObject topmost
		{
			get
			{
				DisplayObject currentObject = this;
				while (currentObject.parent != null) currentObject = currentObject.parent;
				return currentObject;
			}
		}

		public DisplayObject root
		{
			get
			{
				DisplayObject currentObject = this;
				while (currentObject.parent != null)
				{
					if (currentObject.parent is Stage) return currentObject;
					else currentObject = currentObject.parent;
				}
				return null;
			}
		}

		public Stage stage
		{
			get { return topmost as Stage; }
		}

		public bool touchable
		{
			get { return _touchable; }
			set { _touchable = value; }
		}

		virtual public Rect GetBounds(DisplayObject targetSpace)
		{
			if (targetSpace == this || targetSpace == null || contentRect.width == 0 || contentRect.height == 0) // optimization
			{
				return contentRect;
			}
			else if (targetSpace == parent && _rotation == 0f)
			{
				Rect rect = new Rect(_x - contentRect.x * _scaleX,
					_y - contentRect.y * _scaleY,
					contentRect.width * _scaleX,
					contentRect.height * _scaleY);
				return rect;
			}
			else
				return TransformRect(contentRect, targetSpace);
		}

		virtual public DisplayObject HitTest(Vector2 localPoint, bool forTouch)
		{
			if (forTouch && (!_visible || !_touchable || optimizeNotTouchable)) return null;

			Rect rect = GetBounds(this);
			if (rect.width > 0 && rect.height > 0 && rect.Contains(localPoint))
				return this;
			else
				return null;
		}

		public Vector2 GlobalToLocal(Vector2 point)
		{
			return Stage.inst.TransformPoint(point, this);
		}

		public Vector2 LocalToGlobal(Vector2 point)
		{
			return TransformPoint(point, Stage.inst);
		}

		public Vector2 TransformPoint(Vector2 point, DisplayObject targetSpace)
		{
			if (targetSpace == this)
				return point;

			point.y = -point.y;
			if (this.scaleOverrided)
			{
				point.x *= _scaleX;
				point.y *= _scaleY;
			}
			Vector3 v = this.cachedTransform.TransformPoint(point);
			if (targetSpace != null)
			{
				if (targetSpace != Stage.inst || this.stage != null)
					v = targetSpace.cachedTransform.InverseTransformPoint(v);
				v.y = -v.y;
				if (targetSpace.scaleOverrided)
				{
					if (targetSpace._scaleX != 0)
						v.x /= targetSpace._scaleX;
					if (targetSpace._scaleY != 0)
						v.y /= targetSpace._scaleY;
				}
				v.x -= targetSpace._pivotX;
				v.y -= targetSpace._pivotY;
			}
			return v;
		}

		public Rect TransformRect(Rect rect, DisplayObject targetSpace)
		{
			if (targetSpace == this)
				return rect;

			float xMin = float.MaxValue, xMax = float.MinValue;
			float yMin = float.MaxValue, yMax = float.MinValue;
			Rect result = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

			bool skilInversing = targetSpace != Stage.inst || this.stage != null;

			TransformRectPoint(rect.xMin, rect.yMin, targetSpace, ref result, skilInversing);
			TransformRectPoint(rect.xMax, rect.yMin, targetSpace, ref result, skilInversing);
			TransformRectPoint(rect.xMin, rect.yMax, targetSpace, ref result, skilInversing);
			TransformRectPoint(rect.xMax, rect.yMax, targetSpace, ref result, skilInversing);

			return result;
		}

		private void TransformRectPoint(float px, float py, DisplayObject targetSpace, ref Rect rect, bool skipInversing)
		{
			if (this.scaleOverrided)
			{
				px *= _scaleX;
				py *= _scaleY;
			}
			Vector2 v = this.cachedTransform.TransformPoint(px, -py, 0);
			if (skipInversing)
				v = targetSpace.cachedTransform.InverseTransformPoint(v);
			v.y = -v.y;
			if (targetSpace.scaleOverrided)
			{
				if (targetSpace._scaleX != 0)
					v.x /= targetSpace._scaleX;
				if (targetSpace._scaleY != 0)
					v.y /= targetSpace._scaleY;
			}
			if (rect.xMin > v.x) rect.xMin = v.x;
			if (rect.xMax < v.x) rect.xMax = v.x;
			if (rect.yMin > v.y) rect.yMin = v.y;
			if (rect.yMax < v.y) rect.yMax = v.y;
		}

		public void RemoveFromParent()
		{
			if (parent != null)
				parent.RemoveChild(this);
		}

		virtual public void InvalidateBatchingState()
		{
			if (parent != null)
				parent.InvalidateBatchingState();
		}

		virtual public void Update(UpdateContext context, float parentAlpha)
		{
		}

		virtual protected void SetGO_Visible()
		{
			if (parent != null && _visible)
			{
#if !(UNITY_4_6_DOWNWARDS)
				cachedTransform.SetParent(parent.cachedTransform, false);
#else
                cachedTransform.parent = parent.cachedTransform;
                SetGO_Position();
                SetGO_Scale();
                SetGO_Rotation();
#endif
				gameObject.hideFlags = HideFlags.None;
				gameObject.SetActive(true);
			}
			else
			{
#if !(UNITY_4_6_DOWNWARDS)
				cachedTransform.SetParent(null, false);
#else
                cachedTransform.parent = null;
#endif
				gameObject.hideFlags = HideFlags.HideInHierarchy;
				gameObject.SetActive(false);

				Stage.inst.ValidateFocus(this);
			}
		}

		void SetGO_Position()
		{
			cachedTransform.localPosition = new Vector3(_x - _pivotX * _scaleX, -_y + _pivotY * _scaleY, _z);
		}

		void SetGO_Scale()
		{
			if (!scaleOverrided)
				cachedTransform.localScale = new Vector3(scaleX, scaleY, 1);
			else
				cachedTransform.localScale = new Vector3(1, 1, 1);
		}

		void SetGO_Rotation()
		{
			cachedTransform.localEulerAngles = new Vector3(0f, 0f, -_rotation);
		}

		virtual protected void OverridedScale()
		{
		}

		virtual public void Dispose()
		{
			RemoveEventListeners();
			if (gameObject != null)
			{
				GameObject.Destroy(gameObject);
				gameObject = null;
				cachedTransform = null;
			}
		}
	}
}
