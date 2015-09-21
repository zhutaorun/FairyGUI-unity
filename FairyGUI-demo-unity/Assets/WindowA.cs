using System;
using System.Collections.Generic;
using FairyGUI;

public class WindowA : Window
{
    public WindowA()
    {
    }

    protected override void OnInit()
    {
        this.contentPane = UIPackage.CreateObject("Demo", "WindowA").asCom;
        this.Center();
    }

    override protected void OnShown()
    {
        GList list = this.contentPane.GetChild("n6").asList;
        list.RemoveChildrenToPool();

        for (int i = 0; i < 6; i++)
        {
            GButton item = list.AddItemFromPool().asButton;
            item.title = "" + i;
            item.icon = UIPackage.GetItemURL("Demo", "r4");
        }
    }
}
