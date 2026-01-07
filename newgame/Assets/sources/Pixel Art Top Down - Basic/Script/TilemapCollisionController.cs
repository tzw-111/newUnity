using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapCollisionController : MonoBehaviour
{
    [System.Serializable]
    public class CollisionZone
    {
        public string name = "无碰撞区域";
        public Vector3 center;
        public Vector3 size = Vector3.one;
        public bool enabled = true;

        public Bounds GetBounds()
        {
            return new Bounds(center, size);
        }
    }

    [Header("碰撞区域设置")]
    public CollisionZone[] collisionZones;

    [Header("Tilemap引用")]
    public Tilemap targetTilemap;

    [Header("调试")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(1f, 0.5f, 0.5f, 0.3f);

    private void Start()
    {
        if (targetTilemap == null)
            targetTilemap = GetComponent<Tilemap>();

        ApplyCollisionZones();
    }

    /// <summary>
    /// 应用所有碰撞区域设置
    /// </summary>
    [ContextMenu("应用碰撞区域")]
    public void ApplyCollisionZones()
    {
        if (targetTilemap == null)
        {
            Debug.LogError("未指定目标Tilemap!", this);
            return;
        }

        // 首先重置所有图块的碰撞
        ResetAllTileCollisions();

        // 然后应用无碰撞区域
        foreach (var zone in collisionZones)
        {
            if (zone.enabled)
            {
                DisableCollisionInZone(zone);
            }
        }

        // 更新Tilemap Collider
        UpdateTilemapCollider();

        Debug.Log($"已应用 {collisionZones.Length} 个碰撞区域", this);
    }

    /// <summary>
    /// 在指定区域内禁用碰撞
    /// </summary>
    private void DisableCollisionInZone(CollisionZone zone)
    {
        Bounds worldBounds = zone.GetBounds();

        // 获取Tilemap的边界
        BoundsInt cellBounds = targetTilemap.cellBounds;

        for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
        {
            for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);

                // 将单元格位置转换为世界位置
                Vector3 worldPos = targetTilemap.CellToWorld(tilePos) + targetTilemap.cellSize / 2f;

                // 检查是否在区域内
                if (worldBounds.Contains(worldPos))
                {
                    targetTilemap.SetColliderType(tilePos, Tile.ColliderType.None);
                }
            }
        }
    }

    /// <summary>
    /// 重置所有图块的碰撞
    /// </summary>
    [ContextMenu("重置所有碰撞")]
    public void ResetAllTileCollisions()
    {
        if (targetTilemap == null) return;

        BoundsInt cellBounds = targetTilemap.cellBounds;

        for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
        {
            for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                TileBase tile = targetTilemap.GetTile(tilePos);

                if (tile != null)
                {
                    // 恢复为图块默认的碰撞类型
                    TileData tileData = new TileData();
                    tile.GetTileData(tilePos, targetTilemap, ref tileData);
                    targetTilemap.SetColliderType(tilePos, tileData.colliderType);
                }
                else
                {
                    // 如果图块为空，设置为无碰撞
                    targetTilemap.SetColliderType(tilePos, Tile.ColliderType.None);
                }
            }
        }
    }

    /// <summary>
    /// 更新Tilemap Collider
    /// </summary>
    public void UpdateTilemapCollider()
    {
        TilemapCollider2D collider = targetTilemap.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            collider.ProcessTilemapChanges();
        }
    }

    /// <summary>
    /// 添加新的碰撞区域
    /// </summary>
    public void AddCollisionZone(string name, Vector3 center, Vector3 size)
    {
        CollisionZone newZone = new CollisionZone
        {
            name = name,
            center = center,
            size = size,
            enabled = true
        };

        // 扩展数组
        System.Array.Resize(ref collisionZones, collisionZones.Length + 1);
        collisionZones[collisionZones.Length - 1] = newZone;

        ApplyCollisionZones();
    }

    /// <summary>
    /// 移除碰撞区域
    /// </summary>
    public void RemoveCollisionZone(int index)
    {
        if (index >= 0 && index < collisionZones.Length)
        {
            // 将后面的元素前移
            for (int i = index; i < collisionZones.Length - 1; i++)
            {
                collisionZones[i] = collisionZones[i + 1];
            }

            // 缩小数组
            System.Array.Resize(ref collisionZones, collisionZones.Length - 1);

            ApplyCollisionZones();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 在Scene视图中绘制Gizmos
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!drawGizmos || collisionZones == null) return;

        Gizmos.color = gizmoColor;

        foreach (var zone in collisionZones)
        {
            if (zone.enabled)
            {
                // 绘制区域框
                Gizmos.DrawCube(zone.center, zone.size);

                // 绘制线框
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireCube(zone.center, zone.size);
                Gizmos.color = gizmoColor;

                // 显示区域名称
                UnityEditor.Handles.Label(zone.center, zone.name);
            }
        }
    }

    /// <summary>
    /// 在Inspector中验证
    /// </summary>
    private void OnValidate()
    {
        // 自动获取Tilemap引用
        if (targetTilemap == null)
            targetTilemap = GetComponent<Tilemap>();
    }
#endif
}