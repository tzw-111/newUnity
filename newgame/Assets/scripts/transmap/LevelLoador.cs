using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelPortal : MonoBehaviour
{
    [Header("传送配置")]
    [Tooltip("触发物体标签（玩家）")]
    public string triggerTag = "Player";

    [Tooltip("目标场景名称")]
    public string targetScene = "SC Demo";

    [Tooltip("传送延迟时间")]
    public float delayTime = 1f;

    [Header("过渡动画")]
    [Tooltip("过渡动画控制器（可选）")]
    public Animator transitionAnimator;
    public float transitionDuration = 0.5f;

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 仅当玩家碰撞且未触发过才执行
        if (other.CompareTag(triggerTag) && !isTriggered)
        {
            isTriggered = true;
            Debug.Log($"检测到玩家，{delayTime}秒后传送到 {targetScene}");
            StartCoroutine(TransferCoroutine());
        }
    }

    private IEnumerator TransferCoroutine()
    {
        // 等待延迟时间
        yield return new WaitForSeconds(delayTime);

        // 播放过渡动画（如果有）
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("Start");
            yield return new WaitForSeconds(transitionDuration);
        }

        // 加载目标场景
        try
        {
            SceneManager.LoadScene(targetScene);
            Debug.Log($"成功加载场景: {targetScene}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"场景加载失败: {e.Message}");
            Debug.LogError($"请检查场景'{targetScene}'是否已添加到Build Settings");
            isTriggered = false; // 允许重新触发
        }
    }

    // 防止玩家离开传送门后取消传送（可选功能）
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag) && !isTriggered)
        {
            // 如果希望在离开传送区域时取消传送，可在此实现
            // StopCoroutine(TransferCoroutine());
            // isTriggered = false;
        }
    }
}