using UnityEngine;

public sealed class CombatBounds : MonoBehaviour
{
    public Rect playArea = new Rect(-7.5f, -2.35f, 15f, 4.3f);

    public Vector3 Clamp(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, playArea.xMin, playArea.xMax);
        position.y = Mathf.Clamp(position.y, playArea.yMin, playArea.yMax);
        return position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.65f);
        var center = new Vector3(playArea.center.x, playArea.center.y, 0f);
        var size = new Vector3(playArea.width, playArea.height, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
