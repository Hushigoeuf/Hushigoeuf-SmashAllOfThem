using System;
using UnityEngine;

namespace Hushigoeuf
{
    public enum PlayerEventTypes
    {
        PlayerDeath,
        StartShooting,
        StopShooting,
        StartJumping,
        Jumped,
        StopJumping,
    }

    public struct PlayerEvent
    {
        public Player Player;
        public PlayerEventTypes EventType;
        public IPlayerExtension Target;

        public string PlayerID => Player?.PlayerID;

        public PlayerEvent(Player player, PlayerEventTypes eventType, IPlayerExtension target = null)
        {
            Player = player;
            EventType = eventType;
            Target = target;
        }

        private static PlayerEvent e;

        public static void Trigger(Player player, PlayerEventTypes eventType, IPlayerExtension target = null)
        {
            e.Player = player;
            e.EventType = eventType;
            e.Target = target;

            HGEventManager.TriggerEvent(e);
        }
    }

    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(Player))]
    public class Player : HGSingletonExtension<Player, IPlayerExtension>
    {
        [HGShowInSettings] [HGRequired] public string PlayerID = "Player1";

        [NonSerialized] public InputManager LinkedInputManager;

        protected override void OnInitialization()
        {
            base.OnInitialization();

            LinkedInputManager = InputManager.GetInstance(PlayerID);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Death();
        }

        public void Death()
        {
            OnDeath();
        }

        public virtual void TriggerEvent(PlayerEventTypes eventType, IPlayerExtension target = null)
        {
            PlayerEvent.Trigger(this, eventType, target);
        }

        protected virtual void OnDeath()
        {
            for (var i = 0; i < Extensions.Count; i++)
                Extensions[i].OnDeath();
            TriggerEvent(PlayerEventTypes.PlayerDeath);
            HGGameEvent.Trigger(HGGameEventTypes.PlayerDeath);
        }
    }

    public interface IPlayerExtension
    {
        public string PlayerID { get; }
        public InputManager LinkedInputManager { get; }
        void OnDeath();
    }

    public abstract class PlayerExtension : HGExtension<Player>, IPlayerExtension
    {
        public string PlayerID => Base.PlayerID;
        public InputManager LinkedInputManager => Base.LinkedInputManager;

        public virtual void OnDeath()
        {
        }
    }

    public abstract class PlayerExtension<TLinked> : HGExtension<Player, TLinked>, IPlayerExtension
        where TLinked : HGExtension<Player>
    {
        public string PlayerID => Base.PlayerID;
        public InputManager LinkedInputManager => Base.LinkedInputManager;

        public virtual void OnDeath()
        {
        }
    }
}