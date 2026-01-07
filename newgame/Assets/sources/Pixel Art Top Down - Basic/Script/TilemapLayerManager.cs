using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapLayerManager : MonoBehaviour
{
    [System.Serializable]
    public class TilemapLayerSettings
    {
        [Tooltip("Tilemap的名称（支持部分匹配）")]
        public string tilemapNamePattern;

        [Tooltip("Layer名称（0-31）")]
        public string layerName = "Default";

        [Tooltip("Sorting Layer名称")]
        public string sortingLayerName = "Default";

        [Tooltip("Sorting Order（数值越大越靠前）")]
        public int sortingOrder = 0;

        [Tooltip("是否启用Tilemap Collider")]
        public bool enableCollider = true;

        [Tooltip("Tilemap颜色（用于可视化区分）")]
        public Color tintColor = Color.white;
    }

    [Header("Tilemap层设置")]
    [Tooltip("按顺序排列，越靠前的层越在背景")]
    public List<TilemapLayerSettings> layerSettings = new List<TilemapLayerSettings>();

    [Header("自动设置")]
    [Tooltip("是否在Start时自动应用设置")]
    public bool applyOnStart = true;

    [Tooltip("是否在Awake时自动应用设置")]
    public bool applyOnAwake = false;

    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    public bool debugMode = false;

    private Dictionary<Tilemap, TilemapLayerSettings> tilemapSettings = new Dictionary<Tilemap, TilemapLayerSettings>();

    void Awake()
    {
        if (applyOnAwake)
        {
            ApplyAllTilemapLayers();
        }
    }

    void Start()
    {
        if (applyOnStart)
        {
            ApplyAllTilemapLayers();
        }
    }

    /// <summary>
    /// 应用所有Tilemap的层级设置
    /// </summary>
    [ContextMenu("应用所有Tilemap层级设置")]
    public void ApplyAllTilemapLayers()
    {
        Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>(true);

        if (debugMode)
            Debug.Log($"找到 {tilemaps.Length} 个Tilemap", this);

        tilemapSettings.Clear();

        foreach (var tilemap in tilemaps)
        {
            ApplyTilemapLayer(tilemap);
        }

        Debug.Log($"已应用 {tilemapSettings.Count} 个Tilemap的层级设置", this);
    }

    /// <summary>
    /// 应用单个Tilemap的层级设置
    /// </summary>
    public void ApplyTilemapLayer(Tilemap tilemap)
    {
        if (tilemap == null) return;

        // 查找匹配的设置
        TilemapLayerSettings matchedSettings = null;
        foreach (var settings in layerSettings)
        {
            if (string.IsNullOrEmpty(settings.tilemapNamePattern) ||
                tilemap.name.Contains(settings.tilemapNamePattern))
            {
                matchedSettings = settings;
                break;
            }
        }

        if (matchedSettings == null)
        {
            if (debugMode)
                Debug.LogWarning($"未找到Tilemap '{tilemap.name}' 的匹配设置，使用默认", tilemap);
            return;
        }

        // 保存设置
        tilemapSettings[tilemap] = matchedSettings;

        // 设置Layer
        int layer = LayerMask.NameToLayer(matchedSettings.layerName);
        if (layer >= 0)
        {
            tilemap.gameObject.layer = layer;
        }
        else
        {
            Debug.LogWarning($"Layer '{matchedSettings.layerName}' 不存在，Tilemap: {tilemap.name}", tilemap);
        }

        // 设置TilemapRenderer
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = matchedSettings.sortingLayerName;
            renderer.sortingOrder = matchedSettings.sortingOrder;


            if (debugMode)
                Debug.Log($"设置Tilemap '{tilemap.name}': Layer={matchedSettings.layerName}, " +
                         $"SortingLayer={matchedSettings.sortingLayerName}, Order={matchedSettings.sortingOrder}", tilemap);
        }
        tilemap.color = matchedSettings.tintColor;

        // 设置Tilemap Collider
        TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            collider.enabled = matchedSettings.enableCollider;
        }

        // 如果有Composite Collider，也设置
        CompositeCollider2D compositeCollider = tilemap.GetComponent<CompositeCollider2D>();
        if (compositeCollider != null)
        {
            compositeCollider.enabled = matchedSettings.enableCollider;
        }
    }

    /// <summary>
    /// 根据名称获取Tilemap
    /// </summary>
    public Tilemap GetTilemapByName(string name)
    {
        Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>(true);
        foreach (var tilemap in tilemaps)
        {
            if (tilemap.name == name || tilemap.name.Contains(name))
                return tilemap;
        }
        return null;
    }

    /// <summary>
    /// 修改Tilemap的设置
    /// </summary>
    public void UpdateTilemapSettings(string tilemapName, TilemapLayerSettings newSettings)
    {
        Tilemap tilemap = GetTilemapByName(tilemapName);
        if (tilemap != null)
        {
            // 更新设置列表中的匹配项
            for (int i = 0; i < layerSettings.Count; i++)
            {
                if (layerSettings[i].tilemapNamePattern == tilemapName ||
                    tilemap.name.Contains(layerSettings[i].tilemapNamePattern))
                {
                    layerSettings[i] = newSettings;
                    break;
                }
            }

            // 重新应用
            ApplyTilemapLayer(tilemap);
        }
    }

    /// <summary>
    /// 临时禁用所有Tilemap的碰撞
    /// </summary>
    public void DisableAllColliders()
    {
        TilemapCollider2D[] colliders = GetComponentsInChildren<TilemapCollider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }

    /// <summary>
    /// 启用所有Tilemap的碰撞
    /// </summary>
    public void EnableAllColliders()
    {
        TilemapCollider2D[] colliders = GetComponentsInChildren<TilemapCollider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
    }

    /// <summary>
    /// 获取Tilemap的当前设置
    /// </summary>
    public TilemapLayerSettings GetTilemapSettings(Tilemap tilemap)
    {
        if (tilemapSettings.TryGetValue(tilemap, out var settings))
            return settings;
        return null;
    }

    /// <summary>
    /// 创建预设的层级配置
    /// </summary>
    [ContextMenu("创建预设层级配置")]
    public void CreateDefaultLayers()
    {
        layerSettings.Clear();

        // 添加常见图层配置
        layerSettings.Add(new TilemapLayerSettings
        {
            tilemapNamePattern = "Background",
            layerName = "Default",
            sortingLayerName = "Background",
            sortingOrder = -10,
            tintColor = new Color(0.8f, 0.8f, 0.8f, 1f)
        });

        layerSettings.Add(new TilemapLayerSettings
        {
            tilemapNamePattern = "Ground",
            layerName = "Ground",
            sortingLayerName = "Ground",
            sortingOrder = 0,
            enableCollider = true,
            tintColor = Color.white
        });

        layerSettings.Add(new TilemapLayerSettings
        {
            tilemapNamePattern = "Walls",
            layerName = "Walls",
            sortingLayerName = "Walls",
            sortingOrder = 5,
            enableCollider = true,
            tintColor = new Color(1f, 0.9f, 0.9f, 1f)
        });

        layerSettings.Add(new TilemapLayerSettings
        {
            tilemapNamePattern = "Foreground",
            layerName = "Foreground",
            sortingLayerName = "Foreground",
            sortingOrder = 10,
            enableCollider = false,
            tintColor = new Color(1f, 1f, 1f, 0.8f)
        });

        layerSettings.Add(new TilemapLayerSettings
        {
            tilemapNamePattern = "Decorations",
            layerName = "Default",
            sortingLayerName = "Decorations",
            sortingOrder = 15,
            enableCollider = false,
            tintColor = Color.white
        });

        Debug.Log("已创建预设层级配置", this);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 在编辑器中验证设置
    /// </summary>
    private void OnValidate()
    {
        // 检查Layer是否存在
        foreach (var settings in layerSettings)
        {
            if (LayerMask.NameToLayer(settings.layerName) == -1)
            {
                Debug.LogWarning($"Layer '{settings.layerName}' 不存在!", this);
            }
        }
    }

    /// <summary>
    /// 编辑器辅助：绘制Tilemap边界
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!debugMode) return;

        Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>();
        foreach (var tilemap in tilemaps)
        {
            if (tilemapSettings.TryGetValue(tilemap, out var settings))
            {
                // 使用设置的颜色绘制边界
                Gizmos.color = settings.tintColor;
                Bounds bounds = tilemap.localBounds;
                Gizmos.DrawWireCube(tilemap.transform.position + bounds.center, bounds.size);
            }
        }
    }
#endif
}