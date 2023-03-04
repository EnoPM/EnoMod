using System.Linq;
using EnoMod.Kernel;
using EnoMod.Utils;
using UnityEngine;

namespace EnoMod.Customs.Modules;

public static class BetterPolus
{
    public static CustomOption EnableBetterPolus;
    public static CustomOption BetterPolusVitals;
    public static CustomOption BetterPolusWifi;
    public static CustomOption BetterPolusVents;

    [EnoHook(CustomHooks.LoadCustomOptions)]
    public static Hooks.Result LoadCustomOptions()
    {
        EnableBetterPolus = Singleton<CustomOption.Holder>.Instance.Settings.CreateBool(
            nameof(EnableBetterPolus),
            Colors.Cs("#09730b", "Enable BetterPolus"),
            false);
        BetterPolusVitals = Singleton<CustomOption.Holder>.Instance.Settings.CreateBool(
            nameof(BetterPolusVitals),
            Colors.Cs("#18ad1b", "Vitals in laboratory"),
            false,
            EnableBetterPolus);
        BetterPolusWifi = Singleton<CustomOption.Holder>.Instance.Settings.CreateBool(
            nameof(BetterPolusWifi),
            Colors.Cs("#18ad1b", "Wifi in dropship"),
            false,
            EnableBetterPolus);
        BetterPolusVents = Singleton<CustomOption.Holder>.Instance.Settings.CreateBool(
            nameof(BetterPolusVents),
            Colors.Cs("#18ad1b", "Change reactor vents"),
            false,
            EnableBetterPolus);
        return Hooks.Result.Continue;
    }

    // Positions
    private static readonly Vector3 _dvdScreenNewPos = new(26.635f, -15.92f, 1f);
    private static readonly Vector3 _vitalsNewPos = new(31.275f, -6.45f, 1f);

    private static readonly Vector3 _wifiNewPos = new(15.975f, 0.084f, 1f);
    private static readonly Vector3 _navNewPos = new(11.07f, -15.298f, -0.015f);

    private static readonly Vector3 _tempColdNewPos = new(7.772f, -17.103f, -0.017f);

    // Scales
    private const float DvdScreenNewScale = 0.75f;

    // Checks
    private static bool _isAdjustmentsDone;
    private static bool _isObjectsFetched;
    private static bool _isRoomsFetched;
    private static bool _isVentsFetched;

    // Tasks Tweak
    private static Console? _wifiConsole;
    private static Console? _navConsole;

    // Vitals Tweak
    private static SystemConsole? _vitals;
    private static GameObject? _dvdScreenOffice;

    // Vents Tweak
    private static Vent? _electricBuildingVent;
    private static Vent? _electricalVent;
    private static Vent? _scienceBuildingVent;
    private static Vent? _storageVent;

    // TempCold Tweak
    private static Console? _tempCold;

    // Rooms
    private static GameObject? _comms;
    private static GameObject? _dropShip;
    private static GameObject? _outside;
    private static GameObject? _science;

    private static bool IsBetterPolusEnabled()
    {
        return EnableBetterPolus;
    }

    private static bool VitalsAllowed()
    {
        return IsBetterPolusEnabled() && BetterPolusVitals;
    }

    private static bool VentsAllowed()
    {
        return IsBetterPolusEnabled() && BetterPolusVents;
    }

    private static bool WifiAllowed()
    {
        return IsBetterPolusEnabled() && BetterPolusWifi;
    }

