using UnityEngine;

namespace FairyGUI
{
    public class Shape : DisplayObject
    {
        bool _empty;
        int _lineSize;
        Color _lineColor;
        Color _fillColor;
        bool _needRebuild;

        public Shape()
        {
            this.scaleOverrided = true;
        }

        public bool empty
        {
            get { return _empty; }
        }

        public void DrawRect(float aWidth, float aHeight, int lineSize, Color lineColor, Color fillColor)
        {
            if (quadBatch == null)
            {
                quadBatch = new QuadBatch(gameObject);
                quadBatch.texture = NTexture.Empty;
                InvalidateBatchingState();
            }
            _empty = false;
            optimizeNotTouchable = false;
            contentRect = new Rect(0, 0, aWidth, aHeight);
            _needRebuild = true;
            _lineSize = lineSize;
            _lineColor = lineColor;
            _fillColor = fillColor;
        }

        public void ResizeShape(float aWidth, float aHeight)
        {
            DrawRect(aWidth, aHeight, _lineSize, _lineColor, _fillColor);
        }

        public void Clear()
        {
            _empty = true;
            optimizeNotTouchable = true;
            _needRebuild = false;
            if (quadBatch != null)
                quadBatch.enabled = false;
        }

        private void Rebuild()
        {
            _needRebuild = false;
            float rectWidth = contentRect.width * this.scaleX;
            float rectHeight = contentRect.height * this.scaleY;
            if (_lineSize == 0)
            {
                quadBatch.Fill(new Rect(0,0,rectWidth,rectHeight), 1, 1, new Rect(0, 0, 1, 1), _fillColor);
            }
            else
            {
                Vector3[] verts = new Vector3[20];
                Vector2[] uv = new Vector2[20];
                Color32[] cols = new Color32[20];

                int lineSize = Mathf.CeilToInt(Mathf.Min(_lineSize * this.scaleX, _lineSize * this.scaleY));

                Rect rect;
                //left,right
                rect = Rect.MinMaxRect(0, 0, lineSize, rectHeight);
                QuadBatch.FillVertsOfQuad(verts, 0, rect);
                rect = Rect.MinMaxRect(rectWidth - lineSize, 0, rectWidth, rectHeight);
                QuadBatch.FillVertsOfQuad(verts, 4, rect);

                //top, bottom
                rect = Rect.MinMaxRect(lineSize, 0, rectWidth - lineSize, lineSize);
                QuadBatch.FillVertsOfQuad(verts, 8, rect);
                rect = Rect.MinMaxRect(lineSize, rectHeight - lineSize, rectWidth - lineSize, rectHeight);
                QuadBatch.FillVertsOfQuad(verts, 12, rect);

                //middle
                rect = Rect.MinMaxRect(lineSize, lineSize, rectWidth - lineSize, rectHeight - lineSize);
                QuadBatch.FillVertsOfQuad(verts, 16, rect);

                rect = new Rect(0, 0, 1, 1);
                int i;
                for (i = 0; i < 5; i++)
                    QuadBatch.FillUVOfQuad(uv, i * 4, rect);

                Color32 col32 = _lineColor;
                for (i = 0; i < 16; i++)
                    cols[i] = col32;

                col32 = _fillColor;
                for (i = 16; i < 20; i++)
                    cols[i] = col32;

                quadBatch.Fill(verts, uv, cols);
            }
        }

        protected override void OverridedScale()
        {
            _needRebuild = true;
        }

        public override void Update(UpdateContext context, float parentAlpha)
        {
            if (_needRebuild)
                Rebuild();

            if (quadBatch != null)
                quadBatch.Update(context, parentAlpha * alpha);
        }

        public override void Dispose()
        {
            base.Dispose();
            quadBatch.Dispose();
        }
    }
}
