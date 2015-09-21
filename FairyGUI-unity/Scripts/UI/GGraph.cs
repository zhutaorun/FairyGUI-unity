using System;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    public class GGraph : GObject
    {
        Shape _shape;

        public GGraph()
        {
        }

        public void ReplaceMe(GObject target)
        {
            if (parent == null)
                throw new Exception("parent not set");

            target.name = this.name;
            target.alpha = this.alpha;
            target.rotation = this.rotation;
            target.visible = this.visible;
            target.touchable = this.touchable;
            target.grayed = this.grayed;
            target.SetXY(this.x, this.y);
            target.SetSize(this.width, this.height);

            int index = parent.GetChildIndex(this);
            parent.AddChildAt(target, index);
            target.relations.CopyFrom(this.relations);

            parent.RemoveChild(this, true);
        }

        public void AddBeforeMe(GObject target)
        {
            if (parent == null)
                throw new Exception("parent not set");

            int index = parent.GetChildIndex(this);
            parent.AddChildAt(target, index);
        }

        public void AddAfterMe(GObject target)
        {
            if (parent == null)
                throw new Exception("parent not set");

            int index = parent.GetChildIndex(this);
            index++;
            parent.AddChildAt(target, index);
        }

        public void SetNativeObject(DisplayObject obj)
        {
            if (displayObject == obj)
                return;

            if (displayObject != null)
            {
                if (displayObject.parent != null)
                    displayObject.parent.RemoveChild(displayObject, true);
                else
                    displayObject.Dispose();
                _shape = null;
                displayObject = null;
            }

            displayObject = obj;

            if (displayObject != null)
            {
                displayObject.alpha = this.alpha;
                displayObject.rotation = this.rotation;
                displayObject.visible = this.visible;
                displayObject.touchable = this.touchable;
                displayObject.gOwner = this;
            }

            if (parent != null)
                parent.ChildStateChanged(this);
            HandleXYChanged();
        }

        public Shape shape
        {
            get
            {
                if (_shape != null)
                    return _shape;

                if (displayObject != null)
                    displayObject.Dispose();

                _shape = new Shape();
                _shape.gOwner = this;
                _shape.SetScale(GRoot.contentScaleFactor, GRoot.contentScaleFactor);
                displayObject = _shape;
                if (parent != null)
                    parent.ChildStateChanged(this);
                HandleXYChanged();
                displayObject.alpha = this.alpha;
                displayObject.rotation = this.rotation;
                displayObject.visible = this.visible;

                return _shape;
            }
        }

        public void DrawRect(float aWidth, float aHeight, int lineSize, Color lineColor, Color fillColor)
        {
            this.SetSize(aWidth, aHeight);
            this.shape.DrawRect(aWidth * this.scaleX, aHeight * this.scaleY, lineSize, lineColor, fillColor);
        }

        override protected void HandleSizeChanged()
        {
            if (_shape != null)
            {
                _shape.ResizeShape(this.width * this.scaleX, this.height * this.scaleY);
            }
        }

        override public void Setup_BeforeAdd(XML xml)
        {
            string str;
            string type = xml.GetAttribute("type");
            if (type != null && type != "empty")
            {
                _shape = new Shape();
                _shape.gOwner = this;
                _shape.SetScale(GRoot.contentScaleFactor, GRoot.contentScaleFactor);
                displayObject = _shape;
            }

            base.Setup_BeforeAdd(xml);

            if (_shape != null)
            {
                int lineSize;
                str = xml.GetAttribute("lineSize");
                if (str != null)
                    lineSize = int.Parse(str);
                else
                    lineSize = 1;

                Color lineColor;
                str = xml.GetAttribute("lineColor");
                if (str != null)
                    lineColor = ToolSet.ConvertFromHtmlColor(str);
                else
                    lineColor = Color.black;

                Color fillColor;
                str = xml.GetAttribute("fillColor");
                if (str != null)
                    fillColor = ToolSet.ConvertFromHtmlColor(str);
                else
                    fillColor = Color.white;

                string corner;
                str = xml.GetAttribute("corner");
                if (str != null)
                    corner = str;

                DrawRect(this.width, this.height, lineSize, lineColor, fillColor);
            }
        }
    }
}
