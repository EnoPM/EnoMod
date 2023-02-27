using System;
using System.Collections.Generic;
using AmongUs.Data;
using AmongUs.GameOptions;
using EnoMod.Modules;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Reactor.Utilities.Attributes;
using Reactor.Utilities.ImGui;
using UnityEngine;

namespace EnoMod;

partial class EnoModPlugin
{
    private static List<PlayerControl> _bots = new List<PlayerControl>();
    public DebuggerComponent Component { get; private set; } = null!;

    [RegisterInIl2Cpp]
    public class DebuggerComponent : MonoBehaviour
    {
        [HideFromIl2Cpp] public bool DisableGameEnd { get; set; }

        [HideFromIl2Cpp] public DragWindow DebuggerWindow { get; }

        public DebuggerComponent(IntPtr ptr) : base(ptr)
        {
            DebuggerWindow = new DragWindow(new Rect(20, 20, 0, 0), "Debugger", () =>
            {
                GUILayout.Label("Name: " + DataManager.Player.Customization.Name);
                DisableGameEnd = GUILayout.Toggle(DisableGameEnd, "Disable game end");

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
                    var data = PlayerControl.LocalPlayer.Data;

                    var newIsImpostor = GUILayout.Toggle(data.Role.IsImpostor, "Is Impostor");
                    if (data.Role.IsImpostor != newIsImpostor)
                    {
                        PlayerControl.LocalPlayer.RpcSetRole(newIsImpostor ? RoleTypes.Impostor : RoleTypes.Crewmate);
                    }
                }

                if (AmongUsClient.Instance.AmHost)
                {
                    if (GUILayout.Button("Spawn a dummy"))
                    {
                        var playerControl = Instantiate(AmongUsClient.Instance.PlayerPrefab);
                        var i = playerControl.PlayerId = (byte) GameData.Instance.GetAvailableId();

                        _bots.Add(playerControl);
                        GameData.Instance.AddPlayer(playerControl);
                        AmongUsClient.Instance.Spawn(playerControl, i, InnerNet.SpawnFlags.None);

                        if (PlayerCache.LocalPlayer == null) return;
                        playerControl.transform.position = PlayerCache.LocalPlayer.transform.position;
                        playerControl.GetComponent<DummyBehaviour>().enabled = true;
                        playerControl.NetTransform.enabled = false;
                        playerControl.SetName(
                            $"{TranslationController.Instance.GetString(StringNames.Dummy, Array.Empty<Il2CppSystem.Object>())} {i}");
                        var color = (byte) (i % Palette.PlayerColors.Length);
                        playerControl.SetColor(color);
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
                DebuggerWindow.Enabled = !DebuggerWindow.Enabled;
            }
        }

        private void OnGUI()
        {
            DebuggerWindow.OnGUI();
        }
    }
}
