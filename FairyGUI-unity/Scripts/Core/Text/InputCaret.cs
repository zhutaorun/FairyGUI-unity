using UnityEngine;

namespace FairyGUI
{
    class InputCaret
    {
        public Transform cachedTransform { get; private set; }
        public QuadBatch quadBatch { get; private set; }
        public GameObject gameObject { get; private set; }
        public bool active { get; private set; }

        float _nextBlink;
        float _size;

        public InputCaret()
        {
            gameObject = new GameObject("InputCaret");
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            gameObject.layer = Stage.defaultLayer;
            Object.DontDestroyOnLoad(gameObject);

            quadBatch = new QuadBatch(gameObject);
            quadBatch.texture = NTexture.Empty;
            quadBatch.enabled = false;

            cachedTransform = gameObject.transform;
        }

        public void SetParent(Transform parent)
        {
            if (parent != null)
            {
                active = true;
                cachedTransform.parent = parent;
                gameObject.layer = parent.gameObject.layer;
                cachedTransform.localPosition = new Vector3(0, 0, -0.00001f);
                cachedTransform.localScale = new Vector3(1, 1, 1);
                _nextBlink = Time.time + 0.5f;
                quadBatch.enabled = true;

                Input.imeCompositionMode = IMECompositionMode.On;
                Vector2 cp = Stage.inst.camera.WorldToScreenPoint(cachedTransform.TransformPoint(new Vector3(0, 0, 0)));
                cp.y += _size;
                Input.compositionCursorPos = cp;
            }
            else
            {
                active = false;
                cachedTransform.parent = null;
                quadBatch.enabled = false;
                Input.imeCompositionMode = IMECompositionMode.Off;
            }
        }

        public void SetPosition(Vector2 pos)
        {
            cachedTransform.localPosition = new Vector3(pos.x, -pos.y, cachedTransform.localPosition.z);
            Vector2 cp = Stage.inst.camera.WorldToScreenPoint(cachedTransform.TransformPoint(new Vector3(0, 0, 0)));
            cp.y += _size;
            Input.compositionCursorPos = cp;

            _nextBlink = Time.time + 0.5f;
            quadBatch.enabled = true;
        }

        public void SetSizeAndColor(int size, Color color)
        {
            _size = size;
            quadBatch.Fill(new Rect(0, 0, 1, size + 1), 1, 1, new Rect(0, 0, 1, 1), color);
        }

        public void Blink()
        {
            if (_nextBlink < Time.time)
            {
                _nextBlink = Time.time + 0.5f;
                quadBatch.enabled = !quadBatch.enabled;
            }
        }
    }
}
