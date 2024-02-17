using System;
using System.Collections;
using BeauRoutine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrickCore
{
    public class LoadingMenu : UIMenu
    {
        public bool SelfManaged = true;
        public string DefaultWaitText = "LOADING...";
        public TextMeshProUGUI WaitText;
        public Image FillProgress;
        public TextMeshProUGUI ProgressText;

        protected Routine Routine { get; set; }

        public virtual void UpdateProgress(float progress)
        {
            if (FillProgress != null) FillProgress.fillAmount = progress;
            if (ProgressText != null) ProgressText.text = $"{progress * 100.0f:F0}%";
        }
        
        public IEnumerator WaitForRoutine(string waitText, Action<LoadingMenu> onLoadAction, Func<float> waitCondition, float waitCompleteDelay = 0.25f, float waitConditionInterval = 0.1f)
        {
            yield return WaitFor(waitText, onLoadAction, waitCondition, waitCompleteDelay, waitConditionInterval);
        }

        public virtual LoadingMenu WaitFor(string waitText, Action<LoadingMenu> onLoadAction, Func<float> waitCondition, float waitCompleteDelay = 0.25f, float waitConditionInterval = 0.1f)
        {
            if (WaitText != null) WaitText.text = string.IsNullOrEmpty(waitText) ? DefaultWaitText : waitText;
            Routine.Replace(CustomWaiter());

            return this;

            IEnumerator CustomWaiter()
            {
                yield return Routine.WaitCondition(() =>
                {
                    var progress = waitCondition?.Invoke() ?? 1.0f;
                    UpdateProgress(progress);
                    return progress >= 1.0f;
                }, waitConditionInterval);
                if (waitCompleteDelay > 0) yield return Routine.WaitSeconds(waitCompleteDelay);
                if (!SelfManaged) Hide();
                onLoadAction?.Invoke(this);
            }
        }
    
        public virtual LoadingMenu WaitForSceneLoad(string waitText, int buildIndex, Action<LoadingMenu> onLoadAction)
        {
            WaitText.text = string.IsNullOrEmpty(waitText) ? DefaultWaitText : waitText;
            Routine.Replace(Waiter());
            return this;

            IEnumerator Waiter()
            {
                yield return Routine.WaitCondition(() => SceneManager.GetActiveScene().buildIndex == buildIndex, 0.5f);
                if (!SelfManaged) Hide();
                onLoadAction?.Invoke(this);
            }
        }
    }
}