    [EnoHook(CustomHooks.ShipStatusBegin)]
    public static Hooks.Result ShipStatusBegin(ShipStatus shipStatus)
    {
        ApplyChanges(shipStatus);
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.ShipStatusAwake)]
    public static Hooks.Result ShipStatusAwake(ShipStatus shipStatus)
    {
        ApplyChanges(shipStatus);
        return Hooks.Result.Continue;
    }

    [EnoHook(CustomHooks.ShipStatusFixedUpdate)]
    public static Hooks.Result ShipStatusFixedUpdate(ShipStatus shipStatus)
    {
        if (!_isObjectsFetched || !_isAdjustmentsDone)
        {
            ApplyChanges(shipStatus);
        }

        return Hooks.Result.Continue;
    }

    private static void ApplyChanges(ShipStatus shipStatus)
    {
        if (shipStatus.Type != ShipStatus.MapType.Pb) return;
        FindPolusObjects();
        AdjustPolus();
    }

    private static void FindPolusObjects()
    {
        FindVents();
        FindRooms();
        FindObjects();
    }

    private static void AdjustPolus()
    {
        if (_isObjectsFetched && _isRoomsFetched)
        {
            if (VitalsAllowed())
            {
                MoveVitals();
                MoveTempCold();
            }

            if (WifiAllowed())
            {
                SwitchNavWifi();
            }
        }
        else
        {
            EnoModPlugin.Logger.LogError("Couldn't move elements as not all of them have been fetched.");
        }

        if (VentsAllowed())
        {
            AdjustVents();
        }

        _isAdjustmentsDone = true;
    }

    // --------------------
    // - Objects Fetching -
    // --------------------
    private static void FindVents()
    {
        var ventsList = Object.FindObjectsOfType<Vent>().ToList();

        if (_electricBuildingVent == null)
        {
            _electricBuildingVent = ventsList.Find(vent => vent.gameObject.name == "ElectricBuildingVent");
        }

        if (_electricalVent == null)
        {
            _electricalVent = ventsList.Find(vent => vent.gameObject.name == "ElectricalVent");
        }

        if (_scienceBuildingVent == null)
        {
            _scienceBuildingVent = ventsList.Find(vent => vent.gameObject.name == "ScienceBuildingVent");
        }

        if (_storageVent == null)
        {
            _storageVent = ventsList.Find(vent => vent.gameObject.name == "StorageVent");
        }

        _isVentsFetched = _electricBuildingVent != null && _electricalVent != null && _scienceBuildingVent != null &&
                          _storageVent != null;
    }

    private static void FindRooms()
    {
        if (_comms == null)
        {
            _comms = Object.FindObjectsOfType<GameObject>().ToList().Find(o => o.name == "Comms");
        }

        if (_dropShip == null)
        {
            _dropShip = Object.FindObjectsOfType<GameObject>().ToList().Find(o => o.name == "Dropship");
        }

        if (_outside == null)
        {
            _outside = Object.FindObjectsOfType<GameObject>().ToList().Find(o => o.name == "Outside");
        }

        if (_science == null)
        {
            _science = Object.FindObjectsOfType<GameObject>().ToList().Find(o => o.name == "Science");
        }

        _isRoomsFetched = _comms != null && _dropShip != null && _outside != null && _science != null;
    }

    private static void FindObjects()
    {
        if (_wifiConsole == null)
        {
            _wifiConsole = Object.FindObjectsOfType<Console>().ToList()
                .Find(console => console.name == "panel_wifi");
        }

        if (_navConsole == null)
        {
            _navConsole = Object.FindObjectsOfType<Console>().ToList()
                .Find(console => console.name == "panel_nav");
        }

        if (_vitals == null)
        {
            _vitals = Object.FindObjectsOfType<SystemConsole>().ToList()
                .Find(console => console.name == "panel_vitals");
        }

        if (_dvdScreenOffice == null)
        {
            var dvdScreenAdmin = Object.FindObjectsOfType<GameObject>().ToList()
                .Find(o => o.name == "dvdscreen");

            if (dvdScreenAdmin != null)
            {
                _dvdScreenOffice = Object.Instantiate(dvdScreenAdmin);
            }
        }

        if (_tempCold == null)
        {
            _tempCold = Object.FindObjectsOfType<Console>().ToList()
                .Find(console => console.name == "panel_tempcold");
        }

        _isObjectsFetched = _wifiConsole != null && _navConsole != null && _vitals != null &&
                            _dvdScreenOffice != null && _tempCold != null;
    }

    // -------------------
    // - Map Adjustments -
    // -------------------
    private static void AdjustVents()
    {
        if (_isVentsFetched)
        {
            if (_electricBuildingVent != null) _electricBuildingVent.Left = _electricalVent;
            if (_electricalVent != null) _electricalVent.Center = _electricBuildingVent;
            if (_scienceBuildingVent != null) _scienceBuildingVent.Left = _storageVent;
            if (_storageVent != null) _storageVent.Center = _scienceBuildingVent;
        }
        else
        {
            EnoModPlugin.Logger.LogError("Couldn't adjust Vents as not all objects have been fetched.");
        }
    }

    private static void MoveTempCold()
    {
        if (_outside == null || _tempCold == null || _tempCold.transform.position == _tempColdNewPos) return;

        var tempColdTransform = _tempCold.transform;
        tempColdTransform.parent = _outside.transform;
        tempColdTransform.position = _tempColdNewPos;

        // Fixes collider being too high
        var collider = _tempCold.GetComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.size += new Vector2(0f, -0.3f);
    }

    private static void SwitchNavWifi()
    {
        if (_wifiConsole != null && _wifiConsole.transform.position != _wifiNewPos)
        {
            var wifiTransform = _wifiConsole.transform;
            if (_dropShip != null) wifiTransform.parent = _dropShip.transform;
            wifiTransform.position = _wifiNewPos;
        }

        if (_navConsole != null && _navConsole.transform.position != _navNewPos)
        {
            var navTransform = _navConsole.transform;
            if (_comms != null) navTransform.parent = _comms.transform;
            navTransform.position = _navNewPos;
        }

        // Prevents crewmate being able to do the task from outside
        if (_navConsole != null) _navConsole.checkWalls = true;
    }

    private static void MoveVitals()
    {
        if (_vitals != null && _vitals.transform.position != _vitalsNewPos)
        {
            // Vitals
            var vitalsTransform = _vitals.gameObject.transform;
            if (_science != null) vitalsTransform.parent = _science.transform;
            vitalsTransform.position = _vitalsNewPos;
        }

        if (_dvdScreenOffice == null || _dvdScreenOffice.transform.position == _dvdScreenNewPos) return;

        // DvdScreen
        var dvdScreenTransform = _dvdScreenOffice.transform;
        dvdScreenTransform.position = _dvdScreenNewPos;

        var localScale = dvdScreenTransform.localScale;
        localScale = new Vector3(DvdScreenNewScale, localScale.y, localScale.z);
        dvdScreenTransform.localScale = localScale;
    }
}
