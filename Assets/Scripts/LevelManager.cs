using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    public enum LevelEventTypes
    {
        GlassBoxCrashed,
    }

    public struct LevelEvent
    {
        public LevelEventTypes EventType;

        public LevelEvent(LevelEventTypes eventType)
        {
            EventType = eventType;
        }

        private static LevelEvent e;

        public static void Trigger(LevelEventTypes eventType)
        {
            e.EventType = eventType;

            HGEventManager.TriggerEvent(e);
        }
    }

    /// <summary>
    /// В дополнении обрабатывает столкновения игрока со стеклянными контейнерами и
    /// если все они были разрушены, то завершает уровень.
    /// </summary>
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(LevelManager))]
    public class LevelManager : HGLevelManager, HGEventListener<LevelEvent>, HGEventListener<HGUpdateEvent>
    {
        /// Целевое кол-во контейнеров, которое нужно разрушить для победы
        [HGShowInSettings] [MinValue(1)] public int TargetGlassBoxCount;

        protected int CurrentGlassBoxCount;

        protected override void Start()
        {
            base.Start();

            CurrentGlassBoxCount = TargetGlassBoxCount;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            this.HGEventStartListening<LevelEvent>();
            this.HGEventStartListening<HGUpdateEvent>();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            this.HGEventStopListening<LevelEvent>();
            this.HGEventStopListening<HGUpdateEvent>();
        }

        protected virtual void HGOnUpdate(float dt)
        {
            if (Input.GetKeyDown(KeyCode.R))
                HGGameEvent.Trigger(HGGameEventTypes.GameOverRequest);
            else if (Input.GetKeyDown(KeyCode.Escape))
                HGGameEvent.Trigger(HGGameEventTypes.FinishLevelRequest);
        }

        public void OnHGEvent(LevelEvent e)
        {
            switch (e.EventType)
            {
                case LevelEventTypes.GlassBoxCrashed:
                    if (CurrentGlassBoxCount > 0)
                        CurrentGlassBoxCount--;
                    if (CurrentGlassBoxCount <= 0)
                        HGGameEvent.Trigger(HGGameEventTypes.FinishLevelRequest);

                    break;
            }
        }

        public void OnHGEvent(HGUpdateEvent e)
        {
            if (e.EventType == HGUpdateEventTypes.Update)
                HGOnUpdate(e.DateTime);
        }
    }
}