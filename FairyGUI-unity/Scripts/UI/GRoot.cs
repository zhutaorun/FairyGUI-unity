using UnityEngine;
using System.Collections.Generic;

namespace FairyGUI
{
    public class GRoot : GComponent
    {
        public static GRoot inst { get; private set; }
        public static Stage nativeStage { get; private set; }
        public static float contentScaleFactor = 1;

        public EventListener onFocusChanged { get; private set; }

        GGraph _modalLayer;
        GObject _modalWaitPane;
        List<GObject> _popupStack;
        List<GObject> _justClosedPopups;
        GObject _focusedObject;
        GObject _tooltipWin;
        GObject _defaultTooltipWin;
        bool _focusManagement;

        public static GRoot Instantiate()
        {
            GRoot r = new GRoot();
            if (inst == null)
                inst = r;

            Stage.inst.AddChild(r.displayObject);
            return r;
        }

        public GRoot()
        {
            inst = this;

            this.name = "GRoot";
            this.rootContainer.gameObject.name = "GRoot";
            nativeStage = Stage.inst;

            if (RichTextField.objectFactory == null)
                RichTextField.objectFactory = new RichTextObjectFactory();

            _popupStack = new List<GObject>();
            _justClosedPopups = new List<GObject>();

            onFocusChanged = new EventListener(this, "onFocusChanged");

            displayObject.onAddedToStage.Add(__addedToStage);
        }

        public void SetContentScaleFactor(int designUIWidth, int designUIHeight)
        {
            int w = nativeStage.stageWidth;
            int h = nativeStage.stageHeight;
            if (designUIWidth > 0 && designUIHeight > 0)
            {
                float s1 = (float)w / designUIWidth;
                float s2 = (float)h / designUIHeight;
                contentScaleFactor = Mathf.Min(s1, s2);
            }
            else if (designUIWidth > 0)
                contentScaleFactor = (float)w / designUIWidth;
            else if (designUIHeight > 0)
                contentScaleFactor = (float)h / designUIHeight;
            else
                contentScaleFactor = 1;

            this.SetSize(Mathf.RoundToInt(w / contentScaleFactor), Mathf.RoundToInt(h / contentScaleFactor));
        }

        public void EnableFocusManagement()
        {
            _focusManagement = true;
        }

        public void ShowWindow(Window win)
        {
            AddChild(win);
            win.RequestFocus();

            if (win.x > this.width)
                win.x = this.width - win.width;
            else if (win.x + win.width < 0)
                win.x = 0;
            if (win.y > this.height)
                win.y = this.height - win.height;
            else if (win.y + win.height < 0)
                win.y = 0;

            AdjustModalLayer();
        }

        public void HideWindow(Window win)
        {
            win.Hide();
        }

        public void HideWindowImmediately(Window win)
        {
            HideWindowImmediately(win, false);
        }

        public void HideWindowImmediately(Window win, bool dispose)
        {
            if (win.parent == this)
                RemoveChild(win, dispose);
            else if (dispose)
                win.Dispose();

            AdjustModalLayer();
        }

        public void ShowModalWait()
        {
            if (UIConfig.globalModalWaiting != null)
            {
                if (_modalWaitPane == null)
                    _modalWaitPane = UIPackage.CreateObjectFromURL(UIConfig.globalModalWaiting);
                _modalWaitPane.SetSize(this.width, this.height);
                _modalWaitPane.AddRelation(this, RelationType.Size);

                AddChild(_modalWaitPane);
            }
        }

        public void CloseModalWait()
        {
            if (_modalWaitPane != null && _modalWaitPane.parent != null)
                RemoveChild(_modalWaitPane);
        }

        public void CloseAllExceptModals()
        {
            GObject[] arr = _children.ToArray();
            int cnt = arr.Length;
            foreach (GObject g in arr)
            {
                if ((g is Window) && !(g as Window).modal)
                    HideWindowImmediately(g as Window);
            }
        }

        public void CloseAllWindows()
        {
            GObject[] arr = _children.ToArray();
            int cnt = arr.Length;
            foreach (GObject g in arr)
            {
                if (g is Window)
                    HideWindowImmediately(g as Window);
            }
        }

        public Window GetTopWindow()
        {
            int cnt = this.numChildren;
            for (int i = cnt - 1; i >= 0; i--)
            {
                GObject g = this.GetChildAt(i);
                if (g is Window)
                {
                    return (Window)(g);
                }
            }

            return null;
        }

        public bool hasModalWindow
        {
            get { return _modalLayer.parent != null; }
        }

        public bool modalWaiting
        {
            get
            {
                return (_modalWaitPane != null) && _modalWaitPane.onStage;
            }
        }

