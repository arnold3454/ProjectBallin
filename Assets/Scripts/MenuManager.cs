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

    [Header("Flipper Position Adjustments")]
    [SerializeField] private float smallFlipperInset = 2f;
    [SerializeField] private Vector3 smallFlipperOffset = new Vector3(0f, 0f, 0.5f);
    [SerializeField] private Vector3 largeFlipperOffset = new Vector3(0f, 0f, -0.5f);

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

    public void SetSmallMap() => ApplyScale(smallScale, smallCameraZoom, smallFlipperInset, smallFlipperOffset);
    public void SetDefaultMap() => ApplyScale(defaultScale, defaultCameraZoom, 0f, Vector3.zero);
    public void SetLargeMap() => ApplyScale(largeScale, largeCameraZoom, 0f, largeFlipperOffset);

    private void ApplyScale(float mapScale, float cameraScale, float flipperInset, Vector3 flipperOffset)
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
        Vector3 mapOrigin = mapRoot != null ? mapRoot.position : Vector3.zero;
        foreach (FlipperControls flipper in flippers)
        {
            flipper.ScaleMotor(mapScale);
            flipper.ApplyMapPosition(mapScale, mapOrigin, flipperInset, flipperOffset);
        }

        ToggleMenu(); 
    }
}