using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    /// <summary>
    /// Расширение для персонажа, которое отвечает за бонусную систему,
    /// которая заключается в более сильных прыжках.
    /// </summary>
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(BonusPlayerExtension))]
    public class BonusPlayerExtension : PlayerExtension, HGEventListener<PlayerEvent>
    {
        /// На сколько увеличить силу прыжков после получения бонуса
        [HGShowInSettings] [MinValue(0)] public float SpeedCaused;

        /// Кол-во бонусов на самом старте
        [HGShowInSettings] [MinValue(0)] public int CountOnStart;

        [NonSerialized] public int BonusCount;

        protected override void OnInitialization()
        {
            base.OnInitialization();

            BonusCount = CountOnStart;
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

        public void OnHGEvent(PlayerEvent e)
        {
            switch (e.EventType)
            {
                // Как только начинается прыжок - меняем его силу (если есть бонусы в наличии)
                case PlayerEventTypes.StartJumping:
                    if (BonusCount > 0)
                    {
                        var startJumping = (JumpPlayerExtension) e.Target;
                        if (startJumping != null)
                        {
                            startJumping.Speed = startJumping.SpeedOnStart + SpeedCaused;
                            BonusCount--;
                        }
                    }

                    break;

                // После прыжка возвращаем параметры в прежнее состояние
                case PlayerEventTypes.StopJumping:
                    var stopJumping = (JumpPlayerExtension) e.Target;
                    if (stopJumping != null) stopJumping.Speed = stopJumping.SpeedOnStart;
                    break;
            }
        }
    }
}