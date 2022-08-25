using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A modal popup UI menu
/// </summary>
public partial class ModalPopupMenu : UIMenu
{
    /// <summary>
    /// Automatically hide whenever we click a response, otherwise you need to handle the hiding yourself
    /// </summary>
    public bool HideOnResponseClicked = true;

    [Header("Texts")]
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI DescriptionText;
    
    [Header("Buttons")]
    public Button ConfirmButton;
    public TextMeshProUGUI ConfirmButtonText;
    public Button Confirm2Button;
    public TextMeshProUGUI Confirm2ButtonText;
    public Button CancelButton;
    public TextMeshProUGUI CancelButtonText;
    
    private Action _confirmAction;
    private Action _confirm2Action;
    private Action _cancelAction;

    private int MaxTextLength = 2500;

    private static readonly Queue<PopupQueueData> PopupDataQueue = new Queue<PopupQueueData>();
    private static Routine _updater;
    
    protected override void AddressablesAwake()
    {
        ConfirmButton.onClick.AddListener(() =>
        {
            if (HideOnResponseClicked) Hide();
            _confirmAction?.Invoke();
            ExecutePopupQueue();
        });
        Confirm2Button.onClick.AddListener(() =>
        {
            if (HideOnResponseClicked) Hide();
            _confirm2Action?.Invoke();
            ExecutePopupQueue();
        });
        CancelButton.onClick.AddListener(() =>
        {
            if (HideOnResponseClicked) Hide();
            _cancelAction?.Invoke();
            ExecutePopupQueue();
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
        TransitionPanelTransform.gameObject.SetActive(false);
        _updater.Stop();
    }

    private class PopupQueueData
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string OkText { get; set; }
        public Action OkAction { get; set; }
        public string YesText { get; set; }
        public string NoText { get; set; }
        public Action YesAction { get; set; }
        public Action NoAction { get; set; }
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

    private static void ExecutePopupQueue()
    {
        if (PopupDataQueue.Count <= 0) return;
        var data = PopupDataQueue.Dequeue();
        switch (data.Popup)
        {
            case PopupQueueData.PopupType.Ok:
                ShowOkModal(data.Title, data.Description, data.OkText, data.OkAction, data.DescriptionUpdater);
                break;
            case PopupQueueData.PopupType.YesNo:
                ShowYesNoModal(data.Title, data.Description, data.YesText, data.NoText, data.YesAction, data.NoAction, data.DescriptionUpdater);
                break;
            case PopupQueueData.PopupType.ButtonModal:
                ShowButtonsModal(data.Title, data.Description, data.Button1, data.Button2, data.Button3, data.DescriptionUpdater);
                break;
        }
    }

    public static void ShowOkModal(string title, string description, string okText, Action okAction, Func<string, IEnumerator> descriptionUpdater = null)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();

        if (instance.IsOpen)
        {
            PopupDataQueue.Enqueue(new PopupQueueData()
            {
                Title = title,
                Description = description,
                OkText = okText,
                OkAction = okAction,
                Popup = PopupQueueData.PopupType.Ok,
                DescriptionUpdater = descriptionUpdater,
            });
            return;
        }
        
        instance.SetConfirmButtonText(okText);
        instance.SetCancelButtonText(default);
        instance.SetConfirm2ButtonText(default);
        instance._confirmAction = okAction;
        instance._confirm2Action = null;
        instance._cancelAction = null;
        instance.SetTitleText(title);
        instance.SetDescriptionText(description);

        instance.Show();
        
        instance.CancelButton.interactable = true;
        instance.ConfirmButton.interactable = true;
        instance.Confirm2Button.interactable = true;

        _updater.Stop();
        if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(description));
    }

    public static void ShowYesNoModal(string title, string description, string yesText, string noText, Action yesAction, Action noAction, Func<string, IEnumerator> descriptionUpdater = null)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();
        
        if (instance.IsOpen)
        {
            PopupDataQueue.Enqueue(new PopupQueueData()
            {
                Title = title,
                Description = description,
                YesText = yesText,
                NoText = noText,
                YesAction = yesAction,
                NoAction = noAction,
                Popup = PopupQueueData.PopupType.YesNo,
                DescriptionUpdater = descriptionUpdater,
            });
            return;
        }

