using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    public enum InputEventTypes
    {
        ButtonDown,
        ButtonPressed,
        ButtonUp,
        AxisUpdate,
    }

    public struct InputEvent
    {
        public InputEventTypes EventType;
        public InputComponent Target;

        public string InputID => Target?.InputID;
        public string PlayerID => Target?.PlayerID;
        public string TargetID => Target?.TargetID;

        public InputEvent(InputEventTypes eventType, InputComponent target = null)
        {
            EventType = eventType;
            Target = target;
        }

        public void Trigger()
        {
            HGEventManager.TriggerEvent(this);
        }

        private static InputEvent e;

        public static void Trigger(InputEventTypes eventType, InputComponent target = null)
        {
            e.EventType = eventType;
            e.Target = target;

            e.Trigger();
        }
    }

    /// <summary>
    /// Базовый класс для компонентов управления (кнопки, оси и т.д.).
    /// </summary>
    public abstract class InputComponent
    {
        /// ID компонента, который соответствует имени из Unity InputManager
        public readonly string InputID;

        /// ID целевого игрока с которым связан этот компонент
        public readonly string PlayerID;

        /// Базовый ID без лишней информации
        public readonly string TargetID;

        public InputComponent(string playerID, string targetID)
        {
            InputID = playerID + "_" + targetID;
            PlayerID = playerID;
            TargetID = targetID;
        }

        /// <summary>
        /// Вызывает каждый кадр из менеджера.
        /// </summary>
        public virtual void Update(bool triggerEvents = true)
        {
        }

        public virtual void TriggerEvent(InputEventTypes eventType)
        {
            InputEvent.Trigger(eventType, this);
        }
    }

    /// <summary>
    /// Компонент управления - кнопка.
    /// </summary>
    public class InputButton : InputComponent
    {
        public enum ButtonStates
        {
            Disabled,
            ButtonDown,
            ButtonPressed,
            ButtonUp
        }

        public delegate void ButtonDownMethodDelegate();

        public delegate void ButtonPressedMethodDelegate();

        public delegate void ButtonUpMethodDelegate();

        public ButtonDownMethodDelegate ButtonDownMethod;
        public ButtonPressedMethodDelegate ButtonPressedMethod;
        public ButtonUpMethodDelegate ButtonUpMethod;

        public HGStateMachine<ButtonStates> State { get; protected set; }

        public InputButton(string playerID, string targetID) : base(playerID, targetID)
        {
            State = new HGStateMachine<ButtonStates>(null, false);
            State.ChangeState(ButtonStates.Disabled);
        }

        public override void Update(bool triggerEvents = true)
        {
            base.Update(triggerEvents);

            if (Input.GetButtonDown(InputID))
            {
                State.ChangeState(ButtonStates.ButtonDown);
                if (triggerEvents)
                {
                    if (ButtonDownMethod != null) ButtonDownMethod();
                    TriggerEvent(InputEventTypes.ButtonDown);
                }
            }

            if (Input.GetButton(InputID))
            {
                State.ChangeState(ButtonStates.ButtonPressed);
                if (triggerEvents)
                {
                    if (ButtonPressedMethod != null) ButtonPressedMethod();
                    TriggerEvent(InputEventTypes.ButtonPressed);
                }
            }

            if (Input.GetButtonUp(InputID))
            {
                State.ChangeState(ButtonStates.ButtonUp);
                if (triggerEvents)
                {
                    if (ButtonUpMethod != null) ButtonUpMethod();
                    TriggerEvent(InputEventTypes.ButtonUp);
                }
            }
        }
    }

    /// <summary>
    /// Компонент управления - ось.
    /// </summary>
    public class InputAxis : InputComponent
    {
        public float Value;
        public bool Smoothed;

        public InputAxis(string playerID, string targetID, bool smoothed) : base(playerID, targetID)
        {
            Smoothed = smoothed;
        }

        public override void Update(bool triggerEvents = true)
        {
            base.Update(triggerEvents);

            if (Smoothed)
                Value = Input.GetAxis(InputID);
            else Value = Input.GetAxisRaw(InputID);

            if (triggerEvents) TriggerEvent(InputEventTypes.AxisUpdate);
        }
    }

    /// <summary>
    /// Менежер для обработки управления, который привязывается к заданному игроку.
    /// Прослушивает состояние кнопок, осей.
    /// Автоматически определяет тип управления (клавиатура + мышь, контроллер или мобильное устройство).
    /// </summary>
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(InputManager))]
    public class InputManager : HGSingletonListMonoBehaviour<InputManager>, HGEventListener<HGUpdateEvent>
    {
        protected override string InstanceID => PlayerID;

        public enum InputModes
        {
            None,
            Desktop,
            Joystick,
            Mobile
        }

        /// Минимальный индекс кнопки у контроллеров
        /// Используется для определения состояния контроллера при нажатии любой из кнопок
        public static int JoystickButtonFirstIndex = 0;

        /// Максимальный индекс кнопки у контроллеров
        public static int JoystickButtonLastIndex = 19;

        [HGShowInSettings] public bool InputDetectionActive = true;

        /// ID целевого игрока 
        [HGShowInSettings] [HGRequired] public string PlayerID = "Player1";

        /// Автоматически определять тип управления
        [HGShowInSettings] public bool AutoModeDetection = true;

        /// Целевой тип управления
        [HGShowInSettings] public InputModes TargetInputMode = InputModes.None;

        /// Если подключены несколько контроллеров - принимать любой из них
        [HGShowInSettings] public bool AcceptAnyJoystick = true;

        /// Целевой индекс контроллера
        [HGShowInSettings] [MinValue(1)] [DisableIf(nameof(AcceptAnyJoystick))]
        public int TargetJoystickIndex;

        [HGShowInSettings] public bool SmoothMovement = true;

        [HGShowInSettings] [HGRequired] public string ButtonAimName = "Aim";
        [HGShowInSettings] [HGRequired] public string ButtonShootName = "Shoot";
        [HGShowInSettings] [HGRequired] public string ButtonJumpName = "Jump";
        [HGShowInSettings] [HGRequired] public string ButtonRunName = "Run";
        [HGShowInSettings] [HGRequired] public string AxisHorizontalName = "Horizontal";
        [HGShowInSettings] [HGRequired] public string AxisVerticalName = "Vertical";
        [HGShowInSettings] [HGRequired] public string AxisSecondaryHorizontalName = "SecondaryHorizontal";
        [HGShowInSettings] [HGRequired] public string AxisSecondaryVerticalName = "SecondaryVertical";

        [HGShowInBindings] public Camera TargetCamera;
        [HGShowInBindings] public Transform TargetCursor;

#if UNITY_EDITOR
        [HGDebugField] public InputModes EditorCurrentInputMode => State?.CurrentState ?? InputModes.None;
#endif

        [NonSerialized] public HGStateMachine<InputModes> State;
        [NonSerialized] public List<InputComponent> Components = new List<InputComponent>();
        [NonSerialized] public Vector3 CursorPosition;
        [NonSerialized] public Vector2 HorizontalVertical;
        [NonSerialized] public Vector2 SecondaryHorizontalVertical;

        public InputButton AimButton { get; protected set; }
        public InputButton ShootButton { get; protected set; }
        public InputButton JumpButton { get; protected set; }
        public InputButton RunButton { get; protected set; }
        public InputAxis HorizontalAxis { get; protected set; }
        public InputAxis VerticalAxis { get; protected set; }
        public InputAxis SecondaryHorizontalAxis { get; protected set; }
        public InputAxis SecondaryVerticalAxis { get; protected set; }

        public bool IsJoystickSupported => Input.GetJoystickNames().Length != 0;

        protected override void Awake()
        {
            base.Awake();

            Initialization();
        }

        protected override void Start()
        {
            base.Start();

            if (TargetCamera == null) TargetCamera = Camera.main;

            if (TargetCursor == null) TargetCursor = new GameObject().transform;
            TargetCursor.name = name + " - Cursor";
            TargetCursor.SetParent(Transform, false);

            CursorPosition = TargetCursor.position;
            CursorPosition.z = 0;
            TargetCursor.position = CursorPosition;
        }

        protected virtual void Initialization()
        {
            // Задает стартовое состояние менеджера
            // По возможности автоматически определяет его в зависимости от физического контроллера
            State = new HGStateMachine<InputModes>(gameObject, false);
            if (AutoModeDetection)
            {
#if UNITY_ANDROID || UNITY_IPHONE
                State.ChangeState(InputModes.Mobile);
#else
                if (IsJoystickSupported)
                    State.ChangeState(InputModes.Joystick);
                else State.ChangeState(InputModes.Desktop);
#endif
            }
            else
            {
                State.ChangeState(TargetInputMode);
            }

            // Инициализирует компоненты с кнопками
            {
                Components.Add(AimButton = new InputButton(PlayerID, ButtonAimName));
                Components.Add(ShootButton = new InputButton(PlayerID, ButtonShootName));
                Components.Add(JumpButton = new InputButton(PlayerID, ButtonJumpName));
                Components.Add(RunButton = new InputButton(PlayerID, ButtonRunName));
            }

            // Инициализирует компоненты с осями
            {
                Components.Add(HorizontalAxis = new InputAxis(PlayerID, AxisHorizontalName, SmoothMovement));
                Components.Add(VerticalAxis = new InputAxis(PlayerID, AxisVerticalName, SmoothMovement));
                Components.Add(SecondaryHorizontalAxis =
                    new InputAxis(PlayerID, AxisSecondaryHorizontalName, SmoothMovement));
                Components.Add(SecondaryVerticalAxis =
                    new InputAxis(PlayerID, AxisSecondaryVerticalName, SmoothMovement));
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            this.HGEventStartListening();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            this.HGEventStopListening();
        }

        protected virtual void HGOnUpdate(float dt)
        {
            HandleComponents();
            HandleAxisComponents();
            HandleCursorPosition();
            HandleInputMode();
        }

        /// <summary>
        /// Обновляет компоненты управления.
        /// </summary>
        protected virtual void HandleComponents()
        {
            for (var i = 0; i < Components.Count; i++)
                Components[i].Update(true);
        }

        /// <summary>
        /// Обновляет компоненты с осями.
        /// </summary>
        protected virtual void HandleAxisComponents()
        {
            HorizontalVertical.x = HorizontalAxis.Value;
            HorizontalVertical.y = VerticalAxis.Value;

            SecondaryHorizontalVertical.x = SecondaryHorizontalAxis.Value;
            SecondaryHorizontalVertical.y = SecondaryVerticalAxis.Value;
        }

        /// <summary>
        /// Обновляет позицию курсора.
        /// </summary>
        protected virtual void HandleCursorPosition()
        {
            if (State.CurrentState == InputModes.Desktop)
            {
                CursorPosition = TargetCamera.ScreenToWorldPoint(Input.mousePosition);
                CursorPosition.z = 0;
            }
            else
            {
                CursorPosition.x = 0;
                CursorPosition.y = 0;
            }

            TargetCursor.position = CursorPosition;
        }

        /// <summary>
        /// Обновляет тип управления.
        /// </summary>
        protected virtual void HandleInputMode()
        {
            switch (State.CurrentState)
            {
                case InputModes.Desktop:
                    if (GetJoystickAnyButton())
                        State.ChangeState(InputModes.Joystick);
                    break;

                case InputModes.Joystick:
                    if (!GetJoystickAnyButton())
                        if (Input.anyKeyDown)
                            State.ChangeState(InputModes.Desktop);
                    break;
            }
        }

        /// <summary>
        /// Определяет активность контроллера.
        /// </summary>
        protected virtual bool GetJoystickAnyButton()
        {
            var joystickButtonDowned = false;
            if (IsJoystickSupported)
                for (var i = JoystickButtonFirstIndex; i <= JoystickButtonLastIndex; i++)
                {
                    if (AcceptAnyJoystick)
                        joystickButtonDowned = Input.GetKeyDown("joystick button " + i);
                    else joystickButtonDowned = Input.GetKeyDown("joystick " + TargetJoystickIndex + " button " + i);
                    if (joystickButtonDowned) break;
                }

            return joystickButtonDowned;
        }

        /// <summary>
        /// Возвращает направление к курсору от стартовой точки.
        /// Если использует контроллер, то направление определяется на основе осей.
        /// </summary>
        public virtual Vector2 GetInputDirection(Transform point)
        {
            switch (State.CurrentState)
            {
                case InputModes.Desktop:
                    return ((Vector2) CursorPosition - (Vector2) point.position).normalized;

                case InputModes.Joystick:
                    return SecondaryHorizontalVertical;
            }

            return Vector2.zero;
        }

        public void OnHGEvent(HGUpdateEvent e)
        {
            if (e.EventType == HGUpdateEventTypes.Update)
                HGOnUpdate(e.DateTime);
        }
    }
}