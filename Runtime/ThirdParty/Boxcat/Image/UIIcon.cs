#nullable enable
using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    public enum UIMeshMode : byte
    {
        ID = 0,
        FX = 4,
        FY = 5,
        FXY = 6,
        MX = 1,
        MY = 2,
        MXY = 3,
    }

    [Icon("Packages/com.unity.ugui/Runtime/ThirdParty/Boxcat/Image/UIIcon.png")]
    public sealed class UIIcon : UIImageBase
    {
        [SerializeField]
        [OnValueChanged("SetVerticesDirty"), OnValueChanged("Editor_OnSpriteChanged", InvokeOnUndoRedo = false)]
        private float _scaleFactor = 100;
        [SerializeField]
        [OnValueChanged("SetVerticesDirty"), OnValueChanged("Editor_OnSpriteChanged", InvokeOnUndoRedo = false)]
        private UIMeshMode _method;

        public float ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                _scaleFactor = value;
                SetVerticesDirty();
            }
        }

        public UIMeshMode Method
        {
            get => _method;
            set
            {
                _method = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
            UIIconMeshGen.Populate(_method, sprite, _scaleFactor, mb);
            mb.Colors.SetUp(color, mb.Poses.Count);
        }

#if UNITY_EDITOR
        private static readonly DLog _log = new(nameof(UIIcon));

        public void SetSpriteAndMatchDimension(Sprite sprite)
        {
            Sprite = sprite;
            MatchSpriteDimension();
        }

        protected override void Editor_OnSpriteChanged()
        {
            base.Editor_OnSpriteChanged();

            // skip if this is prefab instance (prevent unexpected override)
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this) is false)
            {
                MatchSpriteDimension();
            }
            else
            {
                _log.i("Cannot match size and anchor with sprite in prefab instance.");
            }
        }

        [ContextMenu("Match Size and Anchor with Sprite _m")]
        public void MatchSpriteDimension()
        {
            // ignore if the sprite is not set
            var sprite = ResolveSpriteToRender();
            if (!sprite)
            {
                _log.e("Sprite is not set, cannot match size and anchor.");
                return;
            }

            if (sprite.RefNq(Sprite))
            {
                _log.w("Previewing sprite, ignore.");
                return;
            }

            // ignore if the sprite bounds size is NaN
            if (float.IsNaN(sprite.bounds.size.x))
            {
                _log.e("Sprite bounds size is NaN, cannot match size and anchor.");
                return;
            }

            var trans = rectTransform;
            if (trans.anchorMax.Equals(trans.anchorMin) is false)
            {
                _log.w("Anchor is not equal, cannot match size and anchor.");
                return;
            }

            UnityEditor.Undo.RecordObject(trans, "");
            var oldRect = trans.rect;
            UnityEditor.Undo.RecordObject(this, ""); // for raycastInset

            // record child world position
            var orgChildWorldPos = new Vector3[trans.childCount];
            for (var i = 0; i < orgChildWorldPos.Length; i++)
            {
                var child = trans.GetChild(i);
                UnityEditor.Undo.RecordObject(child, "");
                orgChildWorldPos[i] = child.position;
            }

            // set pivot & sizeDelta
            var pivot = sprite.CalcNormPivot();
            var size = (Vector2)sprite.bounds.size * _scaleFactor;
            ProcessPivotAndSize(ref pivot, ref size, _method);
            trans.pivot = pivot;
            trans.sizeDelta = size;

            // restore child world position
            for (var i = 0; i < orgChildWorldPos.Length; i++)
                trans.GetChild(i).position = orgChildWorldPos[i];

            // adjust raycast padding
            if (raycastTarget)
                raycastInset = AdjustRaycastInset(oldRect, trans.rect, raycastInset);

            return;

            static void ProcessPivotAndSize(ref Vector2 pivot, ref Vector2 size, UIMeshMode method)
            {
                switch (method)
                {
                    case UIMeshMode.ID:
                        break;
                    case UIMeshMode.MX:
                        pivot.x = 0.5f;
                        size.x *= 2;
                        break;
                    case UIMeshMode.MY:
                        pivot.y = 0.5f;
                        size.y *= 2;
                        break;
                    case UIMeshMode.MXY:
                        pivot = new Vector2(0.5f, 0.5f);
                        size *= 2;
                        break;
                    case UIMeshMode.FX:
                        pivot.x = 1 - pivot.x;
                        break;
                    case UIMeshMode.FY:
                        pivot.y = 1 - pivot.y;
                        break;
                    case UIMeshMode.FXY:
                        pivot = new Vector2(1 - pivot.x, 1 - pivot.y);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            static Vector4 AdjustRaycastInset(Rect oldRect, Rect newRect, Vector4 padding)
            {
                padding.x += oldRect.xMin - newRect.xMin; // left
                padding.y += oldRect.yMin - newRect.yMin; // bottom
                padding.z += newRect.xMax - oldRect.xMax; // right
                padding.w += newRect.yMax - oldRect.yMax; // top
                return padding;
            }
        }
#endif
    }
}
