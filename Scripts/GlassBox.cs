using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(GlassBox))]
    public class GlassBox : HGMonoBehaviour
    {
        [HGShowInSettings] [Range(0, 1)] public float MinStrengthToHit;
        [HGShowInSettings] [Range(0, 1)] public float MinMagnitudeToHit;
        [HGShowInSettings] [MinValue(0)] public float SpeedToForceSegment;

        [HGShowInBindings] [HGRequired] public GameObject ObjectBeforeHit;
        [HGShowInBindings] [HGRequired] public GameObject ObjectAfterHit;
        [HGShowInBindings] [HGRequired] public GameObject FragmentContainer;

        protected virtual void OnEnable()
        {
            ObjectBeforeHit.HGSetActive(true);
            ObjectAfterHit.HGSetActive(false);
        }

        protected virtual void Crash(Vector3 position, Vector2 direction, float strength01)
        {
            ObjectBeforeHit.HGSetActive(false);
            ObjectAfterHit.HGSetActive(true);

            var rbs = FragmentContainer.GetComponentsInChildren<Rigidbody2D>();
            var maxDistance = float.MinValue;
            for (var i = 0; i < rbs.Length; i++)
            {
                var d = Vector2.Distance(position, rbs[i].position);
                if (d > maxDistance) maxDistance = d;
            }

            for (var i = 0; i < rbs.Length; i++)
            {
                var d = Vector2.Distance(position, rbs[i].position);
                rbs[i].AddForce(SpeedToForceSegment * direction * strength01 * Mathf.Clamp01(1.5f - d / maxDistance));
            }

            LevelEvent.Trigger(LevelEventTypes.GlassBoxCrashed);
        }

        public virtual void Crash(GameObject target)
        {
            if (!isActiveAndEnabled) return;

            var player = target.GetComponent<Player>();
            if (player == null) return;
            var j = player.GetExtension<JumpPlayerExtension>();
            if (j == null) return;

            if (j.LastJumpStrength01 < MinStrengthToHit) return;
            if (j.LastJumpMagnitude01 < MinMagnitudeToHit) return;

            Crash(player.Transform.position, j.LastJumpDirection, j.LastJumpStrength01);
        }
    }
}