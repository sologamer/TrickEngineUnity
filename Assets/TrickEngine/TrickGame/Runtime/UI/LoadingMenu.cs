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
        
        public virtual LoadingMenu WaitFor(string waitText, Action onLoadAction, Func<float> waitCondition, float waitCompleteDelay = 0.25f, float waitConditionInterval = 0.1f)
        {
            if (WaitText != null) WaitText.text = string.IsNullOrEmpty(waitText) ? DefaultWaitText : waitText;

            IEnumerator CustomWaiter()
            {
                yield return Routine.WaitCondition(() =>
                {
                    var progress = waitCondition?.Invoke() ?? 1.0f;
                    UpdateProgress(progress);
                    return progress >= 1.0f;
                }, waitConditionInterval);
                if (waitCompleteDelay > 0) yield return Routine.WaitSeconds(waitCompleteDelay);
                Hide();
                onLoadAction?.Invoke();
            }
        
            Routine.Replace(CustomWaiter());

            return this;
        }
    
        public virtual LoadingMenu WaitForSceneLoad(string waitText, int buildIndex, Action onLoadAction)
        {
            WaitText.text = string.IsNullOrEmpty(waitText) ? DefaultWaitText : waitText;
        
            IEnumerator Waiter()
            {
                yield return Routine.WaitCondition(() => SceneManager.GetActiveScene().buildIndex == buildIndex, 0.5f);
                Hide();
                onLoadAction?.Invoke();
            }
            Routine.Replace(Waiter());
            return this;
        }
    }
}