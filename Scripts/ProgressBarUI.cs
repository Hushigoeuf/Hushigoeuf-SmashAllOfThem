using UnityEngine;
using UnityEngine.UI;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_GUI + nameof(ProgressBarUI))]
    public class ProgressBarUI : HGMonoBehaviour
    {
        [HGShowInBindings] [HGRequired] public Image TargetImage;

        public virtual void SetValue(float value01)
        {
            TargetImage.fillAmount = value01;
        }

        public virtual void SetValue(float value, float maxValue, float minValue = 0)
        {
            SetValue(1 / (maxValue - minValue) * value);
        }
    }
}