using UnityEngine;

public sealed class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector2 min = new Vector2(-2f, -0.7f);
    public Vector2 max = new Vector2(2f, 0.7f);
    public float damping = 8f;
    public float shakeFrequency = 48f;

    private Vector3 smoothedPosition;
    private bool hasSmoothedPosition;
    private float shakeTimer;
    private float shakeDuration;
    private float shakeStrength;
    private float shakeSeed;

    public void AddShake(float strength, float duration)
    {
        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimer = Mathf.Max(shakeTimer, duration);
        shakeSeed = UnityEngine.Random.Range(0f, 100f);
    }

    private void LateUpdate()
    {
        if (!hasSmoothedPosition)
        {
            smoothedPosition = transform.position;
            hasSmoothedPosition = true;
        }

        if (target != null)
        {
            var desired = new Vector3(
                Mathf.Clamp(target.position.x, min.x, max.x),
                Mathf.Clamp(target.position.y + 0.35f, min.y, max.y),
                transform.position.z);

            smoothedPosition = Vector3.Lerp(smoothedPosition, desired, 1f - Mathf.Exp(-damping * Time.deltaTime));
        }

        transform.position = smoothedPosition + (Vector3)ShakeOffset();
    }

    private Vector2 ShakeOffset()
    {
        if (shakeTimer <= 0f)
        {
            shakeDuration = 0f;
            shakeStrength = 0f;
            return Vector2.zero;
        }

        shakeTimer = Mathf.Max(0f, shakeTimer - Time.unscaledDeltaTime);
        float fade = shakeDuration <= 0f ? 0f : shakeTimer / shakeDuration;
        float time = Time.unscaledTime * shakeFrequency;
        float x = Mathf.PerlinNoise(time, shakeSeed) * 2f - 1f;
        float y = Mathf.PerlinNoise(shakeSeed, time) * 2f - 1f;

        return new Vector2(x, y) * shakeStrength * fade;
    }
}
