using System.Text.RegularExpressions;
using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrickCore
{
    public class TrickInputValidation : MonoBehaviour
    {
        public string Regex;

        public bool ValidateOnChange;

        public bool ToggleRequiredValue = true;

        public RectTransform StatusRoot;
        public RectTransform StatusInvalidRoot;
        public RectTransform StatusValidRoot;

        public TrickInputValidation LinkedTo;

        public Curve TweenCurve = Curve.BounceOut;
        public float TweenDuration = 0.25f;
        public float ShakeAmount = 1.0f;
        public float PauseDuration = 0.025f;

        private Regex _regex;
        private TMP_InputField _input;
        private Toggle _toggle;

        public bool ValidationMode { get; set; }

        private void OnEnable()
        {
            _toggle = GetComponent<Toggle>();
            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(ToggleValueChanged);
            }

            _input = GetComponent<TMP_InputField>();
            if (_input != null) _input.onValueChanged.AddListener(InputValueChanged);


            if (LinkedTo != null)
            {
                LinkedTo._input = LinkedTo.GetComponent<TMP_InputField>();
                if (LinkedTo._input != null) LinkedTo._input.onValueChanged.AddListener(InputValueChanged);

                LinkedTo._toggle = LinkedTo.GetComponent<Toggle>();
                if (LinkedTo._toggle != null) LinkedTo._toggle.onValueChanged.AddListener(ToggleValueChanged);
            }

            if (!string.IsNullOrEmpty(Regex)) Setup(Regex);
        }

        private void OnDisable()
        {
            if (_toggle != null) _toggle.onValueChanged.RemoveListener(ToggleValueChanged);
            if (_input != null) _input.onValueChanged.RemoveListener(InputValueChanged);
            if (LinkedTo != null)
            {
                if (LinkedTo._input != null) LinkedTo._input.onValueChanged.RemoveListener(InputValueChanged);
                if (LinkedTo._toggle != null) LinkedTo._toggle.onValueChanged.RemoveListener(ToggleValueChanged);
            }
        }

        private void ToggleValueChanged(bool arg0)
        {
            if (ValidateOnChange || ValidationMode) ValidateNow();
        }

        private void InputValueChanged(string arg0)
        {
            if (ValidateOnChange || ValidationMode)
            {
                if (_regex == null) return;

                ValidateNow();
            }
        }

        private void ValidateNow()
        {
            var matches = IsValid();
            if (StatusRoot != null) StatusRoot.gameObject.SetActive(true);
            if (StatusValidRoot != null) StatusValidRoot.gameObject.SetActive(matches);
            if (StatusInvalidRoot != null) StatusInvalidRoot.gameObject.SetActive(!matches);

            ValidationMode = true;
        }


        public void Setup(Regex regex)
        {
            _regex = regex;
        }

        public void Setup(string regex) => Setup(new Regex(regex));

        public bool IsValid()
        {
            if (LinkedTo != null)
            {
                if (_input != null)
                    return _input.text == LinkedTo._input.text && LinkedTo.IsValid();
                if (_toggle != null)
                    return _toggle.isOn == LinkedTo._toggle.isOn && LinkedTo.IsValid();
                return false;
            }

            if (_input != null)
                return _regex.IsMatch(_input.text);

            if (_toggle != null)
                return ToggleRequiredValue == _toggle.isOn;

            return false;
        }

        public bool Test(bool shake)
        {
            bool valid = IsValid();
            if (!valid)
            {
                // apply shake on this instance
                if (shake)
                {
                    this.Shake(true, shake: ShakeAmount, pauseDuration: PauseDuration, tweenDuration: TweenDuration,
                        tweenCurve: TweenCurve, loops: 1);
                }

                ValidateNow();
            }
            else
            {
                ValidateNow();
            }

            return valid;
        }

        public void Hide()
        {
            if (StatusRoot != null) StatusRoot.gameObject.SetActive(false);
            ValidationMode = false;
        }
    }
}