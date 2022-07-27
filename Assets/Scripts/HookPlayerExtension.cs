using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    /// <summary>
    /// Расширение для игрока, которое визуализирует столкновения рейкастов с препятствиями.
    /// Он создает "крюки", будто персонаж игрока хватается за препятствия.
    /// Они не привязаны к рейкастам и могут спавниться в неограниченном кол-ве.
    /// </summary>
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(HookPlayerExtension))]
    public class HookPlayerExtension : PlayerExtension<MovementPlayerExtension>
    {
        /// Лимит на кол-во "крюков"
        [HGShowInSettings] [MinValue(1)] public int SpawnRateLimit;

        /// Целевое кол-во сегментов для LineRenderer
        [HGShowInSettings] [MinValue(2)] public int SegmentLength;

        [HGShowInSettings] [MinValue(0)] public float SmoothSpeed;

        /// Минимальное расстояние между двумя "крюками", чтобы они не смогли заспавниться слишком близко
        [HGShowInSettings] [MinValue(0)] public float MinDistanceBetweenPoints;

        /// Максимальная дистанция до игрока после который "крюк" исчезнет
        [HGShowInSettings] [MinValue(0)] public float MaxDistanceToPlayer;

        /// Целевой префаб с LineRenderer
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

        /// <summary>
        /// Проверяет возможность создания объекта на основе рейкаста.
        /// </summary>
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

        /// <summary>
        /// Проверяет объекты на возможность деспавна.
        /// Если дистанция до игрока достигла лимитного значения.
        /// </summary>
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

        /// <summary>
        /// Спавн "крюка" на основе параметров рейкаста.
        /// </summary>
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

        /// <summary>
        /// Деспавн заданного "крюка".
        /// Он помещается в отключенный контейнер и будет ждать сл. возможности для спавна.
        /// </summary>
        protected virtual void Despawn(int i)
        {
            _enableHooks[i].gameObject.SetActive(false);
            _enableHooks[i].transform.SetParent(_disableContainer, false);

            _disableHooks.Add(_enableHooks[i]);
            _enableHooks.RemoveAt(i);
        }
    }
}