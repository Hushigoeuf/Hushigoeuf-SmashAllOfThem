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

    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(LevelManager))]
    public class LevelManager : HGLevelManager, HGEventListener<LevelEvent>
    {
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
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            this.HGEventStopListening<LevelEvent>();
        }

        public void OnHGEvent(LevelEvent e)
        {
            switch (e.EventType)
            {
                case LevelEventTypes.GlassBoxCrashed:
                    if (CurrentGlassBoxCount > 0)
                        CurrentGlassBoxCount--;
                    else
                        HGGameEvent.Trigger(HGGameEventTypes.FinishLevelRequest);

                    break;
            }
        }
    }
}