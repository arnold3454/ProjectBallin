using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene changes for UI buttons: the title screen's Play and Quit, and the Title Screen buttons
/// on the pause and game over panels. Buttons call these from their On Click, wired in the
/// Inspector with the scene name as the argument.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>Loads a scene from the build list by name.</summary>
    public void LoadScene(string sceneName)
    {
        // Menus pause with the time scale, so never carry a paused clock into the next scene.
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void Quit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
