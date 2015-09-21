using System.Collections.Generic;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    public struct ClipInfo
    {
        public Rect rect;
        public Vector2 offset;
        public Vector2 scale;
        public bool soft;
        public Vector4 softness;//left-top-right-bottom
        public uint clipId;
    }

    public class UpdateContext
    {
        Stack<ClipInfo> _clipStack;

        public bool clipped;
        public ClipInfo clipInfo;

        public float allotingZ;
        public uint workCount;
        public int gray;

        public int counter;

        public UpdateContext()
        {
            _clipStack = new Stack<ClipInfo>();
            workCount = 1;
        }

        public void Reset()
        {
            allotingZ = 0.0f;
            workCount++;
            if (workCount == 0)
                workCount = 1;
            gray = 0;
            counter = 0;
            clipped = false;

            _clipStack.Clear();
        }

        public void EnterClipping(Container container)
        {
            _clipStack.Push(clipInfo);

            Rect rect = container.GetWorldClipRect();
            if (clipped)
                rect = ToolSet.Intersection(ref clipInfo.rect, ref rect);
            clipped = true;

            clipInfo.rect = rect;
            rect.x = rect.x + rect.width / 2f;
            rect.y = rect.y + rect.height / 2f;
            rect.width /= 2f;
            rect.height /= 2f;
            if (rect.width == 0 || rect.height == 0)
            {
                clipInfo.offset = new Vector2(-2, -2);
                clipInfo.scale = new Vector2(0, 0);
            }
            else
            {
                clipInfo.offset = new Vector2(-rect.x / rect.width, -rect.y / rect.height);
                clipInfo.scale = new Vector2(1.0f / rect.width, 1.0f / rect.height);
            }

            clipInfo.clipId = container.internalIndex;

            clipInfo.soft = container.clipSoftness != null;
            if (clipInfo.soft)
            {
                clipInfo.softness = (Vector4)container.clipSoftness;
                float vx = clipInfo.rect.width * Stage.inst.stageHeight * 0.25f;
                float vy = clipInfo.rect.height * Stage.inst.stageHeight * 0.25f;

                if (clipInfo.softness.x > 0)
                    clipInfo.softness.x = vx / clipInfo.softness.x;
                else
                    clipInfo.softness.x = 10000f;

                if (clipInfo.softness.y > 0)
                    clipInfo.softness.y = vy / clipInfo.softness.y;
                else
                    clipInfo.softness.y = 10000f;

                if (clipInfo.softness.z > 0)
                    clipInfo.softness.z = vx / clipInfo.softness.z;
                else
                    clipInfo.softness.z = 10000f;

                if (clipInfo.softness.w > 0)
                    clipInfo.softness.w = vy / clipInfo.softness.w;
                else
                    clipInfo.softness.w = 10000f;
            }
        }

        public void LeaveClipping()
        {
            clipInfo = _clipStack.Pop();
            clipped = _clipStack.Count > 0;
        }
    }
}
