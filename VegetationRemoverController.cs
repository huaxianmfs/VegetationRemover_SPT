using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace VegetationRemover
{
    public class VegetationRemoverController : MonoBehaviour
    {
        private GameWorld _gameWorld;
        private int _state = 0;
        private float _stateTime = 0;
        private bool _updateLogged = false;

        // 缓存标记
        private bool _grassCached = false;
        private bool _treeCached = false;
        private bool _collisionCached = false;

        // 草
        private bool _grassRemoved = false;
        private readonly List<MonoBehaviour> _grassManagers = new List<MonoBehaviour>();
        private readonly Dictionary<Terrain, float> _terrainDensityBackup = new Dictionary<Terrain, float>();

        // 树
        private bool _treesRemoved = false;
        private readonly List<GameObject> _treeObjects = new List<GameObject>();

        // 碰撞
        private bool _collisionRemoved = false;
        private readonly List<GameObject> _collisionObjects = new List<GameObject>();

        public VegetationRemoverController(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            if (!_updateLogged)
            {
                _updateLogged = true;
                _stateTime = Time.time;
                VegetationRemoverPlugin.Logger.LogInfo("[Vegetation] Update loop running.");
            }

            switch (_state)
            {
                case 0:
                    if (!Singleton<GameWorld>.Instantiated) return;
                    _gameWorld = Singleton<GameWorld>.Instance;
                    if (_gameWorld == null) return;
                    VegetationRemoverPlugin.Logger.LogInfo("[Vegetation] GameWorld detected.");
                    _state = 1;
                    _stateTime = Time.time;
                    break;

                case 1:
                    if (_gameWorld.MainPlayer == null)
                    {
                        if (Time.time - _stateTime > 60f)
                        {
                            VegetationRemoverPlugin.Logger.LogWarning("[Vegetation] Timeout waiting for MainPlayer.");
                            _state = 3;
                        }
                        return;
                    }
                    VegetationRemoverPlugin.Logger.LogInfo("[Vegetation] MainPlayer detected.");
                    _state = 2;
                    _stateTime = Time.time;
                    break;

                case 2:
                    // 缩短到 1.5s，仅让场景资源短暂稳定
                    if (Time.time - _stateTime < 1.5f) return;

                    VegetationRemoverPlugin.Logger.LogInfo(
                        "[Vegetation] Ready. Ctrl+Numpad1=Grass, Ctrl+Numpad2=Trees, Ctrl+Numpad3=BushCollision");

                    // 配置项默认开启才自动执行；否则仅缓存，等按键
                    if (VegetationRemoverPlugin.RemoveGrass.Value) ToggleGrass();
                    if (VegetationRemoverPlugin.RemoveTrees.Value) ToggleTrees();
                    if (VegetationRemoverPlugin.RemoveBushCollision.Value) ToggleBushCollision();

                    _state = 3;
                    break;

                case 3:
                    bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                    if (!ctrl) return;

                    if (Input.GetKeyDown(KeyCode.Keypad1)) ToggleGrass();
                    if (Input.GetKeyDown(KeyCode.Keypad2)) ToggleTrees();
                    if (Input.GetKeyDown(KeyCode.Keypad3)) ToggleBushCollision();
                    break;
            }
        }

        // ================= 草 =================
        private void CacheGrass()
        {
            _grassManagers.Clear();
            _terrainDensityBackup.Clear();

            // 遍历所有 MonoBehaviour（一次性），匹配 GPUInstancer Detail 管理器
            var all = Resources.FindObjectsOfTypeAll(Il2CppType.Of<MonoBehaviour>());
            foreach (var obj in all)
            {
                var mb = obj.TryCast<MonoBehaviour>();
                if (mb == null) continue;
                var name = mb.GetIl2CppType()?.Name;
                if (string.IsNullOrEmpty(name)) continue;
                if (!name.Contains("GPUInstancer")) continue;
                if (!name.Contains("Detail")) continue;
                _grassManagers.Add(mb);
            }

            // 兜底：把 Terrain 的 density 也先记下来（只有找不到管理器时才用）
            if (_grassManagers.Count == 0)
            {
                var terrains = Terrain.activeTerrains;
                if (terrains != null)
                {
                    foreach (var t in terrains)
                    {
                        if (t == null) continue;
                        _terrainDensityBackup[t] = t.detailObjectDensity;
                    }
                }
            }

            VegetationRemoverPlugin.Logger.LogInfo(
                $"[Vegetation] Grass cache: managers={_grassManagers.Count}, terrains={_terrainDensityBackup.Count}");
        }

        private void ToggleGrass()
        {
            try
            {
                if (!_grassCached)
                {
                    CacheGrass();
                    _grassCached = true;
                }

                if (!_grassRemoved)
                {
                    int n = 0;

                    foreach (var mgr in _grassManagers)
                    {
                        if (mgr == null) continue;
                        mgr.enabled = false;
                        n++;
                    }
                    foreach (var kv in _terrainDensityBackup)
                    {
                        if (kv.Key == null) continue;
                        kv.Key.detailObjectDensity = 0f;
                        n++;
                    }

                    _grassRemoved = true;
                    VegetationRemoverPlugin.Logger.LogInfo($"[Vegetation] Grass removed ({n}).");
                }
                else
                {
                    foreach (var mgr in _grassManagers)
                        if (mgr != null) mgr.enabled = true;

                    foreach (var kv in _terrainDensityBackup)
                        if (kv.Key != null) kv.Key.detailObjectDensity = kv.Value;

                    _grassRemoved = false;
                    VegetationRemoverPlugin.Logger.LogInfo("[Vegetation] Grass restored.");
                }
            }
            catch (Exception ex)
            {
                VegetationRemoverPlugin.Logger.LogError($"ToggleGrass failed: {ex}");
            }
        }

        // ================= 树 =================
        private void CacheTrees()
        {
            _treeObjects.Clear();

            var locationId = _gameWorld.LocationId.ToLowerInvariant();
            List<GameObject> found;

            switch (locationId)
            {
                case "woods":
                case "bigmap":
                case "rezervbase":
                case "interchange":
                    found = FindAll<GameObject>()
                        .Where(x => x.name.ToLower().Contains("slice_") &&
                                    x.name.ToLower().Contains("_trees"))
                        .ToList();
                    break;

                case "tarkovstreets":
                    found = FindAll<GameObject>()
                        .Where(x => x.name.ToLower().Contains("_plants"))
                        .ToList();
                    break;

                case "lighthouse":
                    found = FindAll<GameObject>()
                        .Where(x => x.name.ToLower().Contains("sbg_trees"))
                        .ToList();
                    break;

                case "shoreline":
                    found = FindAll<GameObject>()
                        .Where(x => x.name.ToLower().Contains("sbg_trees") ||
                                    x.name.ToLower().Contains("sbg_shoreline_plants"))
                        .ToList();
                    break;

                case "sandbox":
                case "sandbox_high":
                    found = FindAll<GameObject>()
                        .Where(x => x.name.ToLower().Contains("sbg_sandbox_") &&
                                    x.name.ToLower().Contains("_plants"))
                        .ToList();
                    break;

                default:
                    found = new List<GameObject>();
                    break;
            }

            _treeObjects.AddRange(found);
            VegetationRemoverPlugin.Logger.LogInfo(
                $"[Vegetation] Tree cache: {_treeObjects.Count} on {locationId}.");
        }

        private void ToggleTrees()
        {
            try
            {
                if (!_treeCached)
                {
                    CacheTrees();
                    _treeCached = true;
                }

                if (!_treesRemoved)
                {
                    foreach (var go in _treeObjects)
                        if (go != null) go.SetActive(false);

                    _treesRemoved = true;
                    VegetationRemoverPlugin.Logger.LogInfo(
                        $"[Vegetation] Trees removed ({_treeObjects.Count}).");
                }
                else
                {
                    foreach (var go in _treeObjects)
                        if (go != null) go.SetActive(true);

                    _treesRemoved = false;
                    VegetationRemoverPlugin.Logger.LogInfo(
                        $"[Vegetation] Trees restored ({_treeObjects.Count}).");
                }
            }
            catch (Exception ex)
            {
                VegetationRemoverPlugin.Logger.LogError($"ToggleTrees failed: {ex}");
            }
        }

        // ================= 灌木/沼泽碰撞 =================
        private void CacheCollision()
        {
            _collisionObjects.Clear();

            var obstacles = FindAll<ObstacleCollider>();
            foreach (var obs in obstacles)
            {
                if (obs == null) continue;

                var parentName = obs.transform?.parent?.gameObject?.name?.ToLower() ?? "";
                var ownName = obs.transform?.name?.ToLower() ?? "";

                bool isBush = parentName.Contains("filbert") || parentName.Contains("fibert")
                           || ownName.Contains("filbert")   || ownName.Contains("fibert");
                bool isSwamp = ownName.Contains("swamp");

                if (isBush || isSwamp)
                    _collisionObjects.Add(obs.gameObject);
            }

            var boxes = FindAll<BoxCollider>();
            foreach (var box in boxes)
            {
                if (box != null && box.name == "Swamp_collider")
                    _collisionObjects.Add(box.gameObject);
            }

            VegetationRemoverPlugin.Logger.LogInfo(
                $"[Vegetation] Collision cache: {_collisionObjects.Count}.");
        }

        private void ToggleBushCollision()
        {
            try
            {
                if (!_collisionCached)
                {
                    CacheCollision();
                    _collisionCached = true;
                }

                if (!_collisionRemoved)
                {
                    foreach (var go in _collisionObjects)
                        if (go != null) go.SetActive(false);

                    _collisionRemoved = true;
                    VegetationRemoverPlugin.Logger.LogInfo(
                        $"[Vegetation] Collision removed ({_collisionObjects.Count}).");
                }
                else
                {
                    foreach (var go in _collisionObjects)
                        if (go != null) go.SetActive(true);

                    _collisionRemoved = false;
                    VegetationRemoverPlugin.Logger.LogInfo(
                        $"[Vegetation] Collision restored ({_collisionObjects.Count}).");
                }
            }
            catch (Exception ex)
            {
                VegetationRemoverPlugin.Logger.LogError($"ToggleBushCollision failed: {ex}");
            }
        }

        // ================= 工具 =================
        private static List<T> FindAll<T>() where T : Il2CppObjectBase
        {
            return Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>())
                .Select(x => x.TryCast<T>())
                .Where(x => x != null)
                .ToList();
        }
    }
}