        instance.SetConfirmButtonText(yesText);
        instance.SetCancelButtonText(noText);
        instance.SetConfirm2ButtonText(default);
        instance._confirmAction = yesAction;
        instance._confirm2Action = null;
        instance._cancelAction = noAction;
        instance.SetTitleText(title);
        instance.SetDescriptionText(description);

        instance.Show();

        instance.CancelButton.interactable = true;
        instance.ConfirmButton.interactable = true;
        instance.Confirm2Button.interactable = true;
        
        _updater.Stop();
        if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(description));
    }

    public struct ModalPopupData
    {
        public string Text;
        public Action Action;
    }
    
    public static void ShowButtonsModal(string title, string description, ModalPopupData button1, ModalPopupData button2, ModalPopupData button3, Func<string, IEnumerator> descriptionUpdater = null)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();
        
        if (instance.IsOpen)
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

        instance.SetCancelButtonText(button1.Text);
        instance._cancelAction = button1.Action;
        
        instance.SetConfirmButtonText(button2.Text);
        instance._confirmAction = button2.Action;
        
        instance.SetConfirm2ButtonText(button3.Text);
        instance._confirm2Action = button3.Action;
        instance.SetTitleText(title);
        instance.SetDescriptionText(description);

        instance.Show();
        
        instance.CancelButton.interactable = true;
        instance.ConfirmButton.interactable = true;
        instance.Confirm2Button.interactable = true;

        _updater.Stop();
        if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(description));
    }

    /// <summary>
    /// Sets the text on the first line.
    /// </summary>
    /// <param name="text"></param>
    public void SetTitleText(string text)
    {
        if (TitleText != null)
        {
            TitleText.text = text;
            TitleText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }

    /// <summary>
    /// Sets the text on the second line.
    /// </summary>
    /// <param name="text"></param>
    public void SetDescriptionText(string text)
    {
        if (DescriptionText != null)
        {
            var str = text;
            if (str != null && str.Length > MaxTextLength)
                str = str.Substring(0, MaxTextLength);
            
            DescriptionText.text = str;
            DescriptionText.gameObject.SetActive(!string.IsNullOrEmpty(str));
        }
    }

    /// <summary>
    /// Sets the confirm button text.
    /// </summary>
    /// <param name="text">The confirm button text.</param>
    public void SetConfirmButtonText(string text)
    {
        if (text != null)
        {
            ConfirmButtonText.SetText(text);
            ConfirmButton.gameObject.SetActive(true);
        }
        else
        {
            ConfirmButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sets the confirm button text.
    /// </summary>
    /// <param name="text">The confirm button text.</param>
    public void SetConfirm2ButtonText(string text)
    {
        if (text != null)
        {
            Confirm2ButtonText.SetText(text);
            Confirm2Button.gameObject.SetActive(true);
        }
        else
        {
            Confirm2Button.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sets the cancel button text.
    /// </summary>
    /// <param name="text">The cancel button text.</param>
    public void SetCancelButtonText(string text)
    {
        if (text != null)
        {
            CancelButtonText.SetText(text);
            CancelButton.gameObject.SetActive(true);
        }
        else
        {
            CancelButton.gameObject.SetActive(false);
        }
    }

    public static void SetDescription(string s)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();
        instance.DescriptionText.text = s;
    }

    public static void EnableButton(int index, bool enable)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();
        switch (index)
        {
            case 0:
                instance.CancelButton.interactable = enable;
                break;
            case 1:
                instance.ConfirmButton.interactable = enable;
                break;
            case 2:
                instance.Confirm2Button.interactable = enable;
                break;
        }
    }

    public static void ShowError(string result, Action okAction)
    {
        ShowOkModal("Error", result, "Ok", okAction);
    }

    public static void ShowError(Exception exception, Action okAction)
    {
        ShowError(exception.Message, okAction);
    }

    public static void ShowError(string result)
    {
        ShowOkModal("Error", result, "Ok", null);
    }

    public static void ShowError(Exception exception)
    {
        ShowError(exception.Message, null);
    }
}