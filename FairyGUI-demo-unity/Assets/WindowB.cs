using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;
using DG.Tweening;

public class WindowB : Window
{
    public WindowB()
    {
    }

    protected override void OnInit()
    {
        this.contentPane = UIPackage.CreateObject("Demo", "WindowB").asCom;
        this.Center();

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
        contentPane.GetTransition("t1").Play();
    }

    override protected void OnHide()
    {
        contentPane.GetTransition("t1").Stop();
    }
}
