using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    public enum MovementEventTypes
    {
        Grubbed,
        CloseGrubbed,
        UnGrubbed,
    }

    public struct MovementEvent
    {
        public MovementEventTypes EventType;
        public MovementGrubRaycast GrubRaycast;

        public MovementEvent(MovementEventTypes eventType, MovementGrubRaycast grubRaycast = null)
        {
            EventType = eventType;
            GrubRaycast = grubRaycast;
        }

        public void Trigger()
        {
            HGEventManager.TriggerEvent(this);
        }

        private static MovementEvent e;

        public static void Trigger(MovementEventTypes eventType, MovementGrubRaycast grubRaycast = null)
        {
            e.EventType = eventType;
            e.GrubRaycast = grubRaycast;

            e.Trigger();
        }
    }

    public class MovementGrubRaycast
    {
        public int Index;
        public bool Enabled;
        public Vector2 Direction;
        public Transform Point;
        public bool Grubbed;
        public bool CloseGrubbed;
        public Vector2 GrubPoint;
        public float GrubDistance;
    }

    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(MovementPlayerExtension))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovementPlayerExtension : PlayerExtension
    {
        public enum MovementStates
        {
            GravityMovement,
            GrubMovement,
        }

        [NonSerialized] public Vector2[] GrubRaycastDirections = new Vector2[]
        {
            new Vector2(-1, 0), // left
            new Vector2(1, 0), // right
            new Vector2(0, 1), // up
            new Vector2(0, -1), // down
            new Vector2(-1, 1), // left-up
            new Vector2(1, 1), // right-up
            new Vector2(-1, -1), // left-down
            new Vector2(1, -1), // right-down
        };

        [HGShowInSettings] [MinValue(0)] public float WalkSpeed;
        [HGShowInSettings] public LayerMask ObstacleRaycastTargetLayer;
        [HGShowInSettings] [MinValue(1)] public float ObstacleRaycastDistance;
        [HGShowInSettings] [MinValue(1)] public int ObstacleRaycastMinCount;
        [HGShowInSettings] public bool GrubFollowToTransform;

        [HGShowInSettings] [EnableIf(nameof(GrubFollowToTransform))]
        public Transform GrubFollowCustomTransform;

        [HGShowInSettings] public bool RotationNotChanged;
        [HGShowInSettings] public string AxisHorizontalName = "Horizontal";
        [HGShowInSettings] public string AxisVerticalName = "Vertical";

        [NonSerialized] public HGStateMachine<MovementStates> State;
        [NonSerialized] public int GrubRaycastEnabledCount;
        [NonSerialized] public int GrubRaycastGrubbedCount;
        [NonSerialized] public float GravityScaleOnStart;
        [NonSerialized] public MovementGrubRaycast[] GrubRaycasts;
        [NonSerialized] public bool InputEnabled;

        [HGDebugField] protected Rigidbody2D _rigidbody;
        [HGDebugField] protected Vector2 _inputDirection;
        [HGDebugField] protected float _movementSpeed;

        protected Rigidbody2D Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                    _rigidbody = GetComponent<Rigidbody2D>();
                return _rigidbody;
            }
        }

        public Vector2 Velocity => Rigidbody.velocity;
        public float Magnitude => Velocity.magnitude;

        protected override void OnInitialization()
        {
            base.OnInitialization();

            State = new HGStateMachine<MovementStates>(gameObject, false);
            State.ChangeState(MovementStates.GravityMovement);

            var container = GrubFollowCustomTransform;
            {
                if (!GrubFollowToTransform || GrubFollowCustomTransform == null)
                {
                    container = new GameObject().transform;
                    container.SetParent(Transform, false);
                }

                container.name = "Container - GrubPoints [" + GrubRaycastDirections.Length + "]";
            }

            GrubRaycasts = new MovementGrubRaycast[GrubRaycastDirections.Length];
            for (var i = 0; i < GrubRaycasts.Length; i++)
            {
                var s = new MovementGrubRaycast();

                s.Index = i;
                s.Enabled = true;
                s.Direction = GrubRaycastDirections[i];

                var point = new GameObject("GrubPoint - " + GrubRaycastDirections[i]);
                point.transform.SetParent(container, false);
                point.transform.localPosition = GrubRaycastDirections[i] * ObstacleRaycastDistance;
                s.Point = point.transform;

                GrubRaycasts[i] = s;
            }

            GravityScaleOnStart = Rigidbody.gravityScale;

            _movementSpeed = WalkSpeed;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            InputEnabled = true;
        }

        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            _inputDirection = LinkedInputManager.HorizontalVertical;

            HandleGrubRaycasts();
            HandlePlayerState();
        }

        protected virtual void HandleGrubRaycasts()
        {
            RaycastHit2D hit;

            GrubRaycastEnabledCount = 0;
            GrubRaycastGrubbedCount = 0;
            for (var i = 0; i < GrubRaycasts.Length; i++)
            {
                var oldGrubbed = GrubRaycasts[i].Grubbed;
                var oldCloseGrubbed = GrubRaycasts[i].CloseGrubbed;

                GrubRaycasts[i].Grubbed = false;
                GrubRaycasts[i].CloseGrubbed = false;
                GrubRaycasts[i].GrubPoint.x = 0;
                GrubRaycasts[i].GrubPoint.y = 0;
                GrubRaycasts[i].GrubDistance = 0;

                if (!GrubRaycasts[i].Enabled) continue;

                GrubRaycastEnabledCount++;

                hit = Physics2D.Raycast(transform.position, GetRaycastDirection(i),
                    ObstacleRaycastDistance, ObstacleRaycastTargetLayer);

                GrubRaycasts[i].Grubbed = hit.collider != null;
                if (GrubRaycasts[i].Grubbed)
                {
                    GrubRaycastGrubbedCount++;

                    GrubRaycasts[i].CloseGrubbed = hit.distance < ObstacleRaycastDistance * .5f;
                    GrubRaycasts[i].GrubPoint = hit.point;
                    GrubRaycasts[i].GrubDistance = hit.distance;
                }

                if (!oldGrubbed && GrubRaycasts[i].Grubbed)
                    MovementEvent.Trigger(MovementEventTypes.Grubbed, GrubRaycasts[i]);
                if (!oldCloseGrubbed && GrubRaycasts[i].CloseGrubbed)
                    MovementEvent.Trigger(MovementEventTypes.CloseGrubbed, GrubRaycasts[i]);
                if (oldGrubbed && !GrubRaycasts[i].Grubbed)
                    MovementEvent.Trigger(MovementEventTypes.UnGrubbed, GrubRaycasts[i]);
            }
        }

        protected virtual void HandlePlayerState()
        {
            switch (State.CurrentState)
            {
                case MovementStates.GravityMovement:
                    if (GrubRaycastGrubbedCount != 0)
                        State.ChangeState(MovementStates.GrubMovement);
                    break;

                case MovementStates.GrubMovement:
                    if (GrubRaycastGrubbedCount == 0)
                        State.ChangeState(MovementStates.GravityMovement);
                    break;
            }
        }

        public override void OnFixedUpdate(float fdt)
        {
            base.OnFixedUpdate(fdt);

            HandleInput(fdt);
            HandleGrubGravity(fdt);
            HandleGravity();

            if (RotationNotChanged)
                Rigidbody.MoveRotation(0);
        }

        protected virtual void HandleInput(float fdt)
        {
            if (!InputEnabled) return;
            if (State.CurrentState != MovementStates.GrubMovement) return;

            if (_inputDirection.magnitude > 0)
                AddForce(_movementSpeed * _inputDirection * fdt);
        }

        protected virtual void HandleGrubGravity(float fdt)
        {
            if (GrubRaycastGrubbedCount > ObstacleRaycastMinCount) return;

            for (var i = 0; i < GrubRaycasts.Length; i++)
            {
                if (!GrubRaycasts[i].Enabled) continue;
                if (!GrubRaycasts[i].Grubbed) continue;
                if (GrubRaycasts[i].CloseGrubbed) continue;

                AddForce(_movementSpeed * (GrubRaycasts[i].GrubPoint - (Vector2) Transform.position) * fdt);
            }
        }

        protected virtual void HandleGravity()
        {
            switch (State.CurrentState)
            {
                case MovementStates.GravityMovement:
                    Rigidbody.gravityScale = GravityScaleOnStart;
                    break;

                default:
                    Rigidbody.gravityScale = 0;
                    break;
            }
        }

        protected virtual Vector2 GetRaycastDirection(int i)
        {
            if (GrubFollowToTransform)
            {
                var pos = Transform.position;
                if (GrubFollowCustomTransform != null)
                    pos = GrubFollowCustomTransform.position;
                return pos - GrubRaycasts[i].Point.position;
            }

            return GrubRaycasts[i].Direction;
        }

        public virtual void AddForce(Vector2 force)
        {
            Rigidbody.AddForce(force);
        }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                if (GrubRaycastDirections.Length != 0)
                {
                    Gizmos.color = Color.red;
                    foreach (var d in GrubRaycastDirections)
                        Gizmos.DrawLine(transform.position,
                            transform.position + (Vector3) (d * ObstacleRaycastDistance));
                }

                return;
            }

            if (GrubRaycasts.Length != 0)
                foreach (var i in GrubRaycasts)
                {
                    if (!i.Enabled) continue;

                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position,
                        transform.position + (Vector3) (GetRaycastDirection(i.Index) * ObstacleRaycastDistance));

                    if (!i.Grubbed) continue;

                    Gizmos.color = !i.CloseGrubbed ? Color.red : Color.blue;
                    if (!i.CloseGrubbed) Gizmos.DrawSphere(i.GrubPoint, 0.1f);
                    else Gizmos.DrawSphere(i.GrubPoint, 0.1f);
                }
        }
#endif
    }
}