using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Selectable", 35)]
    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    /// <summary>
    /// Simple selectable object - derived from to create a selectable control.
    /// </summary>
    public class Selectable
        :
        UIBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        private bool m_EnableCalled = false;

        /// <summary>
        ///Transition mode for a Selectable.
        /// </summary>
        public enum Transition : byte
        {
            /// <summary>
            /// No Transition.
            /// </summary>
            None,

            /// <summary>
            /// Use an color tint transition.
            /// </summary>
            Unused_1,

            /// <summary>
            /// Use a sprite swap transition.
            /// </summary>
            SpriteSwap,

            /// <summary>
            /// Use an animation transition.
            /// </summary>
            Animation
        }

        // Type of the transition that occurs when the button state changes.
        [FormerlySerializedAs("transition")]
        [SerializeField]
        private Transition m_Transition = Transition.None;

        // Sprites used for a Image swap-based transition.
        [FormerlySerializedAs("spriteState")]
        [SerializeField]
        private SpriteState m_SpriteState;

        [Tooltip("Can the Selectable be interacted with?")]
        [SerializeField]
        private bool m_Interactable = true;

        // Graphic that will be colored.
        [FormerlySerializedAs("highlightGraphic")]
        [FormerlySerializedAs("m_HighlightGraphic")]
        [SerializeField]
        private Graphic m_TargetGraphic;


        private bool m_GroupsAllowInteraction = true;

        /// <summary>
        /// The type of transition that will be applied to the targetGraphic when the state changes.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button btnMain;
        ///
        ///     void SomeFunction()
        ///     {
        ///         //Sets the main button's transition setting to "Color Tint".
        ///         btnMain.transition = Selectable.Transition.ColorTint;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Transition        transition        { get { return m_Transition; } set { if (SetPropertyUtility.SetStruct(ref m_Transition, value))        OnSetProperty(); } }

        /// <summary>
        /// The SpriteState for this selectable object.
        /// </summary>
        /// <remarks>
        /// Modifications will not be visible if transition is not SpriteSwap.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     //Creates an instance of a sprite state (This includes the highlighted, pressed and disabled sprite.
        ///     public SpriteState sprState = new SpriteState();
        ///     public Button btnMain;
        ///
        ///
        ///     void Start()
        ///     {
        ///         //Assigns the new sprite states to the button.
        ///         btnMain.spriteState = sprState;
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public SpriteState       spriteState       { get { return m_SpriteState; } set { if (SetPropertyUtility.SetStruct(ref m_SpriteState, value))       OnSetProperty(); } }

        /// <summary>
        /// Graphic that will be transitioned upon.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Image newImage;
        ///     public Button btnMain;
        ///
        ///     void SomeFunction()
        ///     {
        ///         //Displays the sprite transitions on the image when the transition to Highlighted,pressed or disabled is made.
        ///         btnMain.targetGraphic = newImage;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Graphic           targetGraphic     { get { return m_TargetGraphic; } set { if (SetPropertyUtility.SetClass(ref m_TargetGraphic, value))     OnSetProperty(); } }

        /// <summary>
        /// Is this object interactable.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Button startButton;
        ///     public bool playersReady;
        ///
        ///
        ///     void Update()
        ///     {
        ///         // checks if the players are ready and if the start button is useable
        ///         if (playersReady == true && startButton.interactable == false)
        ///         {
        ///             //allows the start button to be used
        ///             startButton.interactable = true;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public bool              interactable
        {
            get { return m_Interactable; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Interactable, value))
                {
                    if (!m_Interactable && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                        EventSystem.current.SetSelectedGameObject(null);
                    OnSetProperty();
                }
            }
        }

        private bool             isPointerInside   { get; set; }
        public bool              isPointerDown     { get; private set; }
        private bool             hasSelection      { get; set; }

        protected Selectable()
        {}

        /// <summary>
        /// Convenience function that converts the referenced Graphic to a Image, if possible.
        /// </summary>
        public Image image
        {
            get { return m_TargetGraphic as Image; }
            set { m_TargetGraphic = value; }
        }

        /// <summary>
        /// Convenience function to get the Animator component on the GameObject.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     private Animator buttonAnimator;
        ///     public Button button;
        ///
        ///     void Start()
        ///     {
        ///         //Assigns the "buttonAnimator" with the button's animator.
        ///         buttonAnimator = button.animator;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
#if PACKAGE_ANIMATION
        public Animator animator
        {
            get { return GetComponent<Animator>(); }
        }
