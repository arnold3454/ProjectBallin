using UnityEngine;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject menuPanel;

    [Header("Game References")]
    [SerializeField] private Transform mapRoot; 
    [SerializeField] private Camera mainCamera; 
    
    [Header("Map Scale Multipliers")]
    [SerializeField] private float smallScale = 0.8f;
    [SerializeField] private float defaultScale = 1f;
    [SerializeField] private float largeScale = 1.2f;

    [Header("Camera Zoom Multipliers")]
    [SerializeField] private float smallCameraZoom = 0.8f;
    [SerializeField] private float defaultCameraZoom = 1f;
    [SerializeField] private float largeCameraZoom = 1.35f; 

    private bool isMenuOpen = false;
    private float baseFieldOfView;
    private float baseOrthographicSize;

    private void Start()
    {
        menuPanel.SetActive(false);
        Time.timeScale = 1f; 

        if (mainCamera != null)
        {
            baseFieldOfView = mainCamera.fieldOfView;
            baseOrthographicSize = mainCamera.orthographicSize;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);
        Time.timeScale = isMenuOpen ? 0f : 1f;
    }

    public void SetSmallMap() => ApplyScale(smallScale, smallCameraZoom);
    public void SetDefaultMap() => ApplyScale(defaultScale, defaultCameraZoom);
    public void SetLargeMap() => ApplyScale(largeScale, largeCameraZoom);

    private void ApplyScale(float mapScale, float cameraScale)
    {
        if (mapRoot != null) 
        {
            mapRoot.localScale = Vector3.one * mapScale;
        }

        if (mainCamera != null)
        {
            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize = baseOrthographicSize * cameraScale;
            }
            else
            {
                mainCamera.fieldOfView = baseFieldOfView * cameraScale;
            }
        }

        FlipperControls[] flippers = FindObjectsByType<FlipperControls>(FindObjectsInactive.Include);
        foreach (FlipperControls flipper in flippers)
        {
            flipper.ScaleMotor(mapScale);
        }

        ToggleMenu(); 
    }
}