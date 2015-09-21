using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

public class DragManager
{
    private GLoader _agent;
    private object _sourceData;

    public const string DROP_EVENT = "__drop";
    private static DragManager _inst;
    public static DragManager inst
    {
        get
        {
            if (_inst == null)
                _inst = new DragManager();
            return _inst;
        }
    }

    public DragManager()
    {
        _agent = new GLoader();
        _agent.touchable = false;//important
        _agent.draggable = true;
        _agent.SetSize(88, 88);
        _agent.alwaysOnTop = int.MaxValue;
        _agent.onDragEnd.Add(__dragEnd);
    }

    public GObject dragAgent
    {
        get { return _agent; }
    }

    public bool dragging
    {
        get { return _agent.parent != null; }
    }

    public void StartDrag(GObject source, string icon, object sourceData, int touchPointID = -1)
    {
        if (_agent.parent != null)
            return;

        _sourceData = sourceData;
        _agent.url = icon;
        GRoot.inst.AddChild(_agent);
        Vector2 pt = source.LocalToGlobal(new Vector2(0, 0));
        _agent.SetXY(pt.x, pt.y);
        _agent.StartDrag(null, touchPointID);
    }

    public void Cancel()
    {
        if (_agent.parent != null)
        {
            _agent.StopDrag();
            GRoot.inst.RemoveChild(_agent);
            _sourceData = null;
        }
    }

    private void __dragEnd(EventContext evt)
    {
        if (_agent.parent == null) //cancelled
            return;

        GRoot.inst.RemoveChild(_agent);

        object sourceData = _sourceData;
        _sourceData = null;

        GObject obj = GRoot.inst.objectUnderMouse;
        while (obj != null)
        {
            EventListener listener = obj.GetEventListener(DROP_EVENT);
            if (listener != null)
            {
                obj.RequestFocus();
                listener.Call(sourceData);
                return;
            }

            obj = obj.parent;
        }
    }
}