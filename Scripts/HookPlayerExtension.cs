using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(HookPlayerExtension))]
    public class HookPlayerExtension : PlayerExtension<MovementPlayerExtension>
    {
        [HGShowInSettings] [MinValue(1)] public int SpawnRateLimit;
        [HGShowInSettings] [MinValue(2)] public int SegmentLength;
        [HGShowInSettings] [MinValue(0)] public float SmoothSpeed;
        [HGShowInSettings] [MinValue(0)] public float MinDistanceBetweenPoints;
        [HGShowInSettings] [MinValue(0)] public float MaxDistanceToPlayer;

        [HGShowInBindings] [HGRequired] public MovementGrubHook HookPrefab;

        protected MovementGrubRaycast[] _raycasts;
        protected Transform _enableContainer;
        protected Transform _disableContainer;
        protected List<MovementGrubHook> _enableHooks = new List<MovementGrubHook>();
        protected List<MovementGrubHook> _disableHooks = new List<MovementGrubHook>();

        protected override void OnInitialization()
        {
            base.OnInitialization();

            _raycasts = Parent.GrubRaycasts;

            _enableContainer = new GameObject("Container - Hooks (Enabled)").transform;
            _enableContainer.transform.SetParent(Transform, false);
            _enableContainer.gameObject.SetActive(true);

            _disableContainer = new GameObject("Container - Hooks (Disabled)").transform;
            _disableContainer.transform.SetParent(Transform, false);
            _disableContainer.gameObject.SetActive(false);
        }

        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            HandleSpawn();
            HandleDespawn();

            for (var i = 0; i < _enableHooks.Count; i++)
                _enableHooks[i].OnUpdate(dt);
        }

        protected virtual void HandleSpawn()
        {
            foreach (var raycast in Parent.GrubRaycasts)
                HandleSpawn(raycast);
        }

        protected virtual void HandleSpawn(MovementGrubRaycast raycast)
        {
            var minDistanceToAnother = float.MaxValue;
            for (var i = 0; i < _enableHooks.Count; i++)
            {
                var d = Vector2.Distance(raycast.GrubPoint, _enableHooks[i].GrubPoint);
                if (d < minDistanceToAnother) minDistanceToAnother = d;
            }

            if (minDistanceToAnother > MinDistanceBetweenPoints) Spawn(raycast);
        }

        protected virtual void HandleDespawn()
        {
            for (var i = 0; i < _enableHooks.Count; i++)
            {
                var d = Vector2.Distance(Base.Transform.position, _enableHooks[i].GrubPoint);
                if (d <= MaxDistanceToPlayer) continue;

                Despawn(i);
                i--;
            }
        }

        protected virtual void Spawn(MovementGrubRaycast targetRaycast)
        {
            if (_disableHooks.Count == 0)
            {
                if (_enableHooks.Count >= SpawnRateLimit) return;
                
                _disableHooks.Add(Instantiate(HookPrefab, _disableContainer, false));
                _disableHooks[_disableHooks.Count - 1].gameObject.SetActive(false);
            }

            _disableHooks[0].name = nameof(MovementGrubHook) + " - " + targetRaycast.GrubPoint;
            _disableHooks[0].transform.SetParent(_enableContainer, false);
            _disableHooks[0].SegmentLength = SegmentLength;
            _disableHooks[0].SmoothSpeed = SmoothSpeed;
            _disableHooks[0].GrubPoint = targetRaycast.GrubPoint;
            _disableHooks[0].gameObject.SetActive(true);

            _enableHooks.Add(_disableHooks[0]);
            _disableHooks.RemoveAt(0);
        }

        protected virtual void Despawn(int i)
        {
            _enableHooks[i].gameObject.SetActive(false);
            _enableHooks[i].transform.SetParent(_disableContainer, false);
            
            _disableHooks.Add(_enableHooks[i]);
            _enableHooks.RemoveAt(i);
        }
    }
}