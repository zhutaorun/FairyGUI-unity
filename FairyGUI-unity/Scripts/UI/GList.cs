using System;
using System.Collections.Generic;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    public class GList : GComponent
    {
        public string defaultItem;
        public bool autoResizeItem;
        public ListSelectionMode selectionMode;

        public EventListener onClickItem { get; private set; }

        ListLayoutType _layout;
        int _lineGap;
        int _columnGap;
        GObjectPool _pool;
        bool _selectionHandled;
        int _lastSelectedIndex;

        public GList()
            : base()
        {
            _pool = new GObjectPool();
            _trackBounds = true;
            autoResizeItem = true;

            onClickItem = new EventListener(this, "onClickItem");
        }

        public override void Dispose()
        {
            _pool.Clear();
            base.Dispose();
        }

        public ListLayoutType layout
        {
            get { return _layout; }
            set
            {
                if (_layout != value)
                {
                    _layout = value;
                    SetBoundsChangedFlag();
                }
            }
        }

        public int lineGap
        {
            get { return _lineGap; }
            set
            {
                if (_lineGap != value)
                {
                    _lineGap = value;
                    SetBoundsChangedFlag();
                }
            }
        }

        public int columnGap
        {
            get { return _columnGap; }
            set
            {
                if (_columnGap != value)
                {
                    _columnGap = value;
                    SetBoundsChangedFlag();
                }
            }
        }

        public GObjectPool itemPool
        {
            get { return _pool; }
        }

        public GObject GetFromPool(string url)
        {
            if (string.IsNullOrEmpty(url))
                url = defaultItem;

            return _pool.GetObject(url);
        }

        void ReturnToPool(GObject obj)
        {
            _pool.ReturnObject(obj);
        }

        public GObject AddItemFromPool()
        {
            GObject obj = GetFromPool(null);

            return AddChild(obj);
        }

        public GObject AddItemFromPool(string url)
        {
            GObject obj = GetFromPool(url);

            return AddChild(obj);
        }

        override public GObject AddChildAt(GObject child, int index)
        {
            if (autoResizeItem)
            {
                if (_layout == ListLayoutType.SingleColumn)
                    child.width = this.ViewWidth;
                else if (_layout == ListLayoutType.SingleRow)
                    child.height = this.ViewHeight;
            }

            base.AddChildAt(child, index);
            if (child is GButton)
            {
                GButton button = (GButton)child;
                button.selected = false;
                button.changeStateOnClick = false;
            }

            child.onMouseDown.Add(__mouseDownItem);
            child.onClick.Add(__clickItem);

            return child;
        }

        override public GObject RemoveChildAt(int index, bool dispose)
        {
            GObject child = base.RemoveChildAt(index, dispose);
            child.onMouseDown.Remove(__mouseDownItem);
            child.onClick.Remove(__clickItem);

            return child;
        }

        public void RemoveChildToPoolAt(int index)
        {
            GObject child = base.RemoveChildAt(index);
            ReturnToPool(child);
        }

        public void RemoveChildToPool(GObject child)
        {
            base.RemoveChild(child);
            ReturnToPool(child);
        }

        public void RemoveChildrenToPool()
        {
            RemoveChildrenToPool(0, -1);
        }

        public void RemoveChildrenToPool(int beginIndex, int endIndex)
        {
            if (endIndex < 0 || endIndex >= numChildren)
                endIndex = numChildren - 1;

            for (int i = beginIndex; i <= endIndex; ++i)
                RemoveChildToPoolAt(beginIndex);
        }

        public int selectedIndex
        {
            get
            {
                int cnt = _children.Count;
                for (int i = 0; i < cnt; i++)
                {
                    GButton obj = _children[i].asButton;
                    if (obj != null && obj.selected)
                        return i;
                }
                return -1;
            }

            set
            {
                ClearSelection();
                if (value >= 0 && value < _children.Count)
                    AddSelection(value, false);
            }
        }

        public List<int> GetSelection()
        {
            List<int> ret = new List<int>();
            int cnt = _children.Count;
            for (int i = 0; i < cnt; i++)
            {
                GButton obj = _children[i].asButton;
                if (obj != null && obj.selected)
                    ret.Add(i);
            }
            return ret;
        }

        public void AddSelection(int index, bool scrollItToView)
        {
            if (selectionMode == ListSelectionMode.None)
                return;

            if (selectionMode == ListSelectionMode.Single)
                ClearSelection();

            GButton obj = GetChildAt(index).asButton;
            if (obj != null)
            {
                if (!obj.selected)
                    obj.selected = true;

                if (scrollItToView & scrollPane != null)
                    scrollPane.ScrollToView(obj);
            }
        }

        public void RemoveSelection(int index)
        {
            if (selectionMode == ListSelectionMode.None)
                return;

            GButton obj = GetChildAt(index).asButton;
            if (obj != null && obj.selected)
                obj.selected = false;
        }

        public void ClearSelection()
        {
            int cnt = _children.Count;
            for (int i = 0; i < cnt; i++)
            {
                GButton obj = _children[i].asButton;
                if (obj != null)
                    obj.selected = false;
            }
        }

        public void SelectAll()
        {
            int cnt = _children.Count;
            for (int i = 0; i < cnt; i++)
            {
                GButton obj = _children[i].asButton;
                if (obj != null)
                    obj.selected = true;
            }
        }

        public void SelectNone()
        {
            int cnt = _children.Count;
            for (int i = 0; i < cnt; i++)
            {
                GButton obj = _children[i].asButton;
                if (obj != null)
                    obj.selected = false;
            }
        }

        public void SelectReverse()
        {
            int cnt = _children.Count;
            for (int i = 0; i < cnt; i++)
            {
                GButton obj = _children[i].asButton;
                if (obj != null)
                    obj.selected = !obj.selected;
            }
        }

        public void HandleArrowKey(int dir)
        {
            int index = this.selectedIndex;
            if (index == -1)
                return;

            switch (dir)
            {
                case 1://up
                    if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowVertical)
                    {
                        index--;
                        if (index >= 0)
                        {
                            ClearSelection();
                            AddSelection(index, true);
                        }
                    }
                    else if (_layout == ListLayoutType.FlowHorizontal)
                    {
                        GObject current = _children[index];
                        int k = 0;
                        int i;
                        for (i = index - 1; i >= 0; i--)
                        {
                            GObject obj = _children[i];
                            if (obj.y != current.y)
                            {
                                current = obj;
                                break;
                            }
                            k++;
                        }
                        for (; i >= 0; i--)
                        {
                            GObject obj = _children[i];
                            if (obj.y != current.y)
                            {
                                ClearSelection();
                                AddSelection(i + k + 1, true);
                                break;
                            }
                        }
                    }
                    break;

                case 3://right
                    if (_layout == ListLayoutType.SingleRow || _layout == ListLayoutType.FlowHorizontal)
                    {
                        index++;
                        if (index < _children.Count)
                        {
                            ClearSelection();
                            AddSelection(index, true);
                        }
                    }
                    else if (_layout == ListLayoutType.FlowVertical)
                    {
                        GObject current = _children[index];
                        int k = 0;
                        int cnt = _children.Count;
                        int i;
                        for (i = index + 1; i < cnt; i++)
                        {
                            GObject obj = _children[i];
                            if (obj.x != current.x)
                            {
                                current = obj;
                                break;
                            }
                            k++;
                        }
                        for (; i < cnt; i++)
                        {
                            GObject obj = _children[i];
                            if (obj.x != current.x)
                            {
                                ClearSelection();
                                AddSelection(i - k - 1, true);
                                break;
                            }
                        }
                    }
                    break;

                case 5://down
                    if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowVertical)
                    {
                        index++;
                        if (index < _children.Count)
                        {
                            ClearSelection();
                            AddSelection(index, true);
                        }
                    }
                    else if (_layout == ListLayoutType.FlowHorizontal)
                    {
                        GObject current = _children[index];
                        int k = 0;
                        int cnt = _children.Count;
                        int i;
                        for (i = index + 1; i < cnt; i++)
                        {
                            GObject obj = _children[i];
                            if (obj.y != current.y)
                            {
                                current = obj;
                                break;
                            }
                            k++;
                        }
                        for (; i < cnt; i++)
                        {
                            GObject obj = _children[i];
                            if (obj.y != current.y)
                            {
                                ClearSelection();
                                AddSelection(i - k - 1, true);
                                break;
                            }
                        }
                    }
                    break;

                case 7://left
                    if (_layout == ListLayoutType.SingleRow || _layout == ListLayoutType.FlowHorizontal)
                    {
                        index--;
                        if (index >= 0)
                        {
                            ClearSelection();
                            AddSelection(index, true);
                        }
                    }
                    else if (_layout == ListLayoutType.FlowVertical)
                    {
                        GObject current = _children[index];
                        int k = 0;
                        int i;
                        for (i = index - 1; i >= 0; i--)
                        {
                            GObject obj = _children[i];
                            if (obj.x != current.x)
                            {
                                current = obj;
                                break;
                            }
                            k++;
                        }
                        for (; i >= 0; i--)
                        {
                            GObject obj = _children[i];
                            if (obj.x != current.x)
                            {
                                ClearSelection();
                                AddSelection(i + k + 1, true);
                                break;
                            }
                        }
                    }
                    break;
            }
        }

        void __mouseDownItem(EventContext context)
        {
            GButton item = context.sender as GButton;
            if (item == null || selectionMode == ListSelectionMode.None)
                return;

            _selectionHandled = false;

            if (UIConfig.defaultScrollTouchEffect && this.scrollPane != null)
                return;

            if (selectionMode == ListSelectionMode.Single)
            {
                SetSelectionOnEvent(item, (InputEvent)context.data);
            }
            else
            {
                if (!item.selected)
                    SetSelectionOnEvent(item, (InputEvent)context.data);
                //如果item.selected，这里不处理selection，因为可能用户在拖动
            }
        }

        void __clickItem(EventContext context)
        {
            GObject item = context.sender as GObject;
            if (!_selectionHandled)
                SetSelectionOnEvent(item, (InputEvent)context.data);
            _selectionHandled = false;

            if (scrollPane != null)
                scrollPane.ScrollToView(item, true);

            onClickItem.Call(item);
        }

        void SetSelectionOnEvent(GObject item, InputEvent evt)
        {
            if (!(item is GButton) || selectionMode == ListSelectionMode.None)
                return;

            _selectionHandled = true;
            bool dontChangeLastIndex = false;
            GButton button = (GButton)item;
            int index = GetChildIndex(item);

            if (selectionMode == ListSelectionMode.Single)
            {
                if (!button.selected)
                {
                    ClearSelectionExcept(button);
                    button.selected = true;
                }
            }
            else
            {
                if (evt.shift)
                {
                    if (!button.selected)
                    {
                        if (_lastSelectedIndex != -1)
                        {
                            int min = Math.Min(_lastSelectedIndex, index);
                            int max = Math.Max(_lastSelectedIndex, index);
                            max = Math.Min(max, this.numChildren - 1);
                            for (int i = min; i <= max; i++)
                            {
                                GButton obj = GetChildAt(i).asButton;
                                if (obj != null && !obj.selected)
                                    obj.selected = true;
                            }

                            dontChangeLastIndex = true;
                        }
                        else
                        {
                            button.selected = true;
                        }
                    }
                }
                else if (evt.ctrl || selectionMode == ListSelectionMode.Multiple_SingleClick)
                {
                    button.selected = !button.selected;
                }
                else
                {
                    if (!button.selected)
                    {
                        ClearSelectionExcept(button);
                        button.selected = true;
                    }
                    else
                        ClearSelectionExcept(button);
                }
            }

            if (!dontChangeLastIndex)
                _lastSelectedIndex = index;
        }

        void ClearSelectionExcept(GObject obj)
        {
            int cnt = _children.Count;
            for (int i = 0; i < cnt; i++)
            {
                GButton button = _children[i].asButton;
                if (button != null && button != obj && button.selected)
                    button.selected = false;
            }
        }

        public void ResizeToFit(int itemCount)
        {
            ResizeToFit(itemCount, 0);
        }

        public void ResizeToFit(int itemCount, int minSize)
        {
            EnsureBoundsCorrect();

            int curCount = this.numChildren;
            if (itemCount > curCount)
                itemCount = curCount;

            if (itemCount == 0)
            {
                if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                    this.ViewHeight = minSize;
                else
                    this.ViewWidth = minSize;
            }
            else
            {
                int i = itemCount - 1;
                GObject obj = null;
                while (i >= 0)
                {
                    obj = this.GetChildAt(i);
                    if (obj.visible)
                        break;
                    i--;
                }
                if (i < 0)
                {
                    if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                        this.ViewHeight = minSize;
                    else
                        this.ViewWidth = minSize;
                }
                else
                {
                    float size;
                    if (_layout == ListLayoutType.SingleColumn || _layout == ListLayoutType.FlowHorizontal)
                    {
                        size = obj.y + obj.actualHeight;
                        if (size < minSize)
                            size = minSize;
                        this.ViewHeight = size;
                    }
                    else
                    {
                        size = obj.x + obj.actualWidth;
                        if (size < minSize)
                            size = minSize;
                        this.ViewWidth = size;
                    }
                }
            }
        }

        override protected void HandleSizeChanged()
        {
            base.HandleSizeChanged();

            if (autoResizeItem)
                AdjustItemsSize();

            if (_layout == ListLayoutType.FlowHorizontal || _layout == ListLayoutType.FlowVertical)
                SetBoundsChangedFlag();
        }

        public void AdjustItemsSize()
        {
            if (_layout == ListLayoutType.SingleColumn)
            {
                int cnt = numChildren;
                float cw = this.ViewWidth;
                for (int i = 0; i < cnt; i++)
                {
                    GObject child = GetChildAt(i);
                    child.width = cw;
                }
            }
            else if (_layout == ListLayoutType.SingleRow)
            {
                int cnt = numChildren;
                float ch = this.ViewHeight;
                for (int i = 0; i < cnt; i++)
                {
                    GObject child = GetChildAt(i);
                    child.height = ch;
                }
            }
        }

        override protected internal void FindObjectNear(ref float xValue, ref float yValue)
        {
            int cnt = _children.Count;
            if (cnt == 0)
                return;

            EnsureBoundsCorrect();            
            GObject obj = null;

            int i = 0;
            if (yValue != 0)
            {
                for (; i < cnt; i++)
                {
                    obj = _children[i];
                    if (yValue < obj.y)
                    {
                        if (i == 0)
                        {
                            yValue = 0;
                            break;
                        }
                        else
                        {
                            GObject prev = _children[i - 1];
                            if (yValue < prev.y + prev.actualHeight / 2) //inside item, top half part
                                yValue = prev.y;
                            else if (yValue < prev.y + prev.actualHeight)//inside item, bottom half part
                                yValue = obj.y;
                            else //between two items
                                yValue = obj.y + _lineGap / 2;
                            break;
                        }
                    }
                }

                if (i == cnt)
                    yValue = obj.y;
            }

            if (xValue != 0)
            {
                if (i > 0)
                    i--;
                for (; i < cnt; i++)
                {
                    obj = _children[i];
                    if (xValue < obj.x)
                    {
                        if (i == 0)
                        {
                            xValue = 0;
                            break;
                        }
                        else
                        {
                            GObject prev = _children[i - 1];
                            if (xValue < prev.x + prev.actualWidth / 2) //inside item, top half part
                                xValue = prev.x;
                            else if (xValue < prev.x + prev.actualWidth)//inside item, bottom half part
                                xValue = obj.x;
                            else //between two items
                                xValue = obj.x + _columnGap / 2;
                            break;
                        }
                    }
                }
                if (i == cnt)
                    xValue = obj.x;
            }
        }

        override protected void UpdateBounds()
        {
            int cnt = numChildren;
            int i;
            GObject child;
            float curX = 0;
            float curY = 0;
            float cw, ch;
            float maxWidth = 0;
            float maxHeight = 0;

            for (i = 0; i < cnt; i++)
            {
                child = GetChildAt(i);
                child.EnsureSizeCorrect();
            }

            if (_layout == ListLayoutType.SingleColumn)
            {
                for (i = 0; i < cnt; i++)
                {
                    child = GetChildAt(i);
                    if (!child.visible)
                        continue;

                    if (curY != 0)
                        curY += _lineGap;
                    child.SetXY(curX, curY);
                    curY += child.actualHeight;
                    if (child.actualWidth > maxWidth)
                        maxWidth = child.actualWidth;
                }
                cw = curX + maxWidth;
                ch = curY;
            }
            else if (_layout == ListLayoutType.SingleRow)
            {
                for (i = 0; i < cnt; i++)
                {
                    child = GetChildAt(i);
                    if (!child.visible)
                        continue;

                    if (curX != 0)
                        curX += _columnGap;
                    child.SetXY(curX, curY);
                    curX += child.actualWidth;
                    if (child.actualHeight > maxHeight)
                        maxHeight = child.actualHeight;
                }
                cw = curX;
                ch = curY + maxHeight;
            }
            else if (_layout == ListLayoutType.FlowHorizontal)
            {
                cw = this.ViewWidth;
                for (i = 0; i < cnt; i++)
                {
                    child = GetChildAt(i);
                    if (!child.visible)
                        continue;

                    if (curX != 0)
                        curX += _columnGap;

                    if (curX + child.actualWidth > cw && maxHeight != 0)
                    {
                        //new line
                        curX = 0;
                        curY += maxHeight + _lineGap;
                        maxHeight = 0;
                    }
                    child.SetXY(curX, curY);
                    curX += child.actualWidth;
                    if (child.actualHeight > maxHeight)
                        maxHeight = child.actualHeight;
                }
                ch = curY + maxHeight;
            }
            else
            {
                ch = this.ViewHeight;
                for (i = 0; i < cnt; i++)
                {
                    child = GetChildAt(i);
                    if (!child.visible)
                        continue;

                    if (curY != 0)
                        curY += _lineGap;

                    if (curY + child.actualHeight > ch && maxWidth != 0)
                    {
                        curY = 0;
                        curX += maxWidth + _columnGap;
                        maxWidth = 0;
                    }
                    child.SetXY(curX, curY);
                    curY += child.actualHeight;
                    if (child.actualWidth > maxWidth)
                        maxWidth = child.actualWidth;
                }
                cw = curX + maxWidth;
            }
            SetBounds(0, 0, cw, ch);
        }

        override public void Setup_BeforeAdd(XML xml)
        {
            base.Setup_BeforeAdd(xml);

            string str;
            str = xml.GetAttribute("layout");
            if (str != null)
                _layout = FieldTypes.ParseListLayoutType(str);
            else
                _layout = ListLayoutType.SingleColumn;

            str = xml.GetAttribute("selectionMode");
            if (str != null)
                selectionMode = FieldTypes.ParseListSelectionMode(str);
            else
                selectionMode = ListSelectionMode.Single;

            OverflowType overflow;
            str = xml.GetAttribute("overflow");
            if (str != null)
                overflow = FieldTypes.ParseOverflowType(str);
            else
                overflow = OverflowType.Visible;

            ScrollType scroll;
            str = xml.GetAttribute("scroll");
            if (str != null)
                scroll = FieldTypes.ParseScrollType(str);
            else
                scroll = ScrollType.Vertical;

            ScrollBarDisplayType scrollBarDisplay;
            str = xml.GetAttribute("scrollBar");
            if (str != null)
                scrollBarDisplay = FieldTypes.ParseScrollBarDisplayType(str);
            else
                scrollBarDisplay = ScrollBarDisplayType.Default;

            int scrollBarFlags = xml.GetAttributeInt("scrollBarFlags");

            Margin scrollBarMargin = new Margin();
            str = xml.GetAttribute("scrollBarMargin");
            if (str != null)
                scrollBarMargin.Parse(str);

            str = xml.GetAttribute("margin");
            if (str != null)
                _margin.Parse(str);

            SetupOverflowAndScroll(overflow, scrollBarMargin, scroll, scrollBarDisplay, scrollBarFlags);

            string[] arr = xml.GetAttributeArray("clipSoftness");
            if (arr != null)
                this.clipSoftness = new Vector2(int.Parse(arr[0]), int.Parse(arr[1]));

            _lineGap = xml.GetAttributeInt("lineGap");
            _columnGap = xml.GetAttributeInt("colGap");
            defaultItem = xml.GetAttribute("defaultItem");

            autoResizeItem = xml.GetAttributeBool("autoItemSize", true);

            XMLList col = xml.Elements("item");
            foreach (XML ix in col)
            {
                string url = ix.GetAttribute("url");
                if (string.IsNullOrEmpty(url))
                    url = defaultItem;
                if (string.IsNullOrEmpty(url))
                    continue;

                GObject obj = AddItemFromPool(url);
                if (obj is GButton)
                {
                    ((GButton)obj).title = ix.GetAttribute("title");
                    ((GButton)obj).icon = ix.GetAttribute("icon");
                }
                else if (obj is GLabel)
                {
                    ((GLabel)obj).title = ix.GetAttribute("title");
                    ((GLabel)obj).icon = ix.GetAttribute("icon");
                }
            }
        }
    }
}
