namespace UnityEngine.UI
{
    /// <summary>
    ///   A component is treated as a layout element by the auto layout system if it implements ILayoutElement.
    /// </summary>
    /// <remarks>
    /// The layout system will invoke CalculateLayoutInputHorizontal before querying minWidth, preferredWidth, and flexibleWidth. It can potentially save performance if these properties are cached when CalculateLayoutInputHorizontal is invoked, so they don't need to be recalculated every time the properties are queried.
    ///
    /// The layout system will invoke CalculateLayoutInputVertical before querying minHeight, preferredHeight, and flexibleHeight.It can potentially save performance if these properties are cached when CalculateLayoutInputVertical is invoked, so they don't need to be recalculated every time the properties are queried.
    ///
    /// The minWidth, preferredWidth, and flexibleWidth properties should not rely on any properties of the RectTransform of the layout element, otherwise the behavior will be non-deterministic.
    /// The minHeight, preferredHeight, and flexibleHeight properties may rely on horizontal aspects of the RectTransform, such as the width or the X component of the position.
    /// Any properties of the RectTransforms on child layout elements may always be relied on.
    /// </remarks>
    public interface ILayoutElement
    {
        /// <summary>
        /// After this method is invoked, layout horizontal input properties should return up-to-date values.
        ///  Children will already have up-to-date layout horizontal inputs when this methods is called.
        /// </summary>
        void CalculateLayoutInputHorizontal() { }

        /// <summary>
        ///After this method is invoked, layout vertical input properties should return up-to-date values.
        ///Children will already have up-to-date layout vertical inputs when this methods is called.
        /// </summary>
        void CalculateLayoutInputVertical() { }

        float minWidth => -1;
        float preferredWidth => -1;
        float minHeight => -1;
        float preferredHeight => -1;
        int layoutPriority => 0;
    }

    /// <summary>
    /// Base interface to be implemented by components that control the layout of RectTransforms.
    /// </summary>
    /// <remarks>
    /// If a component is driving its own RectTransform it should implement the interface [[ILayoutSelfController]].
    /// If a component is driving the RectTransforms of its children, it should implement [[ILayoutGroup]].
    ///
    /// The layout system will first invoke SetLayoutHorizontal and then SetLayoutVertical.
    ///
    /// In the SetLayoutHorizontal call it is valid to call LayoutUtility.GetMinWidth, LayoutUtility.GetPreferredWidth, and LayoutUtility.GetFlexibleWidth on the RectTransform of itself or any of its children.
    /// In the SetLayoutVertical call it is valid to call LayoutUtility.GetMinHeight, LayoutUtility.GetPreferredHeight, and LayoutUtility.GetFlexibleHeight on the RectTransform of itself or any of its children.
    ///
    /// The component may use this information to determine the width and height to use for its own RectTransform or the RectTransforms of its children.
    /// </remarks>
    public interface ILayoutController
    {
        /// <summary>
        /// Callback invoked by the auto layout system which handles horizontal aspects of the layout.
        /// </summary>
        void SetLayoutHorizontal() { }

        /// <summary>
        /// Callback invoked by the auto layout system which handles vertical aspects of the layout.
        /// </summary>
        void SetLayoutVertical() { }
    }

    /// <summary>
    /// ILayoutGroup is an ILayoutController that should drive the RectTransforms of its children.
    /// </summary>
    /// <remarks>
    /// ILayoutGroup derives from ILayoutController and requires the same members to be implemented.
    /// </remarks>
    public interface ILayoutGroup : ILayoutController
    {
    }

    /// <summary>
    /// ILayoutSelfController is an ILayoutController that should drive its own RectTransform.
    /// </summary>
    /// <remarks>
    /// The iLayoutSelfController derives from the base controller [[ILayoutController]] and controls the layout of a RectTransform.
    ///
    /// Use the ILayoutSelfController to manipulate a GameObject’s own RectTransform component, which you attach in the Inspector.Use ILayoutGroup to manipulate RectTransforms belonging to the children of the GameObject.
    ///
    /// Call ILayoutController.SetLayoutHorizontal to handle horizontal parts of the layout, and call ILayoutController.SetLayoutVertical to handle vertical parts.
    /// You can change the height, width, position and rotation of the RectTransform.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// //This script shows how the GameObject’s own RectTransforms can be changed.
    /// //This creates a rectangle on the screen of the scale, positition and rotation you define in the Inspector.
    /// //Make sure to set the X and Y scale to be more than 0 to see it
    ///
    /// using UnityEngine;
    /// using UnityEngine.UI;
    /// using UnityEngine.EventSystems;
    ///
    /// public class Example : UIBehaviour, ILayoutSelfController
    /// {
    ///     //Fields in the inspector used to manipulate the RectTransform
    ///     public Vector3 m_Position;
    ///     public Vector3 m_Rotation;
    ///     public Vector2 m_Scale;
    ///
    ///     //This handles horizontal aspects of the layout (derived from ILayoutController)
    ///     public virtual void SetLayoutHorizontal()
    ///     {
    ///         //Move and Rotate the RectTransform appropriately
    ///         UpdateRectTransform();
    ///     }
    ///
    ///     //This handles vertical aspects of the layout
    ///     public virtual void SetLayoutVertical()
    ///     {
    ///         //Move and Rotate the RectTransform appropriately
    ///         UpdateRectTransform();
    ///     }
    ///
    ///     //This tells when there is a change in the inspector
    ///     #if UNITY_EDITOR
    ///     protected override void OnValidate()
    ///     {
    ///         Debug.Log("Validate");
    ///         //Update the RectTransform position, rotation and scale
    ///         UpdateRectTransform();
    ///     }
    ///
    ///     #endif
    ///
    ///     //This tells when there has been a change to the RectTransform's settings in the inspector
    ///     protected override void OnRectTransformDimensionsChange()
    ///     {
    ///         //Update the RectTransform position, rotation and scale
    ///         UpdateRectTransform();
    ///     }
    ///
    ///     void UpdateRectTransform()
    ///     {
    ///         //Fetch the RectTransform from the GameObject
    ///         RectTransform rectTransform = GetComponent<RectTransform>();
    ///
    ///         //Change the scale of the RectTransform using the fields in the inspector
    ///         rectTransform.localScale = new Vector3(m_Scale.x, m_Scale.y, 0);
    ///
    ///         //Change the position and rotation of the RectTransform
    ///         rectTransform.SetPositionAndRotation(m_Position, Quaternion.Euler(m_Rotation));
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public interface ILayoutSelfController : ILayoutController
    {
    }
}
