using System;
using System.Collections.Generic;

namespace FairyGUI
{
    public delegate void EventCallback0();
    public delegate void EventCallback1(EventContext context);

    public class EventDispatcher
    {
        Dictionary<string, EventListener> _dic;

        public EventDispatcher()
        {
        }

        public void AddEventListener(string strType, EventCallback0 callback)
        {
            if (strType == null)
                throw new Exception("event type cant be null");

            if (_dic == null)
                _dic = new Dictionary<string, EventListener>();

            EventListener listener = null;
            if (!_dic.TryGetValue(strType, out listener))
                listener = new EventListener(this, strType);
            listener.Add(callback);
        }

        public void AddEventListener(string strType, EventCallback1 callback)
        {
            if (strType == null)
                throw new Exception("event type cant be null");

            if (_dic == null)
                _dic = new Dictionary<string, EventListener>();

            EventListener listener = null;
            if (!_dic.TryGetValue(strType, out listener))
                listener = new EventListener(this, strType);
            listener.Add(callback);
        }

        public void RemoveEventListener(string strType, EventCallback0 callback)
        {
            if (_dic == null)
                return;

            EventListener listener = null;
            if (_dic.TryGetValue(strType, out listener))
                listener.Remove(callback);
        }

        public void RemoveEventListener(string strType, EventCallback1 callback)
        {
            if (_dic == null)
                return;

            EventListener listener = null;
            if (_dic.TryGetValue(strType, out listener))
                listener.Remove(callback);
        }

        public void RemoveEventListeners()
        {
            RemoveEventListeners(null);
        }

        public void RemoveEventListeners(string strType)
        {
            if (_dic == null)
                return;

            if (strType != null)
            {
                EventListener listener;
                if (_dic.TryGetValue(strType, out listener))
                    listener.Clear();
            }
            else
            {
                foreach (KeyValuePair<string, EventListener> kv in _dic)
                    kv.Value.Clear();
            }
        }

        public EventListener GetEventListener(string strType)
        {
            if (_dic == null)
                return null;

            EventListener listener = null;
            _dic.TryGetValue(strType, out listener);
            return listener;
        }

        internal void RegisterListener(EventListener listener)
        {
            if (_dic == null)
                _dic = new Dictionary<string, EventListener>();

            _dic[listener.type] = listener;
        }

        public bool DispatchEvent(string strType)
        {
            return DispatchEvent(strType, null);
        }

        public bool DispatchEvent(string strType, object data)
        {
            return InternalDispatchEvent(strType, GetEventListener(strType), data);
        }

        internal bool InternalDispatchEvent(string strType, EventListener listener, object data)
        {
            EventListener gListener = null;
            if ((this is DisplayObject) && ((DisplayObject)this).gOwner != null)
                gListener = ((DisplayObject)this).gOwner.GetEventListener(strType);

            bool b1 = listener != null && !listener.isEmpty;
            bool b2 = gListener != null && !gListener.isEmpty;
            if (b1 || b2)
            {
                EventContext context = EventContext.Get();
                context.initiator = this;
                context._stopsPropagation = false;
                context._defaultPrevented = false;
                context.type = strType;
                context.data = data;

                if (b1)
                {
                    listener.CallCaptureInternal(context);
                    listener.CallInternal(context);
                }

                if (b2)
                {
                    gListener.CallCaptureInternal(context);
                    gListener.CallInternal(context);
                }

                EventContext.Return(context);
                context.initiator = null;
                context.sender = null;
                context.data = null;

                return context._defaultPrevented;
            }
            else
                return false;
        }

        public bool DispatchEvent(EventContext context)
        {
            EventListener listener = GetEventListener(context.type);
            EventListener gListener = null;
            if ((this is DisplayObject) && ((DisplayObject)this).gOwner != null)
                gListener = ((DisplayObject)this).gOwner.GetEventListener(context.type);

            EventDispatcher savedSender = context.sender;

            if (listener != null && !listener.isEmpty)
            {
                listener.CallCaptureInternal(context);
                listener.CallInternal(context);
            }

            if (gListener != null && !gListener.isEmpty)
            {
                gListener.CallCaptureInternal(context);
                gListener.CallInternal(context);
            }

            context.sender = savedSender;
            return context._defaultPrevented;
        }

