using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;
    public float smoothTime = 0.12f;
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Zoom")]
    public float zoom = 5.5f;

    [Header("Limites de los assets")]
    [Tooltip("La vista no sale de este rectangulo: bordes izq/der y piso del arte.")]
    public bool limitarALimites = true;
    public float minX = -10.6f;
    public float maxX = 41f;
    public float minY = -5f;
    public float maxY = 7f;

    Camera cam;
    Vector3 velocity;
    bool yaEncadre;
    Vector3 temblorAplicado;
    float temblorRestante;
    float temblorDuracion;
    float temblorAmplitud;
    float temblorSemilla;

    void Awake()
    {
        cam = GetComponent<Camera>();
        EncadrarYa();
    }

    void LateUpdate()
    {
        if (!yaEncadre)
        {
            EncadrarYa();
            yaEncadre = true;
            return;
        }

        Vector3 desired = PosicionDeseada();
        if (desired.z > 0f) return;

        Vector3 baseActual = transform.position - temblorAplicado;
        Vector3 pos = smoothTime <= 0.0001f
            ? desired
            : Vector3.SmoothDamp(baseActual, desired, ref velocity, smoothTime);
        temblorAplicado = OffsetTemblor();
        transform.position = pos + temblorAplicado;
    }

    // Pequeño temblor de cámara. La persecución lo llama cuando la mano apaga un farol.
    public void Temblar(float duracion = 0.24f, float amplitud = 0.07f)
    {
        if (duracion <= 0f || amplitud <= 0f) return;
        if (temblorRestante <= 0f)
            temblorSemilla = Random.Range(0f, 50f);
        temblorDuracion = Mathf.Max(temblorDuracion, duracion);
        temblorRestante = Mathf.Max(temblorRestante, duracion);
        temblorAmplitud = Mathf.Max(temblorAmplitud, amplitud);
    }

    Vector3 OffsetTemblor()
    {
        if (temblorRestante <= 0f)
            return Vector3.zero;

        float dur = Mathf.Max(0.05f, temblorDuracion);
        float queda = temblorRestante / dur;
        temblorRestante -= Time.deltaTime;
        float t = Time.time * 36f + temblorSemilla;
        float x = (Mathf.PerlinNoise(t, 0.17f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(0.17f, t * 1.3f) - 0.5f) * 2f;
        float amp = temblorAmplitud * Mathf.Clamp01(queda);
        if (temblorRestante <= 0f)
        {
            temblorRestante = 0f;
            temblorDuracion = 0f;
            temblorAmplitud = 0f;
        }
        return new Vector3(x * amp, y * amp * 0.55f, 0f);
    }

    void EncadrarYa()
    {
        Vector3 desired = PosicionDeseada();
        if (desired.z > 0f) return;
        transform.position = desired;
        velocity = Vector3.zero;
    }

    Vector3 PosicionDeseada()
    {
        if (cam == null) cam = GetComponent<Camera>();

        if (target == null)
        {
            GameObject pp = GameObject.Find("Pp");
            if (pp != null) target = pp.transform;
        }

        transform.localScale = Vector3.one;

        if (cam != null && cam.orthographic)
            cam.orthographicSize = zoom < 1f ? 5.5f : zoom;

        if (target == null)
            return new Vector3(0f, 0f, 1f);

        Vector3 desired = target.position + offset;
        desired.z = -10f;

        if (limitarALimites && cam != null && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            desired.x = ClampEje(desired.x, minX, maxX, halfW);
            desired.y = ClampEje(desired.y, minY, maxY, halfH);
        }

        return desired;
    }

    static float ClampEje(float centro, float minMundo, float maxMundo, float mitad)
    {
        float lo = minMundo + mitad;
        float hi = maxMundo - mitad;
        if (hi < lo)
            return (minMundo + maxMundo) * 0.5f;
        return Mathf.Clamp(centro, lo, hi);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 c = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Gizmos.DrawWireCube(c, new Vector3(maxX - minX, maxY - minY, 0f));
    }
}
