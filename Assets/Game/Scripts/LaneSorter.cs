using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class LaneSorter : MonoBehaviour
{
    public int baseOrder;
    public int scale = 100;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        spriteRenderer.sortingOrder = baseOrder + Mathf.RoundToInt(-transform.position.y * scale);
    }
}
