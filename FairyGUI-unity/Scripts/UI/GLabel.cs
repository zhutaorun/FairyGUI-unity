using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    public class GLabel : GComponent
    {
        protected GObject _titleObject;
        protected GObject _iconObject;

        public GLabel()
        {
        }

        public string icon
        {
            get
            {
                if (_iconObject is GLoader)
                    return ((GLoader)_iconObject).url;
                else if (_iconObject is GLabel)
                    return ((GLabel)_iconObject).icon;
                else if (_iconObject is GButton)
                    return ((GButton)_iconObject).icon;
                else
                    return null;
            }

            set
            {
                if (_iconObject is GLoader)
                    ((GLoader)_iconObject).url = value;
                else if (_iconObject is GLabel)
                    ((GLabel)_iconObject).icon = value;
                else if (_iconObject is GButton)
                    ((GButton)_iconObject).icon = value;
            }
        }

        public string title
        {
            get
            {
                if (_titleObject != null)
                    return _titleObject.text;
                else
                    return null;
            }
            set
            {
                if (_titleObject != null)
                    _titleObject.text = value;
            }
        }

        override public string text
        {
            get { return this.title; }
            set { this.title = value; }
        }

        public bool editable
        {
            get
            {
                if (_titleObject is GTextInput)
                    return _titleObject.asTextInput.editable;
                else
                    return false;
            }

            set
            {
                if (_titleObject is GTextInput)
                    _titleObject.asTextInput.editable = value;
            }
        }

        public Color titleColor
        {
            get
            {
                if (_titleObject is GTextField)
                    return ((GTextField)_titleObject).color;
                else if (_titleObject is GLabel)
                    return ((GLabel)_titleObject).titleColor;
                else if (_titleObject is GButton)
                    return ((GButton)_titleObject).titleColor;
                else
                    return Color.black;
            }
            set
            {
                if (_titleObject is GTextField)
                    ((GTextField)_titleObject).color = value;
                else if (_titleObject is GLabel)
                    ((GLabel)_titleObject).titleColor = value;
                else if (_titleObject is GButton)
                    ((GButton)_titleObject).titleColor = value;
            }
        }

        override public void ConstructFromXML(XML cxml)
        {
            base.ConstructFromXML(cxml);

            _titleObject = GetChild("title");
            _iconObject = GetChild("icon");
        }

        override public void Setup_AfterAdd(XML cxml)
        {
            base.Setup_AfterAdd(cxml);

            XML xml = cxml.GetNode("Label");
            if (xml == null)
            {
                this.title = string.Empty;
                this.icon = null;
                return;
            }

            this.title = xml.GetAttribute("title");
            this.icon = xml.GetAttribute("icon");
            string str = xml.GetAttribute("titleColor");
            if (str != null)
                this.titleColor = ToolSet.ConvertFromHtmlColor(str);
        }
    }
}
