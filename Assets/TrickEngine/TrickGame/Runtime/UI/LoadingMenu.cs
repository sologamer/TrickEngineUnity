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

        public virtual void UpdateProgress(float progress)
        {
            FillProgress.fillAmount = progress;
            ProgressText.text = $"{progress * 100.0f:F0}%";
        }
        
        public LoadingMenu WaitFor(string waitText, Action onLoadAction, Func<float> waitCondition, float waitCompleteDelay = 0.25f, float waitConditionInterval = 0.1f)
        {
            WaitText.text = string.IsNullOrEmpty(waitText) ? DefaultWaitText : waitText;
        
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
        
            Routine.Start(CustomWaiter());

            return this;
        }
    
        public LoadingMenu WaitForSceneLoad(string waitText, int buildIndex, Action onLoadAction)
        {
            WaitText.text = string.IsNullOrEmpty(waitText) ? DefaultWaitText : waitText;
        
            IEnumerator Waiter()
            {
                yield return Routine.WaitCondition(() => SceneManager.GetActiveScene().buildIndex == buildIndex, 0.5f);
                Hide();
                onLoadAction?.Invoke();
            }
            Routine.Start(Waiter());
            return this;
        }
    }
}