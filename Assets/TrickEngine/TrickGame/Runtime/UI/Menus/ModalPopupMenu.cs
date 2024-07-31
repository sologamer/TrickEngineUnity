using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TrickCore
{
    /// <summary>
    /// A modal popup UI menu
    /// </summary>
    public partial class ModalPopupMenu : ModalPopupMenuT<ModalPopupMenu>
    {
    }
    
    public abstract class ModalPopupMenuT<T> : UIMenu where T : ModalPopupMenuT<T>
    {
        public static T Instance => UIManager.Instance.GetMenu<T>();
        
        /// <summary>
        /// Automatically hide whenever we click a response, otherwise you need to handle the hiding yourself
        /// </summary>
        public bool HideOnResponseClicked = true;

        [Header("Texts")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI DescriptionText;

        [Header("Buttons")] private int _stub;
        
        [FormerlySerializedAs("CancelButton")] public Button Modal1Button;
        [FormerlySerializedAs("CancelButtonText")] public TextMeshProUGUI Modal1ButtonText;

        [FormerlySerializedAs("ConfirmButton")] public Button Modal2Button;
        [FormerlySerializedAs("ConfirmButtonText")] public TextMeshProUGUI Modal2ButtonText;
        
        [FormerlySerializedAs("Confirm2Button")] public Button Modal3Button;
        [FormerlySerializedAs("Confirm2ButtonText")] public TextMeshProUGUI Modal3ButtonText;
        
        // #d4442a
        public Color DefaultColor1 = new Color(212, 68, 42, 255) / 255f;
        // #34911a
        public Color DefaultColor2 = new Color(52, 145, 26, 255) / 255f;
        // #9c6b10
        public Color DefaultColor3 = new Color(156, 107, 16, 255) / 255f;
    

        public int MaxDescriptionLength = 2500;

        protected readonly Queue<PopupQueueData> PopupDataQueue = new();

        private ModalPopupData _modalData1;
        private ModalPopupData _modalData2;
        private ModalPopupData _modalData3;
        private Routine _updater;
    
        protected override void AddressablesAwake()
        {
            Modal1Button.onClick.AddListener(() =>
            {
                SetHideCallbackOnce(() => ExecutePopupQueue());
                if (HideOnResponseClicked) Hide();
                _modalData1.Action?.Invoke();
            });
            Modal2Button.onClick.AddListener(() =>
            {
                SetHideCallbackOnce(() => ExecutePopupQueue());
                if (HideOnResponseClicked) Hide();
                _modalData2.Action?.Invoke();
            });
            Modal3Button.onClick.AddListener(() =>
            {
                SetHideCallbackOnce(() => ExecutePopupQueue());
                if (HideOnResponseClicked) Hide();
                _modalData3.Action?.Invoke();
            });
        }

        public override UIMenu Show()
        {
            base.Show();
        
            return this;
        }

        public override void Hide()
        {
            base.Hide();
            _updater.Stop();
        }
        
        private Routine _waitRoutine;
        
        private void ExecutePopupQueue()
        {
            if (PopupDataQueue.Count <= 0) return;
        
            if (IsOpen)
            {
                _waitRoutine.Replace(WaitToExecuteQueue());
                
                // wait until popup closes, before we show again
                IEnumerator WaitToExecuteQueue()
                {
                    yield return Routine.WaitCondition(() => !IsOpen && !IsTransitioning);
                    ExecuteQueue();
                }
            }
            else
            {
                ExecuteQueue();
            }

            return;

            void ExecuteQueue()
            {
                var data = PopupDataQueue.Dequeue();
                switch (data.Popup)
                {
                    case PopupQueueData.PopupType.Ok:
                        ShowOkModal(data.Title, data.Description, data.Button1, data.DescriptionUpdater);
                        break;
                    case PopupQueueData.PopupType.YesNo:
                        ShowYesNoModal(data.Title, data.Description, data.Button2, data.Button1, data.DescriptionUpdater);
                        break;
                    case PopupQueueData.PopupType.ButtonModal:
                        ShowButtonsModal(data.Title, data.Description, data.Button1, data.Button2, data.Button3, data.DescriptionUpdater);
                        break;
                }
            }
        }

        public void ShowOkModal(string title, string description, ModalPopupData okData, Func<string, IEnumerator> descriptionUpdater = null, string buttonColor = null)
        {
            if (IsOpen)
            {
                PopupDataQueue.Enqueue(new PopupQueueData()
                {
                    Title = title,
                    Description = description,
                    Button1 = okData,
                    Popup = PopupQueueData.PopupType.Ok,
                    DescriptionUpdater = descriptionUpdater,
                });
                return;
            }
        
            SetModal1Data(okData);
            SetModal2Data(default);
            SetModal3Data(default);
            SetTitleText(title);
            SetDescriptionText(description);

            Show();
        
            Modal1Button.interactable = true;
            Modal2Button.interactable = true;
            Modal3Button.interactable = true;

            _updater.Stop();
            if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(description));
        }

        public void ShowYesNoModal(string title, string description, ModalPopupData yesData, ModalPopupData noData, Func<string, IEnumerator> descriptionUpdater = null)
        {
            if (IsOpen)
            {
                PopupDataQueue.Enqueue(new PopupQueueData()
                {
                    Title = title,
                    Description = description,
                    Button1 = noData,
                    Button2 = yesData,
                    Popup = PopupQueueData.PopupType.YesNo,
                    DescriptionUpdater = descriptionUpdater,
                });
                return;
            }

            SetModal1Data(noData);
            SetModal2Data(yesData);
            SetModal3Data(default);
            SetTitleText(title);
            SetDescriptionText(description);

            Show();

            Modal1Button.interactable = true;
            Modal2Button.interactable = true;
            Modal3Button.interactable = true;
        
            _updater.Stop();
            if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(description));
        }

        public void ShowButtonsModal(string title, string description, ModalPopupData button1, ModalPopupData button2, ModalPopupData button3, Func<string, IEnumerator> descriptionUpdater = null)
        {
            if (IsOpen)
            {
                PopupDataQueue.Enqueue(new PopupQueueData()
                {
                    Title = title,
                    Description = description,
                    Button1 = button1,
                    Button2 = button2,
                    Button3 = button3,
                    Popup = PopupQueueData.PopupType.ButtonModal,
                    DescriptionUpdater = descriptionUpdater,
                });
                return;
            }

            SetModal1Data(button1);
            SetModal2Data(button2);
            SetModal3Data(button3);
            SetTitleText(title);
            SetDescriptionText(description);

            Show();
        
            Modal1Button.interactable = true;
            Modal2Button.interactable = true;
            Modal3Button.interactable = true;

            _updater.Stop();
            if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(description));
        }

        /// <summary>
        /// Sets the text on the first line.
        /// </summary>
        /// <param name="text"></param>
        public void SetTitleText(string text)
        {
            if (TitleText == null) return;
            TitleText.text = text;
            TitleText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        /// <summary>
        /// Sets the text on the second line.
        /// </summary>
        /// <param name="text"></param>
        public void SetDescriptionText(string text)
        {
            if (DescriptionText == null) return;
            var str = text;
            if (str != null && str.Length > MaxDescriptionLength)
                str = str.Substring(0, MaxDescriptionLength);
            
            DescriptionText.text = str;
            DescriptionText.gameObject.SetActive(!string.IsNullOrEmpty(str));
        }

        public void SetModal1Data(ModalPopupData data)
        {
            _modalData1 = data;

            if (data.Text != null)
            {
                Modal1ButtonText.SetText(data.Text);
                Modal1Button.gameObject.SetActive(true);
                Modal1Button.image.color = data.ButtonColor != null ? ColorUtility.TryParseHtmlString(data.ButtonColor, out var color) ? color : DefaultColor1 : DefaultColor1;
            }
            else
            {
                Modal1Button.gameObject.SetActive(false);
            }
        }

        public void SetModal2Data(ModalPopupData data)
        {
            _modalData2 = data;

            if (data.Text != null)
            {
                Modal2ButtonText.SetText(data.Text);
                Modal2Button.gameObject.SetActive(true);
                
                Modal2Button.image.color = data.ButtonColor != null ? ColorUtility.TryParseHtmlString(data.ButtonColor, out var color) ? color : DefaultColor2 : DefaultColor2;
            }
            else
            {
                Modal2Button.gameObject.SetActive(false);
            }
        }

        public void SetModal3Data(ModalPopupData data)
        {
            _modalData3 = data;

            if (data.Text != null)
            {
                Modal3ButtonText.SetText(data.Text);
                Modal3Button.gameObject.SetActive(true);
                Modal3Button.image.color = data.ButtonColor != null ? ColorUtility.TryParseHtmlString(data.ButtonColor, out var color) ? color : DefaultColor3 : DefaultColor3;
            }
            else
            {
                Modal3Button.gameObject.SetActive(false);
            }
        }

        public void SetDescription(string s)
        {
            if (DescriptionText != null) DescriptionText.text = s;
        }

        public void EnableButton(int index, bool enable)
        {
            switch (index)
            {
                case 0:
                    Modal1Button.interactable = enable;
                    break;
                case 1:
                    Modal2Button.interactable = enable;
                    break;
                case 2:
                    Modal3Button.interactable = enable;
                    break;
            }
        }

        public void ShowError(string result, Action okAction)
        {
            ShowOkModal("Error", result, new ModalPopupData("Ok", okAction));
        }

        public void ShowError(Exception exception, Action okAction)
        {
            ShowError(exception.Message, okAction);
        }

        public void ShowError(string result)
        {
            ShowError(result, null);
        }

        public void ShowError(Exception exception)
        {
            ShowError(exception.Message, null);
        }
    }
}

public struct ModalPopupData
{
    public readonly string Text;
    public readonly Action Action;
    public readonly string ButtonColor;
            
    public ModalPopupData(string text, Action action = null, string buttonColor = null)
    {
        Text = text;
        Action = action;
        ButtonColor = buttonColor;
    }
}

public class PopupQueueData
{
    public string Title { get; set; }
    public string Description { get; set; }
    public PopupType Popup { get; set; }
    public ModalPopupData Button1 { get; set; }
    public ModalPopupData Button2 { get; set; }
    public ModalPopupData Button3 { get; set; }
    public Func<string, IEnumerator> DescriptionUpdater { get; set; }

    public enum PopupType
    {
        Ok,
        YesNo,
        ButtonModal,
    }
}
