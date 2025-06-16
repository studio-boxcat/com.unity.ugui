using JetBrains.Annotations;

namespace UnityEngine.UI
{
    public struct RaycastRegisterLink
    {
        [CanBeNull] Canvas _canvas;

        public void Reset([CanBeNull] Canvas canvas, Graphic graphic)
        {
            if (ReferenceEquals(_canvas, canvas))
            {
                if (_canvas is null)
                    return;

                // If the graphic originally was raycastable, but now is not, we need to unregister it.
                var raycastable = graphic.raycastTarget && graphic.isActiveAndEnabled;
                if (raycastable == false)
                {
                    GraphicRegistry.UnregisterRaycastGraphicForCanvas(_canvas, graphic);
                    _canvas = null;
                }
                return;
            }

            if (_canvas is not null)
            {
                GraphicRegistry.UnregisterRaycastGraphicForCanvas(_canvas, graphic);
                _canvas = null;
            }

            if (canvas is not null && graphic.raycastTarget && graphic.isActiveAndEnabled)
            {
                GraphicRegistry.RegisterRaycastGraphicForCanvas(canvas, graphic);
                _canvas = canvas;
            }
        }

        public void TryUnlink(Graphic graphic)
        {
            if (_canvas is null)
                return;

            GraphicRegistry.UnregisterRaycastGraphicForCanvas(_canvas, graphic);
            _canvas = null;
        }
    }
}