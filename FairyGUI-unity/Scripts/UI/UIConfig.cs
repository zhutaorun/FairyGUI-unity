using System;
using UnityEngine;
using DG.Tweening;

namespace FairyGUI
{
    public class UIConfig
    {
        //Dynamic Font Support. Put the xxx.ttf into /Resources, and defaultFont="xxx".
        public static string defaultFont = "";
        //Useful for chinese fonts, otherwise, turn off it.
        public static bool renderingTextBrighterOnDesktop = true;

        //Resource using in Window.ShowModalWait for locking the window.
        public static string windowModalWaiting;
        //Resource using in GRoot.ShowModalWait for locking the screen.
        public static String globalModalWaiting;

        //When a modal window is in front, the background becomes dark.
        public static Color modalLayerColor = new Color(0f, 0f, 0f, 0.4f);

        //Default button click sound
        public static AudioClip buttonSound;
        public static float buttonSoundVolumeScale = 1f;

        //Resources for scrollbars
        public static string horizontalScrollBar;
        public static string verticalScrollBar;
        //Scrolling step in pixels
        public static float defaultScrollSpeed = 25;
        //Default scrollbar display mode. Recommened visible for Desktop and Auto for mobile.
        public static ScrollBarDisplayType defaultScrollBarDisplay = ScrollBarDisplayType.Visible;
        //Allow dragging the content to scroll. Recommeded true for mobile.
        public static bool defaultScrollTouchEffect = true;
        //The "rebound" effect in the scolling container. Recommeded true for mobile.
        public static bool defaultScrollBounceEffect = true;

        //Resources for PopupMenu.
        public static string popupMenu;
        //Resources for seperator of PopupMenu.
        public static string popupMenu_seperator;
        //In case of failure of loading content for GLoader, use this sign to indicate an error.
        public static string loaderErrorSign;
        //Resources for tooltips.
        public static string tooltipsWin;

        public static int defaultComboBoxVisibleItemCount = 10;
    }
}
