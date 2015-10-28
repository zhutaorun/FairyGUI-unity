using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;
using DG.Tweening;

public class TestWindow : Window
{
	RenderImage _renderImage;

	public TestWindow()
	{
	}

	protected override void OnInit()
	{
		this.contentPane = UIPackage.CreateObject("Demo", "TestWin").asCom;
		this.Center();

		_renderImage = new RenderImage(contentPane.GetChild("holder").asGraph);
		//RenderImage是不透明的，可以设置最多两张图片作为背景图
		_renderImage.SetBackground(contentPane.GetChild("frame").asCom.GetChild("n0"), contentPane.GetChild("n20"));

		contentPane.GetChild("btnLeft").onMouseDown.Add(__clickLeft);
		contentPane.GetChild("btnRight").onMouseDown.Add(__clickRight);

		//弹出窗口的动效已中心为轴心
		this.SetPivot(this.width / 2, this.height / 2);
	}

	override protected void DoShowAnimation()
	{
		this.SetScale(0.1f, 0.1f);
		this.TweenScale(new Vector2(1, 1), 0.3f).SetEase(Ease.OutQuad).OnComplete(this.OnShown);
	}

	override protected void DoHideAnimation()
	{
		this.TweenScale(new Vector2(0.1f, 0.1f), 0.3f).SetEase(Ease.OutQuad).OnComplete(this.HideImmediately);
	}

	override protected void OnShown()
	{
		_renderImage.LoadModel("Role/npc");
	}

	void __clickLeft()
	{
		_renderImage.StartRotate(-2);
		Stage.inst.onMouseUp.Add(__mouseUp);
	}

	void __clickRight()
	{
		_renderImage.StartRotate(2);
		Stage.inst.onMouseUp.Add(__mouseUp);
	}

	void __mouseUp()
	{
		_renderImage.StopRotate();
		Stage.inst.onMouseUp.Remove(__mouseUp);
	}
}
