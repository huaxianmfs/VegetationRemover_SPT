using Comfort.Common;
using EFT;
using HarmonyLib;

namespace VegetationRemover
{
    [HarmonyPatch(typeof(GameWorld), nameof(GameWorld.OnGameStarted))]
    internal static class VegetationRemoverPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                VegetationRemoverPlugin.Logger.LogInfo("[Vegetation] OnGameStarted postfix fired.");

                var gw = Singleton<GameWorld>.Instance;
                if (gw == null)
                {
                    VegetationRemoverPlugin.Logger.LogWarning("[Vegetation] GameWorld is null.");
                    return;
                }

                gw.gameObject.AddComponent<VegetationRemoverController>();
                VegetationRemoverPlugin.Logger.LogInfo("[Vegetation] Controller added to GameWorld.");
            }
            catch (System.Exception ex)
            {
                VegetationRemoverPlugin.Logger.LogError($"[Vegetation] Failed to add controller: {ex}");
            }
        }
    }
}