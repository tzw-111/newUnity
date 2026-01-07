using UnityEngine;
using UnityEngine.Tilemaps;

namespace Cainos.PixelArtTopDown_Basic
{
    public class StairsLayerTrigger : MonoBehaviour
    {
        [Header("层级设置")]
        [Tooltip("要设置的目标Layer名称（必须在Tags & Layers中定义）")]
        [SerializeField] private string targetLayer = "Default";
        [Tooltip("要设置的目标Sorting Layer名称")]
        [SerializeField] private string targetSortingLayer = "Default";

        [Header("恢复设置")]
        [Tooltip("离开触发器时是否恢复原状")]
        [SerializeField] private bool restoreOnExit = true;
        [Tooltip("恢复时的默认Layer")]
        [SerializeField] private string defaultLayer = "Default";
        [Tooltip("恢复时的默认Sorting Layer")]
        [SerializeField] private string defaultSortingLayer = "Default";

        [Header("目标对象")]
        [Tooltip("只影响特定标签的对象（为空则影响所有）")]
        [SerializeField] private string targetTag = "Player";
        [Tooltip("是否包含子物体")]
        [SerializeField] private bool affectChildren = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag))
            {
                SetObjectLayerAndSorting(other.gameObject, targetLayer, targetSortingLayer);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (restoreOnExit && (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag)))
            {
                SetObjectLayerAndSorting(other.gameObject, defaultLayer, defaultSortingLayer);
            }
        }

        private void SetObjectLayerAndSorting(GameObject obj, string layerName, string sortingLayerName)
        {
            // 保存原始状态（如果还没保存过）
            LayerMemory memory = obj.GetComponent<LayerMemory>();
            if (memory == null)
            {
                memory = obj.AddComponent<LayerMemory>();
                memory.originalLayer = obj.layer;

                // 保存SpriteRenderer的原始状态
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    memory.originalSortingLayer = sr.sortingLayerName;
                    memory.originalSortingOrder = sr.sortingOrder;
                }

                // 保存TilemapRenderer的原始状态
                TilemapRenderer tr = obj.GetComponent<TilemapRenderer>();
                if (tr != null)
                {
                    memory.originalSortingLayer = tr.sortingLayerName;
                    memory.originalSortingOrder = tr.sortingOrder;
                }
            }

            // 设置新Layer（安全版本）
            SetLayerSafe(obj, layerName);

            // 设置Sorting Layer
            SetSortingLayer(obj, sortingLayerName);

            // 递归处理子物体
            if (affectChildren)
            {
                foreach (Transform child in obj.transform)
                {
                    SetObjectLayerAndSorting(child.gameObject, layerName, sortingLayerName);
                }
            }
        }

        private void SetLayerSafe(GameObject obj, string layerName)
        {
            // 检查Layer是否存在
            int newLayer = LayerMask.NameToLayer(layerName);
            if (newLayer != -1)  // NameToLayer返回-1表示Layer不存在
            {
                obj.layer = newLayer;
            }
            else
            {
                Debug.LogWarning($"StairsLayerTrigger: Layer '{layerName}' 不存在。使用默认Layer。", this);

                // 在编辑器模式下给出更多信息
#if UNITY_EDITOR
                Debug.Log($"请在 Unity 编辑器中添加Layer: {layerName}\n" +
                         "步骤: Edit -> Project Settings -> Tags and Layers");
#endif

                // 使用默认Layer
                int defaultLayerId = LayerMask.NameToLayer("Default");
                if (defaultLayerId != -1)
                {
                    obj.layer = defaultLayerId;
                }
            }
        }

        private void SetSortingLayer(GameObject obj, string sortingLayerName)
        {
            // 尝试设置Sorting Layer
            try
            {
                // 为SpriteRenderer设置
                SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sortingLayerName = sortingLayerName;
                }

                // 为TilemapRenderer设置
                TilemapRenderer tilemapRenderer = obj.GetComponent<TilemapRenderer>();
                if (tilemapRenderer != null)
                {
                    tilemapRenderer.sortingLayerName = sortingLayerName;
                }

                // 为其他可能有sorting layer的Renderer设置
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null && renderer is not SpriteRenderer && renderer is not TilemapRenderer)
                {
                    // 使用反射检查是否有sortingLayerName属性
                    var prop = renderer.GetType().GetProperty("sortingLayerName");
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(renderer, sortingLayerName);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"StairsLayerTrigger: 无法设置Sorting Layer '{sortingLayerName}': {e.Message}", this);
            }
        }

        // 辅助方法：检查Layer是否存在
        public static bool DoesLayerExist(string layerName)
        {
            return LayerMask.NameToLayer(layerName) != -1;
        }

        // 辅助方法：检查Sorting Layer是否存在
        public static bool DoesSortingLayerExist(string sortingLayerName)
        {
            foreach (var layer in SortingLayer.layers)
            {
                if (layer.name == sortingLayerName)
                    return true;
            }
            return false;
        }

        // 在编辑器中验证设置
#if UNITY_EDITOR
        private void OnValidate()
        {
            // 检查Layer是否存在
            if (!DoesLayerExist(targetLayer))
            {
                Debug.LogWarning($"目标Layer '{targetLayer}' 不存在!", this);
            }

            if (restoreOnExit && !DoesLayerExist(defaultLayer))
            {
                Debug.LogWarning($"默认Layer '{defaultLayer}' 不存在!", this);
            }

            // 检查Sorting Layer是否存在
            if (!DoesSortingLayerExist(targetSortingLayer))
            {
                Debug.LogWarning($"目标Sorting Layer '{targetSortingLayer}' 不存在!", this);
            }

            if (restoreOnExit && !DoesSortingLayerExist(defaultSortingLayer))
            {
                Debug.LogWarning($"默认Sorting Layer '{defaultSortingLayer}' 不存在!", this);
            }
        }
#endif

        // 辅助类，用于记忆原始层级
        [System.Serializable]
        private class LayerMemory : MonoBehaviour
        {
            [HideInInspector] public int originalLayer;
            [HideInInspector] public string originalSortingLayer;
            [HideInInspector] public int originalSortingOrder;

            // 可选：添加恢复方法
            public void Restore()
            {
                gameObject.layer = originalLayer;

                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingLayerName = originalSortingLayer;
                    sr.sortingOrder = originalSortingOrder;
                }

                TilemapRenderer tr = GetComponent<TilemapRenderer>();
                if (tr != null)
                {
                    tr.sortingLayerName = originalSortingLayer;
                    tr.sortingOrder = originalSortingOrder;
                }
            }
        }
    }
}