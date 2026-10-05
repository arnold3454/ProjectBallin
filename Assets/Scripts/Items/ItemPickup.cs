using TMPro;
using UnityEngine;

/// <summary>
/// A pickup on the table that gives the player its item when a ball rolls
/// through it. If every item slot is full, the pickup stays on the table so it
/// can be collected later.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ItemPickup : MonoBehaviour
{
    [Tooltip("Item this pickup gives. Leave empty to pick a random item from the Item Manager's pool.")]
    [SerializeField] private ItemDefinition item;

    [Header("Visuals")]
    [SerializeField] private Transform visual;
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float pulseAmount = 0.1f;
    [SerializeField] private float pulseSpeed = 4f;
    [Tooltip("How brightly the pickup glows in its item's color.")]
    [SerializeField] private float glow = 0.6f;

    [Header("Lifetime")]
    [Tooltip("Seconds before an uncollected pickup disappears. 0 keeps it forever.")]
    [SerializeField] private float lifetime = 20f;
    [Tooltip("Seconds the pickup blinks before it disappears.")]
    [SerializeField] private float blinkTime = 3f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Vector3 visualBaseScale;
    private float age;
    private bool collected;

    public ItemDefinition Item => item;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (visual != null)
            visualBaseScale = visual.localScale;
    }

    private void Start()
    {
        if (item == null && ItemManager.Instance != null)
            item = ItemManager.Instance.GetRandomItem();

        ApplyItemLook();
    }

    public void SetItem(ItemDefinition newItem)
    {
        item = newItem;
        ApplyItemLook();
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (lifetime > 0f)
        {
            float remaining = lifetime - age;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            SetVisible(remaining > blinkTime || Mathf.Repeat(remaining, 0.3f) > 0.15f);
        }

        if (visual != null)
        {
            visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            visual.localScale = visualBaseScale * (1f + pulseAmount * Mathf.Sin(age * pulseSpeed));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || item == null || !other.CompareTag("Ball"))
            return;

        if (ItemManager.Instance == null || !ItemManager.Instance.TryAddItem(item))
            return;

        collected = true;
        Destroy(gameObject);
    }

    private void ApplyItemLook()
    {
        if (item == null)
            return;

        if (visualRenderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            visualRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, item.Color);
            block.SetColor(EmissionColorId, item.Color * glow);
            visualRenderer.SetPropertyBlock(block);
        }

        if (label != null)
        {
            label.text = item.ShortLabel;
            label.color = item.Color;
        }
    }

    private void SetVisible(bool visible)
    {
        if (visualRenderer != null)
            visualRenderer.enabled = visible;

        if (label != null)
            label.enabled = visible;
    }
}
