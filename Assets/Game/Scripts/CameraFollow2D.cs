using UnityEngine;

public sealed class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector2 min = new Vector2(-2f, -0.7f);
    public Vector2 max = new Vector2(2f, 0.7f);
    public float damping = 8f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        var desired = new Vector3(
            Mathf.Clamp(target.position.x, min.x, max.x),
            Mathf.Clamp(target.position.y + 0.35f, min.y, max.y),
            transform.position.z);

        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-damping * Time.deltaTime));
    }
}
