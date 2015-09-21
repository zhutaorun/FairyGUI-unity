using System;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    public class Container : DisplayObject
    {
        List<DisplayObject> _children;

        Rect? _clipRect;
        Rect? _hitArea;
        Vector4? _clipSoftness;

        bool _fBatchingRequested;
        bool _fBatchingInherited;
        bool _fBatching;
        List<DisplayObject> _descendants;

        bool _skipRendering;

        internal EventCallback0 onUpdate;

        public Container()
            : base()
        {
            _children = new List<DisplayObject>();
        }

        public int numChildren
        {
            get { return _children.Count; }
        }

        public DisplayObject AddChild(DisplayObject child)
        {
            AddChildAt(child, _children.Count);
            return child;
        }

        public DisplayObject AddChildAt(DisplayObject child, int index)
        {
            int count = _children.Count;
            if (index >= 0 && index <= count)
            {
                if (child.parent == this)
                {
                    SetChildIndex(child, index);
                }
                else
                {
                    child.RemoveFromParent();
                    if (index == count)
                        _children.Add(child);
                    else
                        _children.Insert(index, child);
                    child.SetParent(this);
                    child.tmpZ = 0;

                    if (stage != null)
                    {
                        if (child is Container)
                            child.onAddedToStage.BroadcastCall();
                        else
                            child.onAddedToStage.Call();
                    }

                    InvalidateBatchingState();
                }
                return child;
            }
            else
            {
                throw new Exception("Invalid child index");
            }
        }

        public bool Contains(DisplayObject child)
        {
            return _children.Contains(child);
        }

        public DisplayObject GetChildAt(int index)
        {
            return _children[index];
        }

        public int GetChildIndex(DisplayObject child)
        {
            return _children.IndexOf(child);
        }

        public DisplayObject RemoveChild(DisplayObject child)
        {
            return RemoveChild(child, false);
        }

        public DisplayObject RemoveChild(DisplayObject child, bool dispose)
        {
            if (child.parent != this)
                throw new Exception("obj is not a child");

            int i = _children.IndexOf(child);
            return RemoveChildAt(i, dispose);
        }

        public DisplayObject RemoveChildAt(int index)
        {
            return RemoveChildAt(index, false);
        }

        public DisplayObject RemoveChildAt(int index, bool dispose)
        {
            if (index >= 0 && index < _children.Count)
            {
                DisplayObject child = _children[index];

                if (stage != null)
                {
                    if (child is Container)
                        child.onRemovedFromStage.BroadcastCall();
                    else
                        child.onRemovedFromStage.Call();
                }
                _children.Remove(child);
                child.SetParent(null);
                InvalidateBatchingState();
                if (dispose)
                    child.Dispose();

                return child;
            }
            else
                throw new Exception("Invalid child index");
        }

        public void RemoveChildren()
        {
            RemoveChildren(0, int.MaxValue, false);
        }

        public void RemoveChildren(int beginIndex, int endIndex, bool dispose)
        {
            if (endIndex < 0 || endIndex >= numChildren)
                endIndex = numChildren - 1;

            for (int i = beginIndex; i <= endIndex; ++i)
                RemoveChildAt(beginIndex, dispose);
        }

        public void SetChildIndex(DisplayObject child, int index)
        {
            int oldIndex = _children.IndexOf(child);
            if (oldIndex == index) return;
            if (oldIndex == -1) throw new ArgumentException("Not a child of this container");
            _children.RemoveAt(oldIndex);
            if (index >= _children.Count)
                _children.Add(child);
            else
                _children.Insert(index, child);
            InvalidateBatchingState();
        }

        public void SwapChildren(DisplayObject child1, DisplayObject child2)
        {
            int index1 = _children.IndexOf(child1);
            int index2 = _children.IndexOf(child2);
            if (index1 == -1 || index2 == -1)
                throw new Exception("Not a child of this container");
            SwapChildrenAt(index1, index2);
        }

        public void SwapChildrenAt(int index1, int index2)
        {
            DisplayObject obj1 = _children[index1];
            DisplayObject obj2 = _children[index2];
            _children[index1] = obj2;
            _children[index2] = obj1;
            InvalidateBatchingState();
        }

        public Rect? clipRect
        {
            get { return _clipRect; }
            set
            {
                _clipRect = value;
                if (_clipRect != null)
                    contentRect = (Rect)_clipRect;
                else
                    contentRect.Set(0, 0, 0, 0);
            }
        }

        //left-top-right-bottom
        public Vector4? clipSoftness
        {
            get { return _clipSoftness; }
            set { _clipSoftness = value; }
        }

        public Rect? hitArea
        {
            get { return _hitArea; }
            set { _hitArea = value; }
        }

        public override Rect GetBounds(DisplayObject targetSpace)
        {
            int count = _children.Count;

            Rect rect;
            if (count == 0)
            {
                Vector2 v = TransformPoint(new Vector2(0, 0), targetSpace);
                rect = Rect.MinMaxRect(v.x, v.y, 0, 0);
            }
            else if (count == 1)
            {
                rect = _children[0].GetBounds(targetSpace);
            }
            else
            {
                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;

                for (int i = 0; i < count; ++i)
                {
                    rect = _children[i].GetBounds(targetSpace);
                    minX = minX < rect.xMin ? minX : rect.xMin;
                    maxX = maxX > rect.xMax ? maxX : rect.xMax;
                    minY = minY < rect.yMin ? minY : rect.yMin;
                    maxY = maxY > rect.yMax ? maxY : rect.yMax;
                }

                rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            }

            if (_clipRect != null)
            {
                Rect clipRect = TransformRect((Rect)_clipRect, targetSpace);
                rect = ToolSet.Intersection(ref rect, ref clipRect);
            }

            return rect;
        }

        public Rect GetClipRect(DisplayObject targetSpace)
        {
            if (_clipRect == null) return new Rect();

            if (targetSpace == this || targetSpace == null) // optimization
            {
                return (Rect)_clipRect;
            }
            else if (targetSpace == parent && rotation == 0f) // optimization
            {
                Rect rect = (Rect)_clipRect;
                float scaleX = this.scaleX;
                float scaleY = this.scaleY;
                rect = new Rect(this.x + rect.x, this.y + rect.y, rect.width, rect.height);
                rect.x *= scaleX;
                rect.y *= scaleY;
                rect.width *= scaleX;
                rect.height *= scaleY;
                return rect;
            }
            else
            {
                return TransformRect((Rect)_clipRect, targetSpace);
            }
        }

        public Rect GetWorldClipRect()
        {
            if (_clipRect == null) return new Rect();

            Rect rect = (Rect)_clipRect;
            float xMin = float.MaxValue, xMax = float.MinValue;
            float yMin = float.MaxValue, yMax = float.MinValue;
            Rect result = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

            TransformRectPoint(rect.xMin, rect.yMin, ref result);
            TransformRectPoint(rect.xMax, rect.yMin, ref result);
            TransformRectPoint(rect.xMin, rect.yMax, ref result);
            TransformRectPoint(rect.xMax, rect.yMax, ref result);

            return result;
        }

        private void TransformRectPoint(float px, float py, ref Rect rect)
        {
            Vector2 v = this.cachedTransform.TransformPoint(px, -py, 0);
            if (rect.xMin > v.x) rect.xMin = v.x;
            if (rect.xMax < v.x) rect.xMax = v.x;
            if (rect.yMin > v.y) rect.yMin = v.y;
            if (rect.yMax < v.y) rect.yMax = v.y;
        }

        public override DisplayObject HitTest(Vector2 localPoint, bool forTouch)
        {
            if (_skipRendering)
                return null;

            if (forTouch && (!visible || !touchable || optimizeNotTouchable))
                return null;

            if (_clipRect != null && !((Rect)_clipRect).Contains(localPoint))
                return null;
            else
            {
                int count = _children.Count;
                DisplayObject target = null;
                for (int i = count - 1; i >= 0; --i) // front to back!
                {
                    DisplayObject child = _children[i];

                    Vector2 v = TransformPoint(localPoint, child);
                    target = child.HitTest(v, forTouch);

                    if (target != null) break;
                }

                if (target == null && _hitArea != null)
                {
                    if (((Rect)_hitArea).Contains(localPoint))
                        target = this;
                }

                return target;
            }
        }

        public bool IsAncestorOf(DisplayObject obj)
        {
            if (obj == null)
                return false;

            Container p = obj.parent;
            while (p != null)
            {
                if (p == this)
                    return true;

                p = p.parent;
            }
            return false;
        }

        public bool fairyBatching
        {
            get { return _fBatching; }
            set
            {
                if (_fBatching != value)
                {
                    _fBatching = value;
                    _fBatchingRequested = _fBatching;
                    if (!_fBatching)
                    {
                        if (_descendants != null)
                            _descendants.Clear();
                    }
                }
            }
        }

        public bool fairyBatchingInherited
        {
            get { return _fBatchingInherited; }
        }

        override public void InvalidateBatchingState()
        {
            if (_fBatching)
                _fBatchingRequested = true;
            else if (_fBatchingInherited)
            {
                Container p = this.parent;
                while (p != null)
                {
                    if (p._fBatching)
                    {
                        p._fBatchingRequested = true;
                        break;
                    }

                    p = p.parent;
                }
            }
        }

        internal void OnFontRebuild(BaseFont font)
        {
            foreach (DisplayObject child in _children)
            {
                if (child is Container)
                {
                    ((Container)child).OnFontRebuild(font);
                }
                else if ((child is TextField) && ((TextField)child).font == font)
                {
                    ((TextField)child).OnFontRebuild();
                }
            }
        }

        override public void Update(UpdateContext context, float parentAlpha)
        {
            _skipRendering = gOwner != null && gOwner.parent != null && !gOwner.parent.IsChildInView(gOwner);
            if (_skipRendering)
            {
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
                return;
            }
            else if (!gameObject.activeSelf && this.visible)
                gameObject.SetActive(true);

            if (onUpdate != null)
                onUpdate();

            if (_clipRect != null)
                context.EnterClipping(this);

            float thisAlpha = parentAlpha * this.alpha;
            if (_fBatching && !_fBatchingInherited)
            {
                if (_fBatchingRequested)
                {
                    DoFairyBatching();
                }

                this.tmpZ = 0f;
                context.allotingZ += PresetZOrder(0f);
            }

            if (_fBatching || _fBatchingInherited)
            {
                foreach (DisplayObject child in _children)
                {
                    if (child.visible)
                    {
                        context.counter++;
                        child.z = child.tmpZ - this.tmpZ;
                        child.Update(context, thisAlpha);
                    }
                }
            }
            else
            {
                foreach (DisplayObject child in _children)
                {
                    if (child.visible)
                    {
                        context.counter++;

                        if (child.z <= context.allotingZ && (context.allotingZ - child.z) < 1)
                            context.allotingZ = child.z;//optimize not change obj.z;
                        else
                            child.z = context.allotingZ;
                        context.allotingZ -= 0.001f;

                        float savedZ = context.allotingZ;
                        context.allotingZ = 0f;
                        child.Update(context, thisAlpha);
                        context.allotingZ += savedZ;
                    }
                }
            }

            if (_clipRect != null)
                context.LeaveClipping();
        }

        private float PresetZOrder(float start)
        {
            int cnt = _descendants.Count;
            float gz = start;
            for (int i = 0; i < cnt; i++)
            {
                DisplayObject current = _descendants[i];
                if (current.tmpZ <= gz && (gz - current.tmpZ) < 1)
                    gz = current.tmpZ;
                else
                    current.tmpZ = gz;
                gz -= 0.001f;
                if ((current is Container) && ((Container)current)._clipRect != null)
                    gz = ((Container)current).PresetZOrder(gz);
            }

            return gz;
        }

        private void DoFairyBatching()
        {
            _fBatchingRequested = false;

            if (_descendants == null)
                _descendants = new List<DisplayObject>();
            else
                _descendants.Clear();
            CollectChildren(this);

            int cnt = _descendants.Count;

            int i, j;
            for (i = 0; i < cnt; i++)
            {
                DisplayObject current = _descendants[i];
                Rect bound = current.tmpBounds;
                if (current.material == null)
                    continue;

                for (j = i - 1; j >= 0; j--)
                {
                    DisplayObject test = _descendants[j];
                    if (current.material == test.material)
                    {
                        if (i != j + 1)
                        {
                            _descendants.RemoveAt(i);
                            _descendants.Insert(j + 1, current);
                        }
                        break;
                    }

                    if (ToolSet.Intersects(ref bound, ref test.tmpBounds))
                        break;
                }
            }

            for (i = 0; i < cnt; i++)
            {
                DisplayObject current = _descendants[i];
                Rect bound = current.tmpBounds;
                if (current.material == null)
                    continue;

                for (j = i + 1; j < cnt; j++)
                {
                    DisplayObject test = _descendants[j];
                    if (current.material == test.material)
                    {
                        if (i != j - 1)
                        {
                            _descendants.Insert(j, current);
                            _descendants.RemoveAt(i);
                            if (i == 0)
                                i--;
                            else
                                i -= 2;
                        }
                        break;
                    }

                    if (ToolSet.Intersects(ref bound, ref test.tmpBounds))
                        break;
                }
            }
        }

        private void CollectChildren(Container initiator)
        {
            int count = _children.Count;
            for (int i = 0; i < count; i++)
            {
                DisplayObject child = _children[i];
                if (child is Container)
                {
                    Container container = (Container)child;
                    container._fBatchingInherited = true;
                    initiator._descendants.Add(container);

                    if (container._clipRect == null)
                    {
                        child.tmpBounds.Set(0, 0, 0, 0);
                        container.CollectChildren(initiator);
                    }
                    else
                    {
                        container.tmpBounds = container.GetClipRect(initiator);
                        container.DoFairyBatching();
                    }
                }
                else
                {
                    child.tmpBounds = child.GetBounds(initiator);
                    initiator._descendants.Add(child);
                }
            }
        }
    }
}
