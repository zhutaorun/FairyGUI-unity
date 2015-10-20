using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FairyGUI
{
	public class EventListener
	{
		public EventDispatcher owner { get; private set; }

		EventCallback0 _callback0;
		EventCallback1 _callback1;
		EventCallback1 _captureCallback;
		string _type;
		bool _dispatching;
		bool _regsiter;

		public EventListener(EventDispatcher owner, string type)
		{
			this.owner = owner;
			this._type = type;
		}

		public string type
		{
			get { return _type; }
		}

		public void AddCapture(EventCallback1 callback)
		{
			_captureCallback -= callback;
			_captureCallback += callback;

			if (!_regsiter)
			{
				_regsiter = true;
				this.owner.RegisterListener(this);
			}
		}

		public void RemoveCapture(EventCallback1 callback)
		{
			_captureCallback -= callback;
		}

		public void Add(EventCallback1 callback)
		{
			_callback1 -= callback;
			_callback1 += callback;

			if (!_regsiter)
			{
				_regsiter = true;
				this.owner.RegisterListener(this);
			}
		}

		public void Remove(EventCallback1 callback)
		{
			_callback1 -= callback;
		}


		public void Add(EventCallback0 callback)
		{
			_callback0 -= callback;
			_callback0 += callback;

			if (!_regsiter)
			{
				_regsiter = true;
				this.owner.RegisterListener(this);
			}
		}

		public void Remove(EventCallback0 callback)
		{
			_callback0 -= callback;
		}

		public bool isEmpty
		{
			get { return _callback1 == null && _callback0 == null && _captureCallback == null; }
		}

		public void Clear()
		{
			_callback1 = null;
			_callback0 = null;
			_captureCallback = null;
		}

		public bool Call()
		{
			return owner.InternalDispatchEvent(this._type, this, null);
		}

		public bool Call(object data)
		{
			return owner.InternalDispatchEvent(this._type, this, data);
		}

		public bool BubbleCall(object data)
		{
			return owner.BubbleEvent(_type, data);
		}

		public bool BubbleCall()
		{
			return owner.BubbleEvent(_type, null);
		}

		public bool BroadcastCall(object data)
		{
			return owner.BroadcastEvent(_type, data);
		}

		public bool BroadcastCall()
		{
			return owner.BroadcastEvent(_type, null);
		}

		internal void CallInternal(EventContext context)
		{
			if (_dispatching)
				return;

			_dispatching = true;
			context.sender = this.owner;
			try
			{
				if (_callback1 != null)
					_callback1(context);
				if (_callback0 != null)
					_callback0();
			}
			finally
			{
				_dispatching = false;
			}
		}

		internal void CallCaptureInternal(EventContext context)
		{
			if (_captureCallback == null)
				return;

			if (_dispatching)
				return;

			_dispatching = true;
			context.sender = this.owner;
			try
			{
				_captureCallback(context);
			}
			finally
			{
				_dispatching = false;
			}
		}
	}
}
