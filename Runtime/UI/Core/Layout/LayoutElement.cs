using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Layout Element", 140)]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    /// <summary>
    /// Add this component to a GameObject to make it into a layout element or override values on an existing layout element.
    /// </summary>
    public class LayoutElement : UIBehaviour, ILayoutElement
    {
        [SerializeField] private float m_MinWidth = -1;
        [SerializeField] private float m_MinHeight = -1;
        [SerializeField] private float m_PreferredWidth = -1;
        [SerializeField] private float m_PreferredHeight = -1;
        [SerializeField] private float m_FlexibleWidth = -1;
        [SerializeField] private float m_FlexibleHeight = -1;
        [SerializeField] private int m_LayoutPriority = 1;

        /// <summary>
        /// The minimum width this layout element may be allocated.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Transform MyContentPanel;
        ///
        ///     //Sets the flexible height on on all children in the content panel.
        ///     public void Start()
        ///     {
        ///         //Assign all the children of the content panel to an array.
        ///         LayoutElement[] myLayoutElements = MyContentPanel.GetComponentsInChildren<LayoutElement>();
        ///
        ///         //For each child in the array change its LayoutElement's minimum width size to 200.
        ///         foreach (LayoutElement element in myLayoutElements)
        ///         {
        ///             element.minWidth = 200f;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual float minWidth { get { return m_MinWidth; } set { if (SetPropertyUtility.SetValue(ref m_MinWidth, value)) SetDirty(); } }

        /// <summary>
        /// The minimum height this layout element may be allocated.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Transform MyContentPanel;
        ///
        ///     //Sets the flexible height on on all children in the content panel.
        ///     public void Start()
        ///     {
        ///         //Assign all the children of the content panel to an array.
        ///         LayoutElement[] myLayoutElements = MyContentPanel.GetComponentsInChildren<LayoutElement>();
        ///
        ///         //For each child in the array change its LayoutElement's minimum height size to 64.
        ///         foreach (LayoutElement element in myLayoutElements)
        ///         {
        ///             element.minHeight = 64f;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual float minHeight { get { return m_MinHeight; } set { if (SetPropertyUtility.SetValue(ref m_MinHeight, value)) SetDirty(); } }

        /// <summary>
        /// The preferred width this layout element should be allocated if there is sufficient space. The preferredWidth can be set to -1 to remove the size.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Transform MyContentPanel;
        ///
        ///     //Sets the flexible height on on all children in the content panel.
        ///     public void Start()
        ///     {
        ///         //Assign all the children of the content panel to an array.
        ///         LayoutElement[] myLayoutElements = MyContentPanel.GetComponentsInChildren<LayoutElement>();
        ///
        ///         //For each child in the array change its LayoutElement's preferred width size to 250.
        ///         foreach (LayoutElement element in myLayoutElements)
        ///         {
        ///             element.preferredWidth = 250f;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual float preferredWidth { get { return m_PreferredWidth; } set { if (SetPropertyUtility.SetValue(ref m_PreferredWidth, value)) SetDirty(); } }

        /// <summary>
        /// The preferred height this layout element should be allocated if there is sufficient space.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Transform MyContentPanel;
        ///
        ///     //Sets the flexible height on on all children in the content panel.
        ///     public void Start()
        ///     {
        ///         //Assign all the children of the content panel to an array.
        ///         LayoutElement[] myLayoutElements = MyContentPanel.GetComponentsInChildren<LayoutElement>();
        ///
        ///         //For each child in the array change its LayoutElement's preferred height size to 100.
        ///         foreach (LayoutElement element in myLayoutElements)
        ///         {
        ///             element.preferredHeight = 100f;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual float preferredHeight { get { return m_PreferredHeight; } set { if (SetPropertyUtility.SetValue(ref m_PreferredHeight, value)) SetDirty(); } }

        /// <summary>
        /// The extra relative width this layout element should be allocated if there is additional available space.
        /// </summary>
        public virtual float flexibleWidth { get { return m_FlexibleWidth; } set { if (SetPropertyUtility.SetValue(ref m_FlexibleWidth, value)) SetDirty(); } }

        /// <summary>
        /// The extra relative height this layout element should be allocated if there is additional available space.
        /// </summary>
        public virtual float flexibleHeight { get { return m_FlexibleHeight; } set { if (SetPropertyUtility.SetValue(ref m_FlexibleHeight, value)) SetDirty(); } }

        /// <summary>
        /// The Priority of layout this element has.
        /// </summary>
        public virtual int layoutPriority { get { return m_LayoutPriority; } set { if (SetPropertyUtility.SetValue(ref m_LayoutPriority, value)) SetDirty(); } }


        protected LayoutElement()
        {}

        protected virtual void OnEnable()
        {
            SetDirty();
        }

        protected virtual void OnTransformParentChanged()
        {
            SetDirty();
        }

        protected virtual void OnDisable()
        {
            SetDirty();
        }

        protected virtual void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        protected virtual void OnBeforeTransformParentChanged()
        {
            SetDirty();
        }

        /// <summary>
        /// Mark the LayoutElement as dirty.
        /// </summary>
        /// <remarks>
        /// This will make the auto layout system process this element on the next layout pass. This method should be called by the LayoutElement whenever a change is made that potentially affects the layout.
        /// </remarks>
        protected void SetDirty()
        {
            if (!IsActive())
                return;
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

    #if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            SetDirty();
        }

    #endif
    }
}
