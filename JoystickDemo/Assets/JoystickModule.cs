using FairyGUI;
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class JoystickModule : EventDispatcher
{
	float _InitX;
	float _InitY;
	float _startStageX;
	float _startStageY;
	float _lastStageX;
	float _lastStageY;
	GButton _button;
	GObject _touchArea;
	GObject _thumb;
	GObject _center;
	int touchId;
	Tweener _tweener;

	public EventListener onMove { get; private set; }
	public EventListener onEnd { get; private set; }

	public int radius { get; set; }

	public JoystickModule(GComponent mainView)
	{
		onMove = new EventListener(this, "onMove");
		onEnd = new EventListener(this, "onEnd");

		_button = mainView.GetChild("joystick").asButton;
		_button.changeStateOnClick = false;
		_thumb = _button.GetChild("thumb");
		_touchArea = mainView.GetChild("joystick_touch");
		_center = mainView.GetChild("joystick_center");

		_InitX = _center.x + _center.width / 2;
		_InitY = _center.y + _center.height / 2;
		touchId = -1;
		radius = 150;

		_touchArea.onMouseDown.Add(this.onTouchDown);
	}

	public void Trigger(EventContext context)
	{
		onTouchDown(context);
	}

	private void onTouchDown(EventContext context)
	{
		if (touchId == -1)//First touch
		{
			InputEvent inputEvt = (InputEvent)context.data;
			touchId = inputEvt.touchId;

			if (_tweener != null)
			{
				_tweener.Kill();
				_tweener = null;
			}

			float bx = inputEvt.x / GRoot.contentScaleFactor;
			float by = inputEvt.y / GRoot.contentScaleFactor;
			_button.selected = true;

			if (bx < 0)
				bx = 0;
			else if (bx > _touchArea.width)
				bx = _touchArea.width;

			if (by > GRoot.inst.height)
				by = GRoot.inst.height;
			else if (by < _touchArea.y)
				by = _touchArea.y;

			_lastStageX = bx;
			_lastStageY = by;
			_startStageX = bx;
			_startStageY = by;

			_center.visible = true;
			_center.x = bx - _center.width / 2;
			_center.y = by - _center.height / 2;
			_button.x = bx - _button.width / 2;
			_button.y = by - _button.height / 2;

			float deltaX = bx - _InitX;
			float deltaY = by - _InitY;
			float degrees = Mathf.Atan2(deltaY, deltaX) * 180 / Mathf.PI;
			_thumb.rotation = degrees + 90;

			GRoot.nativeStage.onMouseMove.Add(this.OnTouchMove);
			GRoot.nativeStage.onMouseUp.Add(this.OnTouchUp);
		}
	}

	private void OnTouchUp(EventContext context)
	{
		InputEvent inputEvt = (InputEvent)context.data;
		if (inputEvt == null)
			return;

		if (touchId != -1 && inputEvt.touchId == touchId)
		{
			touchId = -1;
			_thumb.rotation = _thumb.rotation + 180;
			_center.visible = false;
			_tweener = _button.TweenMove(new Vector2(_InitX - _button.width / 2, _InitY - _button.height / 2), 0.3f).OnComplete(() =>
			{
				_tweener = null;
				_button.selected = false;
				_thumb.rotation = 0;
				_center.visible = true;
				_center.x = _InitX - _center.width / 2;
				_center.y = _InitY - _center.height / 2;
			}
			);

			GRoot.nativeStage.onMouseMove.Remove(this.OnTouchMove);
			GRoot.nativeStage.onMouseUp.Remove(this.OnTouchUp);

			this.onEnd.Call();
		}
	}

	private void OnTouchMove(EventContext context)
	{
		InputEvent inputEvt = (InputEvent)context.data;
		if (inputEvt == null)
			return;

		if (touchId != -1 && inputEvt.touchId == touchId)
		{
			float bx = inputEvt.x / GRoot.contentScaleFactor;
			float by = inputEvt.y / GRoot.contentScaleFactor;
			float moveX = bx - _lastStageX;
			float moveY = by - _lastStageY;
			_lastStageX = bx;
			_lastStageY = by;
			float buttonX = _button.x + moveX;
			float buttonY = _button.y + moveY;

			float offsetX = buttonX + _button.width / 2 - _startStageX;
			float offsetY = buttonY + _button.height / 2 - _startStageY;

			float rad = Mathf.Atan2(offsetY, offsetX);
			float degree = rad * 180 / Mathf.PI;
			_thumb.rotation = degree + 90;

			float maxX = radius * Mathf.Cos(rad);
			float maxY = radius * Mathf.Sin(rad);
			if (Mathf.Abs(offsetX) > Mathf.Abs(maxX))
				offsetX = maxX;
			if (Mathf.Abs(offsetY) > Mathf.Abs(maxY))
				offsetY = maxY;

			buttonX = _startStageX + offsetX;
			buttonY = _startStageY + offsetY;
			if (buttonX < 0)
				buttonX = 0;
			if (buttonY > GRoot.inst.height)
				buttonY = GRoot.inst.height;

			_button.x = buttonX - _button.width / 2;
			_button.y = buttonY - _button.height / 2;

			this.onMove.Call(degree);
		}
	}
}