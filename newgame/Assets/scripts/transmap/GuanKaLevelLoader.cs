using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡加载器 - 挂载在UI按钮上
/// </summary>
public class GuanKaLevelLoader : MonoBehaviour
{
    [Header("目标场景")]
    [Tooltip("要加载的场景名称")]
    public string sceneName;

    [Header("过渡动画")]
    [Tooltip("过渡动画控制器")]
    public Animator transitionAnimator;
    [Tooltip("过渡动画时长")]
    public float transitionTime = 0.5f;

    /// <summary>
    /// 供按钮点击事件调用
    /// </summary>
    public void LoadScene()
    {
        StartCoroutine(LoadSceneCoroutine());
    }

    private IEnumerator LoadSceneCoroutine()
    {
        // 播放过渡动画（如果有）
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("Start");
            yield return new WaitForSeconds(transitionTime);
        }

        // 加载场景
        try
        {
            SceneManager.LoadScene(sceneName);
            Debug.Log($"加载场景: {sceneName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"场景加载失败: {e.Message}");
            Debug.LogError($"请检查场景'{sceneName}'是否已添加到Build Settings");
        }
    }
}