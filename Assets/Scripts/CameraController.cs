using UnityEngine;

namespace Hushigoeuf
{
    /// <summary>
    /// Простейший контроллер для камеры, который включает или выключает объекты
    /// с Virtual Camera из Cinemachine в зависимости от состояния игры.
    /// </summary>
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(CameraController))]
    public class CameraController : HGMonoBehaviour, HGEventListener<HGGameEvent>
    {
        [HGShowInBindings] public GameObject VCamOnStart;
        [HGShowInBindings] public GameObject VCamOnPlayerDeath;
        [HGShowInBindings] public GameObject VCamOnFinished;

        protected virtual void Awake()
        {
            VCamOnStart?.HGSetActive(true);
            VCamOnPlayerDeath?.HGSetActive(false);
            VCamOnFinished?.HGSetActive(false);
        }

        protected virtual void OnEnable()
        {
            this.HGEventStartListening();
        }

        protected virtual void OnDisable()
        {
            this.HGEventStopListening();
        }

        public void OnHGEvent(HGGameEvent e)
        {
            switch (e.EventType)
            {
                case HGGameEventTypes.PlayerDeath:
                    VCamOnStart?.HGSetActive(false);
                    VCamOnPlayerDeath?.HGSetActive(true);
                    break;

                case HGGameEventTypes.LevelFinished:
                    VCamOnStart?.HGSetActive(false);
                    VCamOnFinished?.HGSetActive(true);
                    break;
            }
        }
    }
}