#endif

        protected override void Awake()
        {
            if (m_TargetGraphic == null)
                m_TargetGraphic = GetComponent<Graphic>();
        }

        private static readonly List<CanvasGroup> m_CanvasGroupCache = new List<CanvasGroup>();
        protected override void OnCanvasGroupChanged()
        {
            // Figure out if parent groups allow interaction
            // If no interaction is alowed... then we need
            // to not do that :)
            var groupAllowInteraction = true;
            Transform t = transform;
            while (t != null)
            {
                t.GetComponents(m_CanvasGroupCache);
                bool shouldBreak = false;
                for (var i = 0; i < m_CanvasGroupCache.Count; i++)
                {
                    // if the parent group does not allow interaction
                    // we need to break
                    if (m_CanvasGroupCache[i].enabled && !m_CanvasGroupCache[i].interactable)
                    {
                        groupAllowInteraction = false;
                        shouldBreak = true;
                    }
                    // if this is a 'fresh' group, then break
                    // as we should not consider parents
                    if (m_CanvasGroupCache[i].ignoreParentGroups)
                        shouldBreak = true;
                }
                if (shouldBreak)
                    break;

                t = t.parent;
            }

            if (groupAllowInteraction != m_GroupsAllowInteraction)
            {
                m_GroupsAllowInteraction = groupAllowInteraction;
                OnSetProperty();
            }
        }

        /// <summary>
        /// Is the object interactable.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Button startButton;
        ///
        ///     void Update()
        ///     {
        ///         if (!startButton.IsInteractable())
        ///         {
        ///             Debug.Log("Start Button has been Disabled");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual bool IsInteractable()
        {
            return m_GroupsAllowInteraction && m_Interactable;
        }

        // Call from unity if animation properties have changed
        protected override void OnDidApplyAnimationProperties()
        {
            OnSetProperty();
        }

        // Select on enable and add to the list.
        protected override void OnEnable()
        {
            //Check to avoid multiple OnEnable() calls for each selectable
            if (m_EnableCalled)
                return;

            base.OnEnable();

            if (EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                hasSelection = true;
            }

            isPointerDown = false;
            DoStateTransition(isPointerDown);

            m_EnableCalled = true;
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            // If our parenting changes figure out if we are under a new CanvasGroup.
            OnCanvasGroupChanged();
        }

        private void OnSetProperty()
        {
            DoStateTransition(isPointerDown);
        }

        // Remove from the list.
        protected override void OnDisable()
        {
            //Check to avoid multiple OnDisable() calls for each selectable
            if (!m_EnableCalled)
                return;

            InstantClearState();
            base.OnDisable();

            m_EnableCalled = false;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsPressed())
            {
                InstantClearState();
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // OnValidate can be called before OnEnable, this makes it unsafe to access other components
            // since they might not have been initialized yet.
            // OnSetProperty potentially access Animator or Graphics. (case 618186)
            if (isActiveAndEnabled)
            {
                if (!interactable && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                    EventSystem.current.SetSelectedGameObject(null);
                // Need to clear out the override image on the target...
                DoSpriteSwap(null);

                // If the transition mode got changed, we need to clear all the transitions, since we don't know what the old transition mode was.
                TriggerAnimation(AnimationTriggers.Normal);

                // And now go to the right state.
                DoStateTransition(isPointerDown);
            }
        }

        protected override void Reset()
        {
            m_TargetGraphic = GetComponent<Graphic>();
        }

#endif // if UNITY_EDITOR

        /// <summary>
        /// Clear any internal state from the Selectable (used when disabling).
        /// </summary>
        protected virtual void InstantClearState()
        {
            isPointerInside = false;
            isPointerDown = false;
            hasSelection = false;

            switch (m_Transition)
            {
                case Transition.SpriteSwap:
                    DoSpriteSwap(null);
                    break;
                case Transition.Animation:
                    TriggerAnimation(AnimationTriggers.Normal);
                    break;
            }
        }

        /// <summary>
        /// Transition the Selectable to the entered state.
        /// </summary>
        /// <param name="state">State to transition to</param>
        protected virtual void DoStateTransition(bool pressed)
        {
            // XXX: ????????? ??????????????? ????????? ?????? ??????.
            if (m_Transition == Transition.None)
                return;

            if (!gameObject.activeInHierarchy)
                return;

            switch (m_Transition)
            {
                case Transition.SpriteSwap:
                    DoSpriteSwap(pressed ? m_SpriteState.pressedSprite : null);
                    break;
                case Transition.Animation:
                    var triggerName = pressed ? AnimationTriggers.Pressed : AnimationTriggers.Normal;
                    TriggerAnimation(triggerName);
                    break;
            }
        }

        void DoSpriteSwap(Sprite newSprite)
        {
            if (image == null)
                return;

            image.overrideSprite = newSprite;
        }

        void TriggerAnimation(int triggerId)
        {
#if PACKAGE_ANIMATION
            if (transition != Transition.Animation || animator == null || !animator.isActiveAndEnabled || !animator.hasBoundPlayables)
                return;

            animator.ResetTrigger(AnimationTriggers.Normal);
            animator.ResetTrigger(AnimationTriggers.Pressed);

            animator.SetTrigger(triggerId);
#endif
        }

        /// <summary>
        /// Returns whether the selectable is currently 'highlighted' or not.
        /// </summary>
        /// <remarks>
        /// Use this to check if the selectable UI element is currently highlighted.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// //Create a UI element. To do this go to Create>UI and select from the list. Attach this script to the UI GameObject to see this script working. The script also works with non-UI elements, but highlighting works better with UI.
        ///
        /// using UnityEngine;
        /// using UnityEngine.Events;
        /// using UnityEngine.EventSystems;
        /// using UnityEngine.UI;
        ///
        /// //Use the Selectable class as a base class to access the IsHighlighted method
        /// public class Example : Selectable
        /// {
        ///     //Use this to check what Events are happening
        ///     BaseEventData m_BaseEvent;
        ///
        ///     void Update()
        ///     {
        ///         //Check if the GameObject is being highlighted
        ///         if (IsHighlighted())
        ///         {
        ///             //Output that the GameObject was highlighted, or do something else
        ///             Debug.Log("Selectable is Highlighted");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        protected bool IsHighlighted()
        {
            if (!IsActive() || !IsInteractable())
                return false;
            return isPointerInside && !isPointerDown && !hasSelection;
        }

        /// <summary>
        /// Whether the current selectable is being pressed.
        /// </summary>
        protected bool IsPressed()
        {
            if (!IsActive() || !IsInteractable())
                return false;
            return isPointerDown;
        }

        // Change the button to the correct state
        private void EvaluateAndTransitionToSelectionState()
        {
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(isPointerDown);
        }

        /// <summary>
        /// Evaluate current state and transition to pressed state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerDownHandler// required interface when using the OnPointerDown method.
        /// {
        ///     //Do this when the mouse is clicked over the selectable object this script is attached to.
        ///     public void OnPointerDown(PointerEventData eventData)
        ///     {
        ///         Debug.Log(this.gameObject.name + " Was Clicked.");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // Selection tracking
            if (IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            isPointerDown = true;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Evaluate eventData and transition to appropriate state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerUpHandler, IPointerDownHandler// These are the interfaces the OnPointerUp method requires.
        /// {
        ///     //OnPointerDown is also required to receive OnPointerUp callbacks
        ///     public void OnPointerDown(PointerEventData eventData)
        ///     {
        ///     }
        ///
        ///     //Do this when the mouse click on this selectable UI object is released.
        ///     public void OnPointerUp(PointerEventData eventData)
        ///     {
        ///         Debug.Log("The mouse click was released");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            isPointerDown = false;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Evaluate current state and transition to appropriate state.
        /// New state could be pressed or hover depending on pressed state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerEnterHandler// required interface when using the OnPointerEnter method.
        /// {
        ///     //Do this when the cursor enters the rect area of this selectable UI object.
        ///     public void OnPointerEnter(PointerEventData eventData)
        ///     {
        ///         Debug.Log("The cursor entered the selectable UI element.");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Evaluate current state and transition to normal state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerExitHandler// required interface when using the OnPointerExit method.
        /// {
        ///     //Do this when the cursor exits the rect area of this selectable UI object.
        ///     public void OnPointerExit(PointerEventData eventData)
        ///     {
        ///         Debug.Log("The cursor exited the selectable UI element.");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Set selection and transition to appropriate state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, ISelectHandler// required interface when using the OnSelect method.
        /// {
        ///     //Do this when the selectable UI object is selected.
        ///     public void OnSelect(BaseEventData eventData)
        ///     {
        ///         Debug.Log(this.gameObject.name + " was selected");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnSelect(BaseEventData eventData)
        {
            hasSelection = true;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Unset selection and transition to appropriate state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IDeselectHandler //This Interface is required to receive OnDeselect callbacks.
        /// {
        ///     public void OnDeselect(BaseEventData data)
        ///     {
        ///         Debug.Log("Deselected");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnDeselect(BaseEventData eventData)
        {
            hasSelection = false;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Selects this Selectable.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour// required interface when using the OnSelect method.
        /// {
        ///     public InputField myInputField;
        ///
        ///     //Do this OnClick.
        ///     public void SaveGame()
        ///     {
        ///         //Makes the Input Field the selected UI Element.
        ///         myInputField.Select();
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void Select()
        {
            if (EventSystem.current == null || EventSystem.current.alreadySelecting)
                return;

            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
