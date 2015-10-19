using System;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    public class GSlider : GComponent
    {
        int _max;
        int _value;
        ProgressTitleType _titleType;

        GTextField _titleObject;
        GMovieClip _aniObject;
        GObject _barObjectH;
        GObject _barObjectV;
        float _barMaxWidth;
        float _barMaxHeight;
        float _barMaxWidthDelta;
        float _barMaxHeightDelta;
        GObject _gripObject;
        Vector2 _clickPos;
        float _clickPercent;
        int _touchId;

        public EventListener onChanged { get; private set; }

        public GSlider()
        {
            _value = 50;
            _max = 100;

            onChanged = new EventListener(this, "onChanged");
        }

        public ProgressTitleType titleType
        {
            get
            {
                return _titleType;
            }
            set
            {
                if (_titleType != value)
                {
                    _titleType = value;
                    Update();
                }
            }
        }

        public int max
        {
            get
            {
                return _max;
            }
            set
            {
                if (_max != value)
                {
                    _max = value;
                    Update();
                }
            }
        }

        public int value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    Update();
                }
            }
        }

        private void Update()
        {
            float percent = Math.Min((float)_value / _max, 1);
            UpdateWidthPercent(percent);
        }

        private void UpdateWidthPercent(float percent)
        {
            if (_titleObject != null)
            {
                switch (_titleType)
                {
                    case ProgressTitleType.Percent:
                        _titleObject.text = Mathf.RoundToInt(percent * 100) + "%";
                        break;

                    case ProgressTitleType.ValueAndMax:
                        _titleObject.text = _value + "/" + max;
                        break;

                    case ProgressTitleType.Value:
                        _titleObject.text = "" + _value;
                        break;

                    case ProgressTitleType.Max:
                        _titleObject.text = "" + _max;
                        break;
                }
            }

            if (_barObjectH != null)
                _barObjectH.width = Mathf.RoundToInt((this.width - _barMaxWidthDelta) * percent);
            if (_barObjectV != null)
                _barObjectV.height = Mathf.RoundToInt((this.height - _barMaxHeightDelta) * percent);
            if (_aniObject != null)
                _aniObject.frame = Mathf.RoundToInt(percent * 100);
        }

        override public void ConstructFromXML(XML cxml)
        {
            base.ConstructFromXML(cxml);

            XML xml = cxml.GetNode("Slider");

            string str;
            str = xml.GetAttribute("titleType");
            if (str != null)
                _titleType = FieldTypes.ParseProgressTitleType(str);
            else
                _titleType = ProgressTitleType.Percent;

            _titleObject = GetChild("title") as GTextField;
            _barObjectH = GetChild("bar");
            _barObjectV = GetChild("bar_v");
            _aniObject = GetChild("ani") as GMovieClip;
            _gripObject = GetChild("grip");

            if (_barObjectH != null)
            {
                _barMaxWidth = _barObjectH.width;
                _barMaxWidthDelta = this.width - _barMaxWidth;
            }
            if (_barObjectV != null)
            {
                _barMaxHeight = _barObjectV.height;
                _barMaxHeightDelta = this.height - _barMaxHeight;
            }

            if (_gripObject != null)
                _gripObject.displayObject.onMouseDown.Add(__gripMouseDown);
        }

        override public void Setup_AfterAdd(XML cxml)
        {
            base.Setup_AfterAdd(cxml);

            XML xml = cxml.GetNode("Slider");
            if (xml != null)
            {
                _value = xml.GetAttributeInt("value");
                _max = xml.GetAttributeInt("max");
            }
            Update();
        }

        override protected void HandleSizeChanged()
        {
            base.HandleSizeChanged();

            if (_barObjectH != null)
                _barMaxWidth = this.width - _barMaxWidthDelta;
            if (_barObjectV != null)
                _barMaxHeight = this.height - _barMaxHeightDelta;

            if (!this.underConstruct)
                Update();
        }

        private void __gripMouseDown(EventContext context)
        {
            InputEvent evt = context.inputEvent;
            _touchId = evt.touchId;

            _clickPos.x = evt.x / GRoot.contentScaleFactor;
            _clickPos.y = evt.y / GRoot.contentScaleFactor;
            _clickPercent = (float)_value / _max;
            _gripObject.displayObject.stage.onMouseMove.Add(__gripMouseMove);
            _gripObject.displayObject.stage.onMouseUp.Add(__gripMouseUp);
        }

        private void __gripMouseMove(EventContext context)
        {
            InputEvent evt = context.inputEvent;
            if (_touchId != evt.touchId)
                return;

            float deltaX = evt.x / GRoot.contentScaleFactor - _clickPos.x;
            float deltaY = evt.y / GRoot.contentScaleFactor - _clickPos.y;

            float percent;
            if (_barObjectH != null)
                percent = _clickPercent + deltaX / _barMaxWidth;
            else
                percent = _clickPercent + deltaY / _barMaxHeight;
            if (percent > 1)
                percent = 1;
            else if (percent < 0)
                percent = 0;

            int newValue = Mathf.RoundToInt(percent * _max);
            if (newValue != _value)
            {
                _value = newValue;
                onChanged.Call();
            }
            UpdateWidthPercent(percent);
        }

        private void __gripMouseUp(EventContext context)
        {
            InputEvent evt = context.inputEvent;
            if (_touchId != evt.touchId)
                return;

            float percent = (float)_value / _max;
            UpdateWidthPercent(percent);
            _gripObject.displayObject.stage.onMouseMove.Remove(__gripMouseMove);
            _gripObject.displayObject.stage.onMouseUp.Remove(__gripMouseUp);
        }
    }
}
