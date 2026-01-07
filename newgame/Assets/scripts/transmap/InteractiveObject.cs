using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 交互物体 - 石柱
/// </summary>
public class InteractiveObject : MonoBehaviour
{
    [Header("交互设置")]
    public float interactionRadius = 3f;
    public string playerTag = "Player";

    [Header("UI引用")]
    public GameObject interactionPrompt; // 提示文本UI
    public GameObject levelSelectCanvas; // 选关画布

    private Transform player;
    private bool isPlayerNearby = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) player = playerObj.transform;

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        // 检测距离
        float distance = Vector3.Distance(transform.position, player.position);
        isPlayerNearby = distance <= interactionRadius;

        // 显示/隐藏提示
        if (interactionPrompt != null)
        {
            Text promptText = interactionPrompt.GetComponentInChildren<Text>();
            if (promptText != null) promptText.text = "按 [M] 交互";
            interactionPrompt.SetActive(isPlayerNearby);
        }

        // 检测M键
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.M))
        {
            ShowLevelSelect();
        }
    }

    void ShowLevelSelect()
    {
        if (levelSelectCanvas != null)
        {
            levelSelectCanvas.SetActive(true);
            Debug.Log("显示选关界面");
        }

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    // 可视化交互范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}