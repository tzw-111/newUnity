using UnityEngine;
using System.Collections;

// 定义命中事件委托
public delegate void OnAttackHitDelegate(Collider2D hitTarget);

[RequireComponent(typeof(Collider2D))]
public class FlyingEyeAttackTrigger : MonoBehaviour
{
    [Header("可视化设置（编辑模式辅助，不影响运行时）")]
    public bool showInSceneView = true;
    public Color gizmoColor = new Color(1, 0, 0, 0.5f);

    [Header("目标检测设置")]
    public string targetTag = "Player";

    private FlyingEyeController parentController;
    private Collider2D attackCollider;

    public event OnAttackHitDelegate OnAttackHit;

    private void Awake()
    {
        parentController = GetComponentInParent<FlyingEyeController>();
        attackCollider = GetComponent<Collider2D>();

        if (parentController == null)
        {
            Debug.LogError("未找到父对象的FlyingEyeController！");
        }

        attackCollider.isTrigger = true;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 核心：OnEnable时立即扫描已存在的碰撞体
    /// 解决玩家在攻击框内无法受伤的问题
    /// </summary>
    private void OnEnable()
    {
        StartCoroutine(DetectObjectsAlreadyInside());
    }

    /// <summary>
    /// 检测攻击框激活时已经存在的碰撞体
    /// </summary>
    private IEnumerator DetectObjectsAlreadyInside()
    {
        yield return new WaitForFixedUpdate();

        Collider2D[] colliders = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D().NoFilter();
        int hitCount = attackCollider.OverlapCollider(filter, colliders);

        Debug.Log($"[主动扫描] 攻击框激活时已存在 {hitCount} 个碰撞体");

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = colliders[i];
            // 仅保留有效过滤：检测目标Tag
            if (col != null && col.CompareTag(targetTag))
            {
                Debug.Log($"[主动扫描] 发现已在框内的玩家: {col.name}");
                OnAttackHit?.Invoke(col);
                yield break; // 击中后立即停止，防止同一帧多次触发
            }
        }
    }

    /// <summary>
    /// 处理新进入的物体
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 仅保留有效逻辑：检测目标Tag
        if (collision.CompareTag(targetTag))
        {
            Debug.Log($"[新进入] 攻击判定框命中 {collision.name}");
            OnAttackHit?.Invoke(collision);
        }
    }

    #region Gizmos可视化（编辑模式辅助，可保留）
    private void OnDrawGizmos()
    {
        if (attackCollider == null || !showInSceneView) return;

        Gizmos.color = gizmoColor;

        if (attackCollider is CircleCollider2D circleCollider)
        {
            Gizmos.DrawSphere(circleCollider.bounds.center, circleCollider.radius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(circleCollider.bounds.center, circleCollider.radius);
        }
        else if (attackCollider is BoxCollider2D boxCollider)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(boxCollider.bounds.center, transform.rotation, Vector3.one);
            Gizmos.matrix = matrix;
            Gizmos.DrawCube(Vector3.zero, boxCollider.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, boxCollider.size);
        }
        else if (attackCollider is PolygonCollider2D polygonCollider)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector2[] points = polygonCollider.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 nextPoint = points[(i + 1) % points.Length];
                Gizmos.DrawLine(points[i], nextPoint);
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
    #endregion
}