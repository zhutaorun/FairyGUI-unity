using System.Collections.Generic;

namespace FairyGUI
{
    public class EventContext
    {
        public EventDispatcher sender { get; internal set; }
        public object initiator { get; internal set; }
        public string type;
        public object data;

        internal bool _defaultPrevented;
        internal bool _stopsPropagation;

        internal List<EventListener> callChain = new List<EventListener>();

        public void StopPropagation()
        {
            _stopsPropagation = true;
        }

        public void PreventDefault()
        {
            _defaultPrevented = true;
        }

        public bool isDefaultPrevented
        {
            get { return _defaultPrevented; }
        }

        public InputEvent inputEvent
        {
            get { return (InputEvent)data; }
        }

        static Stack<EventContext> pool = new Stack<EventContext>();
        internal static EventContext Get()
        {
            if (pool.Count > 0)
                return pool.Pop();
            else
                return new EventContext();
        }

        internal static void Return(EventContext value)
        {
            pool.Push(value);
        }
    }

}
