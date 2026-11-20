using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Lives in a minigame scene. Its <see cref="GoBack"/> is wired to the minigame's Go Back button
/// and hands play back to the board through the portal that opened the minigame.
/// </summary>
public class MinigameExit : MonoBehaviour
{
    [Tooltip("Board scene to load when this minigame was opened on its own (pressing Play with only this scene open), so there's no portal waiting for it.")]
    [SerializeField] private string fallbackBoardScene = "PortalTest";

    [Tooltip("This scene's own EventSystem, used when the scene is played on its own. Turned off when the board's EventSystem is already running underneath.")]
    [SerializeField] private EventSystem localEventSystem;

    private void Start()
    {
        // Loaded on top of the board, the board's EventSystem already drives every canvas.
        if (localEventSystem != null && EventSystem.current != null && EventSystem.current != localEventSystem)
            localEventSystem.gameObject.SetActive(false);
    }

    /// <summary>Closes the minigame and returns to the board. Hooked up to the Go Back button.</summary>
    public void GoBack()
    {
        if (MinigamePortal.ReturnToBoard())
            return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(fallbackBoardScene);
    }
}