        public GObject objectUnderMouse
        {
            get
            {
                return DisplayObjectToGObject(nativeStage.objectUnderMouse);
            }
        }

        public GObject DisplayObjectToGObject(DisplayObject obj)
        {
            while (obj != null && obj != nativeStage)
            {
                if (obj.gOwner != null)
                    return obj.gOwner;

                obj = obj.parent;
            }
            return null;
        }

        private void AdjustModalLayer()
        {
            int cnt = this.numChildren;

            if (_modalWaitPane != null && _modalWaitPane.parent != null)
                SetChildIndex(_modalWaitPane, cnt - 1);

            for (int i = cnt - 1; i >= 0; i--)
            {
                GObject g = this.GetChildAt(i);
                if (g != _modalLayer && (g is Window) && (g as Window).modal)
                {
                    if (_modalLayer.parent == null)
                        AddChildAt(_modalLayer, i);
                    else if (i > 0)
                        SetChildIndex(_modalLayer, i - 1);
                    else
                        AddChildAt(_modalLayer, 0);
                    return;
                }
            }

            if (_modalLayer.parent != null)
                RemoveChild(_modalLayer);
        }

        public void ShowPopup(GObject popup)
        {
            ShowPopup(popup, null, null);
        }

        public void ShowPopup(GObject popup, GObject target)
        {
            ShowPopup(popup, target, null);
        }

        public void ShowPopup(GObject popup, GObject target, object downward)
        {
            if (_popupStack.Count > 0)
            {
                int k = _popupStack.IndexOf(popup);
                if (k != -1)
                {
                    for (int i = _popupStack.Count - 1; i >= k; i--)
                    {
                        int last = _popupStack.Count - 1;
                        ClosePopup(_popupStack[last]);
                        _popupStack.RemoveAt(last);
                    }
                }
            }
            _popupStack.Add(popup);

            AddChild(popup);
            AdjustModalLayer();

            if ((popup is Window) && target == null && downward == null)
                return;

            Vector2 pos = GetPoupPosition(popup, target, downward);
            popup.x = pos.x;
            popup.y = pos.y;
        }

        public Vector2 GetPoupPosition(GObject popup, GObject target, object downward)
        {
            Vector2 pos;
            float sizeW = 0;
            float sizeH = 0;
            if (target != null)
            {
                pos = target.LocalToGlobal(new Vector2(0, 0));
                sizeW = target.width;
                sizeH = target.height;
            }
            else
            {
                pos = new Vector2(Stage.mouseX / contentScaleFactor, Stage.mouseY / contentScaleFactor);
            }
            float xx, yy;
            xx = pos.x;
            if (xx + popup.width > this.width)
                xx = xx + sizeW - popup.width;
            yy = pos.y + sizeH;
            if ((downward == null && yy + popup.height > this.height)
                || downward != null && (bool)downward == false)
            {
                yy = pos.y - popup.height - 1;
                if (yy < 0)
                {
                    yy = 0;
                    xx += sizeW / 2;
                }
            }

            return new Vector2(Mathf.RoundToInt(xx), Mathf.RoundToInt(yy));
        }

        public void TogglePopup(GObject popup)
        {
            TogglePopup(popup, null, null);
        }

        public void TogglePopup(GObject popup, GObject target)
        {
            TogglePopup(popup, target, null);
        }

        public void TogglePopup(GObject popup, GObject target, object downward)
        {
            if (_justClosedPopups.IndexOf(popup) != -1)
                return;

            ShowPopup(popup, target, downward);
        }

        public void HidePopup()
        {
            HidePopup(null);
        }

        public void HidePopup(GObject popup)
        {
            if (popup != null)
            {
                int k = _popupStack.IndexOf(popup);
                if (k != -1)
                {
                    for (int i = _popupStack.Count - 1; i >= k; i--)
                    {
                        int last = _popupStack.Count - 1;
                        ClosePopup(_popupStack[last]);
                        _popupStack.RemoveAt(last);
                    }
                }
            }
            else
            {
                foreach (GObject obj in _popupStack)
                    ClosePopup(obj);
                _popupStack.Clear();
            }
        }

        public bool hasAnyPopup
        {
            get { return _popupStack.Count > 0; }
        }

        void ClosePopup(GObject target)
        {
            if (target.parent != null)
            {
                if (target is Window)
                    ((Window)target).Hide();
                else
                    RemoveChild(target);
            }
        }

        public void ShowTooltips(string msg)
        {
            if (_defaultTooltipWin == null)
            {
                string resourceURL = UIConfig.tooltipsWin;
                if (string.IsNullOrEmpty(resourceURL))
                {
                    Debug.LogError("FairyGUI: UIConfig.tooltipsWin not defined");
                    return;
                }

                _defaultTooltipWin = UIPackage.CreateObjectFromURL(resourceURL);
            }

            _defaultTooltipWin.text = msg;
            ShowTooltipsWin(_defaultTooltipWin);
        }

