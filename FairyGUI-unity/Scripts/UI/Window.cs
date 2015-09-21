using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    public class Window : GComponent
    {
        GComponent _frame;
        GComponent _contentPane;
        GObject _modalWaitPane;
        GObject _closeButton;
        GObject _dragArea;
        GObject _contentArea;
        bool _modal;

        List<IUISource> _uiSources;
        bool _inited;
        bool _loading;

        protected int _requestingCmd;

        public Window()
            : base()
        {
            _uiSources = new List<IUISource>();
            this.focusable = true;

            displayObject.onAddedToStage.Add(__onShown);
            displayObject.onRemovedFromStage.Add(__onHide);
            displayObject.onMouseDown.AddCapture(__mouseDown);

            this.rootContainer.gameObject.name = "Window";
        }

        public void AddUISource(IUISource source)
        {
            _uiSources.Add(source);
        }

        public GComponent contentPane
        {
            set
            {
                if (_contentPane != value)
                {
                    if (_contentPane != null)
                        RemoveChild(_contentPane);
                    _contentPane = value;
                    if (_contentPane != null)
                    {
                        AddChild(_contentPane);
                        this.SetSize(_contentPane.width, _contentPane.height);
                        _contentPane.AddRelation(this, RelationType.Size);
                        _contentPane.fairyBatching = true;
                        _frame = _contentPane.GetChild("frame") as GComponent;
                        if (_frame != null)
                        {
                            this.closeButton = _frame.GetChild("closeButton");
                            this.dragArea = _frame.GetChild("dragArea");
                            this.contentArea = _frame.GetChild("contentArea");
                        }
                    }
                    else
                        _frame = null;
                }
            }
            get
            {
                return _contentPane;
            }
        }

        public GComponent frame
        {
            get { return _frame; }
        }

        public GObject closeButton
        {
            get { return _closeButton; }
            set
            {
                if (_closeButton != null)
                    _closeButton.onClick.Remove(closeEventHandler);
                _closeButton = value;
                if (_closeButton != null)
                    _closeButton.onClick.Add(closeEventHandler);
            }
        }

        public GObject dragArea
        {
            get { return _dragArea; }
            set
            {
                if (_dragArea != value)
                {
                    if (_dragArea != null)
                    {
                        _dragArea.draggable = false;
                        _dragArea.onDragStart.Remove(__dragStart);
                    }

                    _dragArea = value;
                    if (_dragArea != null)
                    {
                        if ((_dragArea is GGraph) && ((GGraph)_dragArea).displayObject == null)
                            ((GGraph)_dragArea).DrawRect(_dragArea.width, _dragArea.height, 0, Color.clear, Color.clear);
                        _dragArea.draggable = true;
                        _dragArea.onDragStart.Add(__dragStart);
                    }
                }
            }
        }

        public GObject contentArea
        {
            get { return _contentArea; }
            set { _contentArea = value; }
        }

        public GObject modalWaitingPane
        {
            get { return _modalWaitPane; }
        }

        public void Show()
        {
            GRoot.inst.ShowWindow(this);
        }

        public void showOn(GRoot r)
        {
            r.ShowWindow(this);
        }

        public void Hide()
        {
            if (this.isShowing)
                DoHideAnimation();
        }

        public void HideImmediately()
        {
            GRoot r = (parent is GRoot) ? (GRoot)parent : null;
            if (r == null)
                r = GRoot.inst;
            r.HideWindowImmediately(this);
        }

        public void CenterOn(GRoot r, bool restraint)
        {
            this.SetXY((int)((r.width - this.width) / 2), (int)((r.height - this.height) / 2));
            if (restraint)
            {
                this.AddRelation(r, RelationType.Center_Center);
                this.AddRelation(r, RelationType.Middle_Middle);
            }
        }

        public void ToggleStatus()
        {
            if (isTop)
                Hide();
            else
                Show();
        }

        public bool isShowing
        {
            get { return parent != null; }
        }

        public bool isTop
        {
            get { return parent != null && parent.GetChildIndex(this) == parent.numChildren - 1; }
        }

        public bool modal
        {
            get { return _modal; }
            set { _modal = value; }
        }

        public void BringToFront()
        {
            GRoot r = this.root;
            if (r == null)
                r = GRoot.inst;
            r.ShowWindow(this);
        }

        public void ShowModalWait()
        {
            ShowModalWait(0);
        }

        public void ShowModalWait(int requestingCmd)
        {
            if (requestingCmd != 0)
                _requestingCmd = requestingCmd;

            if (UIConfig.windowModalWaiting != null)
            {
                if (_modalWaitPane == null)
                    _modalWaitPane = UIPackage.CreateObjectFromURL(UIConfig.windowModalWaiting);

                LayoutModalWaitPane();

                AddChild(_modalWaitPane);
            }
        }

        virtual protected void LayoutModalWaitPane()
        {
            if (_contentArea != null)
            {
                Vector2 pt = _frame.LocalToGlobal(new Vector2(0, 0));
                pt = this.GlobalToLocal(pt);
                _modalWaitPane.SetXY((int)pt.x + _contentArea.x, (int)pt.y + _contentArea.y);
                _modalWaitPane.SetSize(_contentArea.width, _contentArea.height);
            }
            else
                _modalWaitPane.SetSize(this.width, this.height);
        }

        public bool CloseModalWait()
        {
            return CloseModalWait(0);
        }

        public bool CloseModalWait(int requestingCmd)
        {
            if (requestingCmd != 0)
            {
                if (_requestingCmd != requestingCmd)
                    return false;
            }
            _requestingCmd = 0;

            if (_modalWaitPane != null && _modalWaitPane.parent != null)
                RemoveChild(_modalWaitPane);

            return true;
        }

        public bool modalWaiting
        {
            get { return (_modalWaitPane != null) && _modalWaitPane.inContainer; }
        }

        public void Init()
        {
            if (_inited || _loading)
                return;

            if (_uiSources.Count > 0)
            {
                _loading = false;
                int cnt = _uiSources.Count;
                for (int i = 0; i < cnt; i++)
                {
                    IUISource lib = _uiSources[i];
                    if (!lib.loaded)
                    {
                        lib.Load(__uiLoadComplete);
                        _loading = true;
                    }
                }

                if (!_loading)
                    _init();
            }
            else
                _init();
        }

        virtual protected void OnInit()
        {
        }

        virtual protected void OnShown()
        {
        }

        virtual protected void OnHide()
        {
        }

        virtual protected void DoShowAnimation()
        {
            OnShown();
        }

        virtual protected void DoHideAnimation()
        {
            this.HideImmediately();
        }

        void __uiLoadComplete()
        {
            int cnt = _uiSources.Count;
            for (int i = 0; i < cnt; i++)
            {
                IUISource lib = _uiSources[i];
                if (!lib.loaded)
                    return;
            }

            _loading = false;
            _init();
        }

        void _init()
        {
            _inited = true;
            OnInit();

            if (this.isShowing)
                DoShowAnimation();
        }

        override public void Dispose()
        {
            if (displayObject != null)
                displayObject.RemoveEventListeners();
            if (parent != null)
                this.HideImmediately();

            base.Dispose();
        }

        protected void closeEventHandler()
        {
            Hide();
        }

        void __onShown()
        {
            if (!_inited)
                Init();
            else
                DoShowAnimation();
        }

        void __onHide()
        {
            CloseModalWait();
            OnHide();
        }

        private void __mouseDown(EventContext context)
        {
            if (this.isShowing)
            {
                BringToFront();
            }
        }

        private void __dragStart(EventContext context)
        {
            context.PreventDefault();

            this.StartDrag(null);
        }
    }
}
