﻿using System;
using AmongUs.Data;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using Reactor.Utilities.Attributes;
using Reactor.Utilities.ImGui;
using UnityEngine;

namespace EnoMod.Kernel.EnoMenu;

[RegisterInIl2Cpp]
public class MainComponent : MonoBehaviour
{

    [HideFromIl2Cpp]
    public DragWindow TestWindow { get; }

    public MainComponent(IntPtr ptr) : base(ptr)
    {
        TestWindow = new DragWindow(new Rect(20, 20, 0, 0), "EnoMenu", () =>
        {
            if (PlayerCache.LocalPlayer != null)
            {
                GUILayout.Label($"Player id: {PlayerCache.LocalPlayer.PlayerId}");
                GUILayout.Label($"Friend code: {PlayerCache.LocalPlayer.PlayerControl.FriendCode}");
            }
            GUILayout.Label("Player: " + DataManager.Player.Customization.Name);

            if (ShipStatus.Instance && AmongUsClient.Instance.AmHost)
            {
                if (GUILayout.Button("Force game end"))
                {
                    ShipStatus.Instance.enabled = false;
                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                }

                if (GUILayout.Button("Call a meeting"))
                {
                    PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                }
            }

            if (TutorialManager.InstanceExists && PlayerControl.LocalPlayer)
            {
                var data = PlayerControl.LocalPlayer.Data;

                var newIsImpostor = GUILayout.Toggle(data.Role.IsImpostor, "Is Impostor");
                if (data.Role.IsImpostor != newIsImpostor)
                {
                    PlayerControl.LocalPlayer.RpcSetRole(newIsImpostor ? RoleTypes.Impostor : RoleTypes.Crewmate);
                }

                if (GUILayout.Button("Spawn a dummy"))
                {
                    var playerControl = Instantiate(TutorialManager.Instance.PlayerPrefab);
                    var i = playerControl.PlayerId = (byte) GameData.Instance.GetAvailableId();
                    GameData.Instance.AddPlayer(playerControl);
                    AmongUsClient.Instance.Spawn(playerControl, -2, SpawnFlags.None);
                    playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
                    playerControl.GetComponent<DummyBehaviour>().enabled = true;
                    playerControl.NetTransform.enabled = false;
                    playerControl.SetName(
                        $"{TranslationController.Instance.GetString(StringNames.Dummy, Array.Empty<Il2CppSystem.Object>())} {i}");
                    var color = (byte) (i % Palette.PlayerColors.Length);
                    playerControl.SetColor(color);
                    playerControl.SetHat(HatManager.Instance.allHats[i % HatManager.Instance.allHats.Count].ProdId,
                        playerControl.Data.DefaultOutfit.ColorId);
                    playerControl.SetPet(HatManager.Instance.allPets[i % HatManager.Instance.allPets.Count].ProdId);
                    playerControl.SetSkin(HatManager.Instance.allSkins[i % HatManager.Instance.allSkins.Count].ProdId,
                        color);
                    GameData.Instance.RpcSetTasks(playerControl.PlayerId, new Il2CppStructArray<byte>(0));
                }
            }

            if (PlayerControl.LocalPlayer)
            {
                var position = PlayerControl.LocalPlayer.transform.position;
                GUILayout.Label($"x: {position.x}");
                GUILayout.Label($"y: {position.y}");
            }
        })
        {
            Enabled = false,
        };
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestWindow.Enabled = !TestWindow.Enabled;
        }
    }

    private void OnGUI()
    {
        TestWindow.OnGUI();
    }
}
