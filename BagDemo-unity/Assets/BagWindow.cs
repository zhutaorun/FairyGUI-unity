using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;
using DG.Tweening;

public class BagWindow : Window
{
	GList _list;

	public BagWindow()
	{
	}

	protected override void OnInit()
	{
		this.contentPane = UIPackage.CreateObject("Bag", "BagWin").asCom;
		this.Center();

		_list = this.contentPane.GetChild("list").asList;
		_list.onClickItem.Add(__clickItem);


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
		//load icons
		for (int i = 0; i < 10; i++)
		{
			GButton button = _list.GetChildAt(i).asButton;
			button.icon = "i" + UnityEngine.Random.Range(0, 10);
			button.title = "" + UnityEngine.Random.Range(0, 100);
		}
	}

	void __clickItem(EventContext context)
	{
		GButton item = (GButton)context.data;
		this.contentPane.GetChild("n11").asLoader.url = item.icon;
		this.contentPane.GetChild("n13").text = item.icon;
	}
}
