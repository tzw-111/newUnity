using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 主菜单导航 - 处理退出功能
/// </summary>
public class MainMenuNavigator : MonoBehaviour
{
    [Header("主菜单场景")]
    [Tooltip("主菜单场景名称")]
    public string mainMenuScene = "UI Main Menu";

    [Header("过渡动画")]
    public Animator transitionAnimator;
    public float transitionTime = 0.5f;

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMainMenu()
    {
        StartCoroutine(ReturnToMenuCoroutine());
    }

    private IEnumerator ReturnToMenuCoroutine()
    {
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("Start");
            yield return new WaitForSeconds(transitionTime);
        }

        try
        {
            SceneManager.LoadScene(mainMenuScene);
            Debug.Log("返回主菜单");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"返回主菜单失败: {e.Message}");
        }
    }

    /// <summary>
    /// 退出游戏（仅支持打包后的版本）
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        Debug.Log("退出游戏");
    }
}