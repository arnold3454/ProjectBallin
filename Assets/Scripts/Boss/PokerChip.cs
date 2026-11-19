using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One of the dealer's chips from the Club phase. It is a HUD element, not a board object: a big
/// disc under the Canvas's ChipRain that slides down the screen and hides whatever is behind it,
/// the table and the rest of the HUD alike. Purely cosmetic, so it never blocks clicks or touches
/// the ball. The chips sit switched off under ChipRain in the scene; <see cref="ChipRain"/> switches
/// one on for each drop and the chip switches itself off again once it has fallen off the screen.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PokerChip : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Image tinted with the chip colour: a plain circle. Defaults to the Image on this object.")]
    [SerializeField] private Image face;

    /// <summary>Radius in canvas units (pixels at the canvas's reference resolution).</summary>
    public float Radius { get; private set; }

    /// <summary>True while the chip is on screen.</summary>
    public bool IsFalling => gameObject.activeSelf;

    /// <summary>Centre of the chip, measured from the top-centre of ChipRain.</summary>
    public Vector2 Position => Rect.anchoredPosition;

    private RectTransform rect;
    private float speed;
    private float endY;
    private float spin;

    private RectTransform Rect => rect != null ? rect : rect = (RectTransform)transform;

    /// <summary>
    /// Sizes, colours and shows the chip at <paramref name="start"/>, then lets it fall until its
    /// centre reaches <paramref name="vanishY"/>. Positions are measured from the top-centre of
    /// ChipRain, so y goes negative down the screen.
    /// </summary>
    public void Drop(Vector2 start, float radius, float fallSpeed, float vanishY, Color chipColor, float spinSpeed)
    {
        Radius = Mathf.Max(1f, radius);
        speed = fallSpeed;
        endY = vanishY;
        spin = spinSpeed;

        Rect.sizeDelta = Vector2.one * (Radius * 2f);
        Rect.anchoredPosition = start;
        Rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        if (face == null)
            face = GetComponent<Image>();
        if (face != null)
            face.color = chipColor;

        // The newest chip lands on top of the ones already falling.
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    /// <summary>Takes the chip off the screen and back into the pool.</summary>
    public void Hide()
    {
        speed = 0f;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (speed <= 0f)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        Vector2 position = Rect.anchoredPosition;
        position.y -= speed * Time.deltaTime;
        Rect.anchoredPosition = position;

        if (spin != 0f)
            Rect.Rotate(0f, 0f, spin * Time.deltaTime);

        // Fallen far enough: the chip has done its job and goes back into the pool.
        if (position.y <= endY)
            Hide();
    }
}