        public bool BubbleEvent(string strType, object data)
        {
            EventContext context = EventContext.Get();
            context.initiator = this;
            context._stopsPropagation = false;
            context._defaultPrevented = false;
            context.type = strType;
            context.data = data;
            List<EventListener> bubbleChain = context.callChain;

            EventListener listener = GetEventListener(strType);
            if (listener != null && !listener.isEmpty)
                bubbleChain.Add(listener);

            if ((this is DisplayObject) && ((DisplayObject)this).gOwner != null)
            {
                listener = ((DisplayObject)this).gOwner.GetEventListener(strType);
                if (listener != null && !listener.isEmpty)
                    bubbleChain.Add(listener);
            }

            if (this is DisplayObject)
            {
                DisplayObject element = (DisplayObject)this;
                while ((element = element.parent) != null)
                {
                    listener = element.GetEventListener(strType);
                    if (listener != null && !listener.isEmpty)
                        bubbleChain.Add(listener);

                    if (element.gOwner != null)
                    {
                        listener = element.gOwner.GetEventListener(strType);
                        if (listener != null && !listener.isEmpty)
                            bubbleChain.Add(listener);
                    }
                }
            }
            else if (this is GObject)
            {
                GObject element = (GObject)this;
                while ((element = element.parent) != null)
                {
                    listener = element.GetEventListener(strType);
                    if (listener != null && !listener.isEmpty)
                        bubbleChain.Add(listener);
                }
            }

            int length = bubbleChain.Count;
            for (int i = length - 1; i >= 0; i--)
                bubbleChain[i].CallCaptureInternal(context);

            for (int i = 0; i < length; ++i)
            {
                bubbleChain[i].CallInternal(context);
                if (context._stopsPropagation)
                    break;
            }

            bubbleChain.Clear();
            EventContext.Return(context);
            context.initiator = null;
            context.sender = null;
            context.data = null;
            return context._defaultPrevented;
        }

        public bool BroadcastEvent(string strType, object data)
        {
            EventContext context = EventContext.Get();
            context.initiator = this;
            context._stopsPropagation = false;
            context._defaultPrevented = false;
            context.type = strType;
            context.data = data;
            List<EventListener> bubbleChain = context.callChain;

            if (this is Container)
                GetChildEventListeners(strType, (Container)this, bubbleChain);
            else if (this is GComponent)
                GetChildEventListeners(strType, (GComponent)this, bubbleChain);

            int length = bubbleChain.Count;
            for (int i = 0; i < length; ++i)
                bubbleChain[i].CallInternal(context);

            bubbleChain.Clear();
            EventContext.Return(context);
            context.initiator = null;
            context.sender = null;
            context.data = null;
            return context._defaultPrevented;
        }

        static void GetChildEventListeners(string strType, Container container, List<EventListener> listeners)
        {
            EventListener listener = container.GetEventListener(strType);
            if (listener != null)
                listeners.Add(listener);
            if (container.gOwner != null)
            {
                listener = container.gOwner.GetEventListener(strType);
                if (listener != null && !listener.isEmpty)
                    listeners.Add(listener);
            }

            int count = container.numChildren;
            for (int i = 0; i < count; ++i)
            {
                DisplayObject obj = container.GetChildAt(i);
                if (obj is Container)
                    GetChildEventListeners(strType, (Container)obj, listeners);
                else
                {
                    listener = obj.GetEventListener(strType);
                    if (listener != null && !listener.isEmpty)
                        listeners.Add(listener);

                    if (obj.gOwner != null)
                    {
                        listener = obj.gOwner.GetEventListener(strType);
                        if (listener != null && !listener.isEmpty)
                            listeners.Add(listener);
                    }
                }
            }
        }

        static void GetChildEventListeners(string strType, GComponent container, List<EventListener> listeners)
        {
            EventListener listener = container.GetEventListener(strType);
            if (listener != null)
                listeners.Add(listener);

            int count = container.numChildren;
            for (int i = 0; i < count; ++i)
            {
                GObject obj = container.GetChildAt(i);
                if (obj is GComponent)
                    GetChildEventListeners(strType, (GComponent)obj, listeners);
                else
                {
                    listener = obj.GetEventListener(strType);
                    if (listener != null)
                        listeners.Add(listener);
                }
            }
        }
    }
}
