using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneAdditive : MonoBehaviour
{
    [Header("场景设置")]
    [Tooltip("要加载的场景名称（必须在Build Settings中）")]
    public string sceneName;
    
    private void Start()
    {
        // 如果挂载在Button上，自动绑定点击事件
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(LoadScene);
        }
    }
    
    /// <summary>
    /// 加载指定场景并卸载当前场景
    /// </summary>
    public void LoadScene()
    {
        LoadScene(sceneName);
    }
    
    /// <summary>
    /// 加载指定场景并卸载当前场景（重载方法，可以传入场景名称）
    /// </summary>
    public void LoadScene(string targetSceneName)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("LoadSceneAdditive: 场景名称为空！");
            return;
        }
        
        // 检查场景是否存在
        if (!SceneExists(targetSceneName))
        {
            Debug.LogError($"LoadSceneAdditive: 场景 {targetSceneName} 不存在或未添加到Build Settings！");
            return;
        }
        
        // 使用Single模式加载场景（会卸载当前场景）
        SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        Debug.Log($"LoadSceneAdditive: 加载场景 {targetSceneName}（替换当前场景）");
    }
    
    /// <summary>
    /// 检查场景是否存在
    /// </summary>
    private bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}