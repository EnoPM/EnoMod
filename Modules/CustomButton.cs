using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EnoMod.Modules
{
    public class CustomButton
    {
        public static readonly List<CustomButton> Buttons = new();
        private readonly ActionButton _actionButton;
        private readonly GameObject _actionButtonGameObject;
        private readonly SpriteRenderer _actionButtonRenderer;
        private readonly Material _actionButtonMat;
        private readonly TextMeshPro _actionButtonLabelText;
        private readonly Vector3 _positionOffset;
        private readonly float _maxTimer = float.MaxValue;
        public float Timer;
        private Action _onClick;
        private readonly Action _initialOnClick;
        private readonly Action _onMeetingEnds;
        private readonly Func<bool> _hasButton;
        private readonly Func<bool> _couldUse;
        private readonly Action _onEffectEnds;
        private readonly bool _hasEffect;
        private bool _isEffectActive;
        private readonly bool _showButtonText;
        public float _effectDuration;
        private readonly Sprite _sprite;
        private readonly HudManager _hudManager;
        private readonly bool _mirror;
        private readonly KeyCode? _hotkey;
        private readonly string _buttonText;
        private static readonly int _desat = Shader.PropertyToID("_Desat");

        private static float _deltaTime = Time.deltaTime / 1.6f;

        public static class ButtonPositions
        {
            // Not usable for impostors
            public static readonly Vector3 LowerRowRight = new Vector3(-2f, -0.06f, 0);
            public static readonly Vector3 LowerRowCenter = new Vector3(-3f, -0.06f, 0);
            public static readonly Vector3 LowerRowLeft = new Vector3(-4f, -0.06f, 0);

            // Not usable for impostors
            public static readonly Vector3 UpperRowRight = new Vector3(0f, 1f, 0f);

            // Not usable for impostors
            public static readonly Vector3 UpperRowCenter = new Vector3(-1f, 1f, 0f);
            public static readonly Vector3 UpperRowLeft = new Vector3(-2f, 1f, 0f);
            public static readonly Vector3 UpperRowFarLeft = new Vector3(-3f, 1f, 0f);
        }

        public void EffectDuration(float duration)
        {
            _effectDuration = duration;
        }

        public float EffectDuration()
        {
            return _effectDuration;
        }

        public CustomButton(
            Action onClick,
            Func<bool> hasButton,
            Func<bool> couldUse,
            Action onMeetingEnds,
            Sprite sprite,
            Vector3 positionOffset,
            HudManager hudManager,
            KeyCode? hotkey,
            bool hasEffect,
            float effectDuration,
            Action onEffectEnds,
            bool mirror = false,
            string buttonText = "")
        {
            this._hudManager = hudManager;
            this._onClick = onClick;
            this._initialOnClick = onClick;
            this._hasButton = hasButton;
            this._couldUse = couldUse;
            this._positionOffset = positionOffset;
            this._onMeetingEnds = onMeetingEnds;
            this._hasEffect = hasEffect;
            this._effectDuration = effectDuration;
            this._onEffectEnds = onEffectEnds;
            this._sprite = sprite;
            this._mirror = mirror;
            this._hotkey = hotkey;
            this._buttonText = buttonText;
            Timer = 16.2f;
            Buttons.Add(this);
            _actionButton =
                UnityEngine.Object.Instantiate(hudManager.KillButton, hudManager.KillButton.transform.parent);
            _actionButtonGameObject = _actionButton.gameObject;
            _actionButtonRenderer = _actionButton.graphic;
            _actionButtonMat = _actionButtonRenderer.material;
            _actionButtonLabelText = _actionButton.buttonLabelText;
            var button = _actionButton.GetComponent<PassiveButton>();
            this._showButtonText = _actionButtonRenderer.sprite == sprite || buttonText != string.Empty;
            button.OnClick = new Button.ButtonClickedEvent();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction) OnClickEvent);

            SetActive(false);
        }

        public CustomButton(
            Action onClick,
            Func<bool> hasButton,
            Func<bool> couldUse,
            Action onMeetingEnds,
            Sprite sprite,
            Vector3 positionOffset,
            HudManager hudManager,
            KeyCode? hotkey,
            bool mirror = false,
            string buttonText = "")
            : this(
                onClick,
                hasButton,
                couldUse,
                onMeetingEnds,
                sprite,
                positionOffset,
                hudManager,
                hotkey,
                false,
                0f,
                () => { },
                mirror,
                buttonText)
        {
        }

        public void OnClickEvent()
        {
            if (!(this.Timer < 0f) || !_hasButton() || !_couldUse()) return;

            _actionButtonRenderer.color = new Color(1f, 1f, 1f, 0.3f);
            this._onClick();

            if (!this._hasEffect || this._isEffectActive) return;

            this.Timer = this.EffectDuration();
            _actionButton.cooldownTimerText.color = new Color(0F, 0.8F, 0F);
            this._isEffectActive = true;
        }

        public static void HudUpdate()
        {
            Buttons.RemoveAll(item => item._actionButton == null);

            foreach (var button in Buttons)
            {
                try
                {
                    button.Update();
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine(
                        "[WARNING] NullReferenceException from HudUpdate().HasButton(), if theres only one warning its fine");
                }
            }
        }

        public static void MeetingEndedUpdate()
        {
            Buttons.RemoveAll(item => item._actionButton == null);
            foreach (var button in Buttons)
            {
                try
                {
                    button._onMeetingEnds();
                    button.Update();
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine(
                        "[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
                }
            }
        }

        public static void ResetAllCooldown()
        {
            foreach (var button in Buttons)
            {
                try
                {
                    button.Timer = button._maxTimer;
                    button.Update();
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine(
                        "[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
                }
            }
        }

        public void SetActive(bool isActive)
        {
            if (isActive)
            {
                _actionButtonGameObject.SetActive(true);
                _actionButtonRenderer.enabled = true;
            }
            else
            {
                _actionButtonGameObject.SetActive(false);
                _actionButtonRenderer.enabled = false;
            }
        }

        public void Update()
        {
            var localPlayer = PlayerCache.LocalPlayer;
            if (localPlayer == null) return;
            var movable = localPlayer is { PlayerControl.moveable: true };

            if (MeetingHud.Instance || ExileController.Instance || !_hasButton())
            {
                SetActive(false);
                return;
            }

            SetActive(_hudManager.UseButton.isActiveAndEnabled || _hudManager.PetButton.isActiveAndEnabled);

            _actionButtonRenderer.sprite = _sprite;
            if (_showButtonText && _buttonText != string.Empty)
            {
                _actionButton.OverrideText(_buttonText);
            }

            _actionButtonLabelText.enabled = _showButtonText; // Only show the text if it's a kill button
            if (_hudManager.UseButton != null)
            {
                var pos = _hudManager.UseButton.transform.localPosition;
                if (_mirror)
                {
                    var main = Camera.main;
                    if (main != null)
                    {
                        var aspect = main.aspect;
                        var safeOrthographicSize = CameraSafeArea.GetSafeOrthographicSize(main);
                        var xPos = 0.05f - safeOrthographicSize * aspect * 1.70f;
                        pos = new Vector3(xPos, pos.y, pos.z);
                    }
                }

                _actionButton.transform.localPosition = pos + _positionOffset;
            }

            if (_couldUse())
            {
                _actionButtonRenderer.color = _actionButtonLabelText.color = Palette.EnabledColor;
                _actionButtonMat.SetFloat(_desat, 0f);
            }
            else
            {
                _actionButtonRenderer.color = _actionButtonLabelText.color = Palette.DisabledClear;
                _actionButtonMat.SetFloat(_desat, 1f);
            }

            if (Timer >= 0)
            {
                if (_hasEffect && _isEffectActive)
                    Timer -= _deltaTime;
                else if (!localPlayer.PlayerControl.inVent && movable)
                    Timer -= _deltaTime;
            }

            if (Timer <= 0 && _hasEffect && _isEffectActive)
            {
                _isEffectActive = false;
                _actionButton.cooldownTimerText.color = Palette.EnabledColor;
                _onEffectEnds();
            }

            _actionButton.SetCoolDown(Timer, (_hasEffect && _isEffectActive) ? EffectDuration() : _maxTimer);

            // Trigger OnClickEvent if the hotkey is being pressed down
            if (_hotkey.HasValue && Input.GetKeyDown(_hotkey.Value))
            {
                OnClickEvent();
            }
            else
            {
                _onClick = _initialOnClick;
            }
        }
    }
}
