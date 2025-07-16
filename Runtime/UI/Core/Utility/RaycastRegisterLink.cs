#nullable enable
namespace UnityEngine.UI
{
    public struct RaycastRegisterLink
    {
        private Canvas? _canvas;

        public void Reset(Canvas? canvas, Graphic graphic)
        {
            if (_canvas.RefEq(canvas)) // no need to unregister & register again.
            {
                if (_canvas is null)
                    return;

                // If the graphic originally was raycastable, but now is not, we need to unregister it.
                var raycastable = graphic is { raycastTarget: true, isActiveAndEnabled: true };
                if (raycastable is false)
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

            if (canvas is not null && graphic is { raycastTarget: true, isActiveAndEnabled: true })
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