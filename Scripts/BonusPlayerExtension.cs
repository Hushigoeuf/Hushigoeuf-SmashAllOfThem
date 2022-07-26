using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(BonusPlayerExtension))]
    public class BonusPlayerExtension : PlayerExtension, HGEventListener<PlayerEvent>
    {
        [HGShowInSettings] [MinValue(0)] public float SpeedCaused;
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

                case PlayerEventTypes.StopJumping:
                    var stopJumping = (JumpPlayerExtension) e.Target;
                    if (stopJumping != null) stopJumping.Speed = stopJumping.SpeedOnStart;
                    break;
            }
        }
    }
}