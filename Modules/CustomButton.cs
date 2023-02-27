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
        public ActionButton ActionButton;
        public GameObject ActionButtonGameObject;
        public SpriteRenderer ActionButtonRenderer;
        public Material ActionButtonMat;
        public TextMeshPro ActionButtonLabelText;
        public Vector3 PositionOffset;
        public float MaxTimer = float.MaxValue;
        public float Timer = 0f;
        private Action OnClick;
        private Action InitialOnClick;
        private Action OnMeetingEnds;
        public Func<bool> HasButton;
        public Func<bool> CouldUse;
        private Action OnEffectEnds;
        public bool HasEffect;
        public bool isEffectActive = false;
        public bool showButtonText = false;
        public float EffectDuration;
        public Sprite Sprite;
        public HudManager hudManager;
        public bool mirror;
        public KeyCode? hotkey;
        private string buttonText;
        private static readonly int Desat = Shader.PropertyToID("_Desat");

        private static float _deltaTime = Time.deltaTime / 2;

        public static class ButtonPositions
        {
            public static readonly Vector3
                lowerRowRight = new Vector3(-2f, -0.06f, 0); // Not usable for imps beacuse of new button positions!

            public static readonly Vector3 lowerRowCenter = new Vector3(-3f, -0.06f, 0);
            public static readonly Vector3 lowerRowLeft = new Vector3(-4f, -0.06f, 0);

            public static readonly Vector3
                upperRowRight = new Vector3(0f, 1f, 0f); // Not usable for imps beacuse of new button positions!

            public static readonly Vector3
                upperRowCenter = new Vector3(-1f, 1f, 0f); // Not usable for imps beacuse of new button positions!

            public static readonly Vector3 upperRowLeft = new Vector3(-2f, 1f, 0f);
            public static readonly Vector3 upperRowFarLeft = new Vector3(-3f, 1f, 0f);
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
            this.hudManager = hudManager;
            this.OnClick = onClick;
            this.InitialOnClick = onClick;
            this.HasButton = hasButton;
            this.CouldUse = couldUse;
            this.PositionOffset = positionOffset;
            this.OnMeetingEnds = onMeetingEnds;
            this.HasEffect = hasEffect;
            this.EffectDuration = effectDuration;
            this.OnEffectEnds = onEffectEnds;
            this.Sprite = sprite;
            this.mirror = mirror;
            this.hotkey = hotkey;
            this.buttonText = buttonText;
            Timer = 16.2f;
            Buttons.Add(this);
            ActionButton =
                UnityEngine.Object.Instantiate(hudManager.KillButton, hudManager.KillButton.transform.parent);
            ActionButtonGameObject = ActionButton.gameObject;
            ActionButtonRenderer = ActionButton.graphic;
            ActionButtonMat = ActionButtonRenderer.material;
            ActionButtonLabelText = ActionButton.buttonLabelText;
            var button = ActionButton.GetComponent<PassiveButton>();
            this.showButtonText = ActionButtonRenderer.sprite == sprite || buttonText != string.Empty;
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
            if (!(this.Timer < 0f) || !HasButton() || !CouldUse()) return;

            ActionButtonRenderer.color = new Color(1f, 1f, 1f, 0.3f);
            this.OnClick();

            if (!this.HasEffect || this.isEffectActive) return;

            this.Timer = this.EffectDuration;
            ActionButton.cooldownTimerText.color = new Color(0F, 0.8F, 0F);
            this.isEffectActive = true;
        }

        public static void HudUpdate()
        {
            Buttons.RemoveAll(item => item.ActionButton == null);

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
            Buttons.RemoveAll(item => item.ActionButton == null);
            foreach (var button in Buttons)
            {
                try
                {
                    button.OnMeetingEnds();
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
                    button.Timer = button.MaxTimer;
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
                ActionButtonGameObject.SetActive(true);
                ActionButtonRenderer.enabled = true;
            }
            else
            {
                ActionButtonGameObject.SetActive(false);
                ActionButtonRenderer.enabled = false;
            }
        }

        public void Update()
        {
            var localPlayer = PlayerCache.LocalPlayer;
            if (localPlayer == null) return;
            var movable = localPlayer is { PlayerControl.moveable: true };

            if (MeetingHud.Instance || ExileController.Instance || !HasButton())
            {
                SetActive(false);
                return;
            }

            SetActive(hudManager.UseButton.isActiveAndEnabled || hudManager.PetButton.isActiveAndEnabled);

            ActionButtonRenderer.sprite = Sprite;
            if (showButtonText && buttonText != string.Empty)
            {
                ActionButton.OverrideText(buttonText);
            }

            ActionButtonLabelText.enabled = showButtonText; // Only show the text if it's a kill button
            if (hudManager.UseButton != null)
            {
                var pos = hudManager.UseButton.transform.localPosition;
                if (mirror)
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

                ActionButton.transform.localPosition = pos + PositionOffset;
            }

            if (CouldUse())
            {
                ActionButtonRenderer.color = ActionButtonLabelText.color = Palette.EnabledColor;
                ActionButtonMat.SetFloat(Desat, 0f);
            }
            else
            {
                ActionButtonRenderer.color = ActionButtonLabelText.color = Palette.DisabledClear;
                ActionButtonMat.SetFloat(Desat, 1f);
            }

            if (Timer >= 0)
            {
                if (HasEffect && isEffectActive)
                    Timer -= _deltaTime;
                else if (!localPlayer.PlayerControl.inVent && movable)
                    Timer -= _deltaTime;
            }

            if (Timer <= 0 && HasEffect && isEffectActive)
            {
                isEffectActive = false;
                ActionButton.cooldownTimerText.color = Palette.EnabledColor;
                OnEffectEnds();
            }

            ActionButton.SetCoolDown(Timer, (HasEffect && isEffectActive) ? EffectDuration : MaxTimer);

            // Trigger OnClickEvent if the hotkey is being pressed down
            if (hotkey.HasValue && Input.GetKeyDown(hotkey.Value))
            {
                OnClickEvent();
            }
            else
            {
                OnClick = InitialOnClick;
            }
        }
    }
}