        public void ShowTooltipsWin(GObject tooltipWin)
        {
            HideTooltips();

            _tooltipWin = tooltipWin;
            Timers.inst.Add(0.1f, 1, __showTooltipsWin);
        }

        void __showTooltipsWin(object param)
        {
            if (_tooltipWin == null)
                return;

            float xx = (Stage.mouseX + 10) / contentScaleFactor;
            float yy = (Stage.mouseY + 20) / contentScaleFactor;

            if (xx + _tooltipWin.width > this.width)
                xx = xx - _tooltipWin.width;
            if (yy + _tooltipWin.height > this.height)
            {
                yy = yy - _tooltipWin.height - 1;
                if (yy < 0)
                    yy = 0;
            }

            _tooltipWin.x = Mathf.RoundToInt(xx);
            _tooltipWin.y = Mathf.RoundToInt(yy);
            AddChild(_tooltipWin);
        }

        public void HideTooltips()
        {
            if (_tooltipWin != null)
            {
                if (_tooltipWin.parent != null)
                    RemoveChild(_tooltipWin);
                _tooltipWin = null;
            }
        }

        public GObject focus
        {
            get
            {
                if (_focusedObject != null && !_focusedObject.onStage)
                    _focusedObject = null;

                return _focusedObject;
            }

            set
            {
                if (!_focusManagement)
                    return;

                if (value != null && (!value.focusable || !value.onStage))
                {
                    Debug.LogError("invalid focus target");
                    return;
                }

                if (_focusedObject != value)
                {
                    GObject old = null;
                    if (_focusedObject != null && _focusedObject.onStage)
                        old = _focusedObject;
                    _focusedObject = value;
                    onFocusChanged.Call(old);
                }
            }
        }

        void __addedToStage()
        {
            nativeStage.onStageResized.Add(__winResize);
            nativeStage.onMouseDown.AddCapture(__stageMouseDown);

            __winResize();

            _modalLayer = new GGraph();
            _modalLayer.DrawRect(this.width, this.height, 0, Color.white, UIConfig.modalLayerColor);
            _modalLayer.shape.name = "test";
            _modalLayer.AddRelation(this, RelationType.Size);
        }

        void __stageMouseDown(EventContext context)
        {
            if (_focusManagement)
            {
                DisplayObject mc = nativeStage.objectUnderMouse as DisplayObject;
                while (mc != nativeStage && mc != null)
                {
                    GObject gg = mc.gOwner;
                    if (gg != null && gg.touchable && gg.focusable)
                    {
                        this.focus = gg;
                        break;
                    }
                    mc = mc.parent;
                }
            }

            if (_tooltipWin != null)
                HideTooltips();

            CheckPopups();
        }

        void CheckPopups()
        {
            _justClosedPopups.Clear();
            if (_popupStack.Count > 0)
            {
                DisplayObject mc = nativeStage.objectUnderMouse as DisplayObject;
                bool handled = false;
                while (mc != nativeStage && mc != null)
                {
                    if (mc.gOwner != null)
                    {
                        int k = _popupStack.IndexOf(mc.gOwner);
                        if (k != -1)
                        {
                            for (int i = _popupStack.Count - 1; i > k; i--)
                            {
                                int last = _popupStack.Count - 1;
                                GObject popup = _popupStack[last];
                                ClosePopup(popup);
                                _justClosedPopups.Add(popup);
                                _popupStack.RemoveAt(last);
                            }
                            handled = true;
                            break;
                        }
                    }
                    mc = mc.parent;
                }

                if (!handled)
                {
                    for (int i = _popupStack.Count - 1; i >= 0; i--)
                    {
                        GObject popup = _popupStack[i];
                        ClosePopup(popup);
                        _justClosedPopups.Add(popup);
                    }
                    _popupStack.Clear();
                }
            }
        }

        void __winResize()
        {
            this.SetSize(Mathf.CeilToInt(nativeStage.stageWidth / contentScaleFactor), Mathf.CeilToInt(nativeStage.stageHeight / contentScaleFactor));
        }

        public void EnableSound()
        {
            nativeStage.EnableSound();
        }

        public void DisableSound()
        {
            nativeStage.DisableSound();
        }

        public void PlayOneShotSound(AudioClip clip, float volumeScale)
        {
            nativeStage.PlayOneShotSound(clip, volumeScale);
        }

        public void PlayOneShotSound(AudioClip clip)
        {
            nativeStage.PlayOneShotSound(clip);
        }

        public float soundVolume
        {
            get { return nativeStage.soundVolume; }
            set { nativeStage.soundVolume = value; }
        }
    }
}
