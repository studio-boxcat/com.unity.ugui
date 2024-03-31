namespace UnityEngine.UI
{
    /// <summary>
    /// Interface for elements that can be clipped if they are under an IClipper
    /// </summary>
    public interface IClippable
    {
        GameObject gameObject { get; }

        /// <summary>
        /// Will be called when the state of a parent IClippable changed.
        /// </summary>
        void RecalculateClipping();

        /// <summary>
        /// Set the clip rect for the IClippable.
        /// </summary>
        /// <param name="value">The Rectangle for the clipping</param>
        /// <param name="validRect">Is the rect valid.</param>
        void SetClipRect(Rect value, bool validRect);

        /// <summary>
        /// Set the clip softness for the IClippable.
        ///
        /// The softness is a linear alpha falloff over clipSoftness pixels.
        /// </summary>
        /// <param name="clipSoftness">The number of pixels to apply the softness to </param>
        void SetClipSoftness(Vector2 clipSoftness);
    }
}
