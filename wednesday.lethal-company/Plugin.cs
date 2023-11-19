using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace wednesday.lethal_company;

[BepInPlugin("2f07ff27-48ea-4482-bf9d-ec6d7d2846ca", "wednesday.lethal-company", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    private static bool _shouldDraw;
    
    private static Rect _statsWindow = new (0, 0, 200, 117);
    private static readonly Rect StatsArea = new (10, 20, 180, 90);
    private static float _statsAmount;

    private static Rect _timeWindow = new (210, 0, 200, 115);
    private static readonly Rect TimeArea =  new (10, 20, 180, 90);
    private static float _timeModifier = 1f;
    private static bool _shouldModifyTime;

    private static Rect _playerWindow = new(420, 0, 200, 215);
    private static readonly Rect PlayerArea = new(10, 20, 180, 190);
    public static float DamageModifer = 1f;
    public static bool ShouldModifyDamage;
    public static float SpeedModifer = 1f;
    public static bool ShouldModifySpeed;
    public static float WeightModifier = 1f;
    public static bool ShouldModifyWeight;
    
    private static Rect _itemWindow = new (630, 0, 200, 88);
    private static readonly Rect ItemArea =  new (10, 20, 180, 74);
    public static float BatteryModifer = 1f;
    public static bool ShouldModifyBattery;
    
    public void Awake()
    {
        Logger.LogInfo("Running Patches.");

        var harmonyInstance = new Harmony("wednesday");
        harmonyInstance.PatchAll(typeof(DamagePatch));
        harmonyInstance.PatchAll(typeof(SpeedPatch));
        harmonyInstance.PatchAll(typeof(ItemPatch));
        
        Logger.LogInfo("Loaded!");
    }
    
    public void Update()
    {
        var quickMenuManager = FindObjectOfType<QuickMenuManager>();

        _shouldDraw = false;
        
        if (quickMenuManager)
            _shouldDraw = quickMenuManager.isMenuOpen;
    }

    public void OnGUI()
    {
        if (!_shouldDraw)
            return;
        
        _statsWindow = GUI.Window(1, _statsWindow, StatsWindowRoutine, "Stats");
        _timeWindow = GUI.Window(2, _timeWindow, TimeWindowRoutine, "Time");
        _playerWindow = GUI.Window(3, _playerWindow, PlayerWindowRoutine, "Player");
        _itemWindow = GUI.Window(4, _itemWindow, ItemWindowRoutine, "Item");
    }

    public void StatsWindowRoutine(int windowId)
    {
        GUILayout.BeginArea(StatsArea);

        GUILayout.Label($"Stat Amount: {(int)_statsAmount}");
        _statsAmount = GUILayout.HorizontalSlider(_statsAmount, -1000f, 1000f);

        if (GUILayout.Button("Give Money"))
        {
            var terminal = FindObjectOfType<Terminal>();

            if (terminal)
                terminal.groupCredits += (int)_statsAmount;
        }

        if (GUILayout.Button("Give Quota"))
        { 
            var timeOfDay = FindObjectOfType<TimeOfDay>();

            if (timeOfDay)
            {
                timeOfDay.quotaFulfilled += (int)_statsAmount;
                timeOfDay.UpdateProfitQuotaCurrentTime();
            }
        }
        
        GUILayout.EndArea();
        
        GUI.DragWindow();
    }
    
    public void TimeWindowRoutine(int windowId)
    {
        GUILayout.BeginArea(TimeArea);

        GUILayout.Label($"Time Value: {_timeModifier:0.##}");
        _timeModifier = GUILayout.HorizontalSlider(_timeModifier, 0f, 2f);
        var didModifyTime = _shouldModifyTime;
        _shouldModifyTime = GUILayout.Toggle(_shouldModifyTime, "Time Modifier");
        
        if (_shouldModifyTime)
        {
            var timeOfDay = FindObjectOfType<TimeOfDay>();

            if (timeOfDay)
                timeOfDay.globalTimeSpeedMultiplier = _timeModifier;
        }

        if (didModifyTime && !_shouldModifyTime)
        {
            var timeOfDay = FindObjectOfType<TimeOfDay>();

            if (timeOfDay)
                timeOfDay.globalTimeSpeedMultiplier = 1f;
        }

        if (GUILayout.Button("Reset Time"))
        { 
            var timeOfDay = FindObjectOfType<TimeOfDay>();

            if (timeOfDay)
                timeOfDay.globalTime = 100f;
        }
        
        GUILayout.EndArea();
        
        GUI.DragWindow();
    }

    public void PlayerWindowRoutine(int windowId)
    {
        GUILayout.BeginArea(PlayerArea);

        GUILayout.Label($"Damage Value: {DamageModifer:0.##}");
        DamageModifer = GUILayout.HorizontalSlider(DamageModifer, 0f, 2f);
        ShouldModifyDamage = GUILayout.Toggle(ShouldModifyDamage, "Damage Modifier");
        
        GUILayout.Label($"Speed Value: {SpeedModifer:0.##}");
        SpeedModifer = GUILayout.HorizontalSlider(SpeedModifer, 0f, 20f);
        ShouldModifySpeed = GUILayout.Toggle(ShouldModifySpeed, "Speed Modifier");

        GUILayout.Label($"Weight Value: {WeightModifier:0.##}");
        WeightModifier = GUILayout.HorizontalSlider(WeightModifier, 0f, 2f);
        ShouldModifyWeight = GUILayout.Toggle(ShouldModifyWeight, "Weight Modifier");
        
        GUILayout.EndArea();
        
        GUI.DragWindow();
    }

    public void ItemWindowRoutine(int windowId)
    {
        GUILayout.BeginArea(ItemArea);

        GUILayout.Label($"Battery Value: {BatteryModifer:0.##}");
        BatteryModifer = GUILayout.HorizontalSlider(BatteryModifer, 0f, 2f);
        ShouldModifyBattery = GUILayout.Toggle(ShouldModifyBattery, "Battery Modifier");

        GUILayout.EndArea();
        
        GUI.DragWindow();
    }
}

internal class DamagePatch
{
    [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
    [HarmonyPrefix]
    private static bool Patch(ref int damageNumber)
    {
        if (Plugin.ShouldModifyDamage)
            damageNumber = (int)(damageNumber * Plugin.DamageModifer);

        return true;
    }
}

internal class SpeedPatch
{
    private static float _oldWeight = 1f; 
    
    [HarmonyPatch(typeof(PlayerControllerB), "SpeedCheat_performed")]
    [HarmonyPrefix]
    private static bool AntiCheatPatch()
    {
        // This was originally the anti-cheat.
        // Just not running the original code should get rid of the pesky error console.
        return false;
    }
    
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    [HarmonyPrefix]
    private static bool UpdatePatchPre()
    {
        if (!Plugin.ShouldModifyWeight)
            return true;
        
        var localPlayerController = GameNetworkManager.Instance.localPlayerController;

        if (!localPlayerController)
            return true;

        _oldWeight = localPlayerController.carryWeight;
        localPlayerController.carryWeight = Math.Max(localPlayerController.carryWeight * Plugin.WeightModifier, 1f);

        return true;
    }
    
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    [HarmonyPostfix]
    private static void UpdatePatchPost()
    {
        var localPlayerController = GameNetworkManager.Instance.localPlayerController;
        var field = typeof(PlayerControllerB).GetField("sprintMultiplier",
            BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance)!;

        if (!localPlayerController)
            return;
        
        if (!field.Equals(null) && Plugin.ShouldModifySpeed)
            field.SetValue(localPlayerController, Plugin.SpeedModifer);

        if (Plugin.ShouldModifyWeight)
            localPlayerController.carryWeight = _oldWeight;
    }
}

internal class ItemPatch
{
    private static float _oldUsage = 1f;
    
    [HarmonyPatch(typeof(GrabbableObject), "Update")]
    [HarmonyPrefix]
    // ReSharper disable once InconsistentNaming
    private static bool PatchPre(GrabbableObject __instance)
    {
        if (!Plugin.ShouldModifyBattery)
            return true;
        
        _oldUsage = __instance.itemProperties.batteryUsage;
        __instance.itemProperties.batteryUsage *= (2f + 1f / 10f + -Plugin.BatteryModifer) * 2f + float.Epsilon; // Time.deltaTime / 0 = no no.

        return true;
    }
    
    [HarmonyPatch(typeof(GrabbableObject), "Update")]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    private static void PatchPost(GrabbableObject __instance)
    {
        if (!Plugin.ShouldModifyBattery)
            return;
        
        __instance.itemProperties.batteryUsage = _oldUsage;
    }
}