using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace VegetationRemover
{
    [BepInPlugin(PluginGuid, "Vegetation Remover", "1.0.0")]
    public class VegetationRemoverPlugin : BasePlugin
    {
        public const string PluginGuid = "com.cwx.vegetationremover";

        public static ManualLogSource Logger;

        public static ConfigEntry<bool> RemoveGrass;
        public static ConfigEntry<bool> RemoveTrees;
        public static ConfigEntry<bool> RemoveBushCollision;

        public override void Load()
        {
            Logger = base.Log;
            Logger.LogInfo("[Vegetation] Plugin Load() called.");

            RemoveGrass = Config.Bind("Features", "Remove Grass", false,
                "移除地图上所有草");
            RemoveTrees = Config.Bind("Features", "Remove Trees", false,
                "移除地图上的树木和大型植被");
            RemoveBushCollision = Config.Bind("Features", "Remove Bush Collision", false,
                "移除灌木和沼泽的碰撞/减速效果");

            ClassInjector.RegisterTypeInIl2Cpp<VegetationRemoverController>();
            Logger.LogInfo("[Vegetation] VegetationRemoverController registered.");

            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll();
            Logger.LogInfo("[Vegetation] Harmony PatchAll completed.");

            Logger.LogInfo("Vegetation Remover plugin loaded.");
        }
    }
}