using UnityEngine;
using FairyGUI;

public class Main : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = 60;
        DontDestroyOnLoad(this.gameObject);

        Stage.Instantiate(5);
        Stage.inst.AddChild(new GRoot().displayObject);
        Stage.inst.camera.clearFlags = CameraClearFlags.Depth;

        if(Application.isMobilePlatform)
            GRoot.inst.SetContentScaleFactor(640, 960);

        UIPackage.AddPackage("demo");

        UIConfig.defaultFont = "arial";
        UIConfig.verticalScrollBar = UIPackage.GetItemURL("Demo", "ScrollBar_VT");
        UIConfig.horizontalScrollBar = UIPackage.GetItemURL("Demo", "ScrollBar_HZ");
        UIConfig.popupMenu = UIPackage.GetItemURL("Demo", "PopupMenu");

        new MainPanel();
    }
}