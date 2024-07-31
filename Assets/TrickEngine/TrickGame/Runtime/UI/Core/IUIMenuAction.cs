using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrickCore
{
    public interface IUIMenuAction
    {
        void ExecuteShow(UIMenu menu);
        void ExecuteHide(UIMenu menu);
    }

    public class HandleSortingOrderMenuAction : IUIMenuAction
    {
        public void ExecuteShow(UIMenu menu)
        {
            
        }

        public void ExecuteHide(UIMenu menu)
        {
            if (menu.MenuCanvas != null) menu.MenuCanvas.sortingOrder = menu.StartingSortingOrder;
        }
    }
    

    public class HandleCallbackMenuAction : IUIMenuAction
    {
        public void ExecuteShow(UIMenu menu)
        {
            
        }

        public void ExecuteHide(UIMenu menu)
        {
            if (menu.HideCallbackOnceQueue.Count > 0)
            {
                menu.HideCallbackOnceQueue.Pop()?.Invoke();
            }
        }
    }
    
    public class HandleMainCameraMenuAction : IUIMenuAction
    {
        public void ExecuteShow(UIMenu menu)
        {
            if (!menu.DisableMainCamera) return;
            if (menu.MainCamera == null) menu.MainCamera = Camera.main;
            if (menu.MainCamera != null) menu.MainCamera.enabled = false;
        }

        public void ExecuteHide(UIMenu menu)
        {
            if (!menu.DisableMainCamera) return;
            if (menu.MainCamera == null) menu.MainCamera = Camera.main;
            if (menu.MainCamera != null) menu.MainCamera.enabled = true;
        }
    }
    
    public class HandleAudioMenuAction : IUIMenuAction
    {
        public void ExecuteShow(UIMenu menu)
        {
            // Stop main track if any is playing
            if (menu.StopMainTrack)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.ActiveMainTrack?.Stop();
            }
                
            // Play main track audio if any is assigned
            if (menu.MenuShowAudio != null && menu.MenuShowAudio.IsValid())
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMainTrack(menu.MenuShowAudio);
            }
        }

        public void ExecuteHide(UIMenu menu)
        {
            
        }
    }
    
    public class FixURPUIMenuAction : IUIMenuAction
    {
        public void ExecuteShow(UIMenu menu)
        {
            if (menu.FixRenderScale && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset)
            {
                menu.RenderScaleBefore = asset.renderScale;
                if (asset.renderScale < 1) asset.renderScale = 1.0f;
            }
        }

        public void ExecuteHide(UIMenu menu)
        {
            if (menu.FixRenderScale && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset)
            {
                asset.renderScale = menu.RenderScaleBefore;
            }
        }
    }
}