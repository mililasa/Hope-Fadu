using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;                 // aca va la nina (Pp)
    public float smoothTime = 0.2f;
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Encuadre fijo")]
    // Con esto activado, siempre ves la misma cantidad de mundo a lo ancho,
    // sin importar el tamano de la ventana. Ojo: en ventanas muy anchas
    // vas a ver MENOS a lo alto (es el precio de fijar el ancho).
    public bool anchoFijo = true;
    public float anchoVisible = 20f;         // en unidades del mundo

    [Header("Limites del nivel")]
    public bool limitarALimites = true;
    public float minX = -20f;
    public float maxX = 20f;
    public float minY = -10f;
    public float maxY = 10f;

    private Vector3 velocity;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (cam == null) cam = GetComponent<Camera>();

        if (anchoFijo && cam.orthographic && cam.aspect > 0f)
            cam.orthographicSize = anchoVisible / (2f * cam.aspect);

        if (target == null) return;

        Vector3 desired = target.position + offset;

        if (limitarALimites && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // Si el nivel es mas chico que lo que entra en pantalla,
            // centramos en vez de forzar un clamp imposible.
            float loX = minX + halfW, hiX = maxX - halfW;
            desired.x = (loX > hiX) ? (minX + maxX) * 0.5f : Mathf.Clamp(desired.x, loX, hiX);

            float loY = minY + halfH, hiY = maxY - halfH;
            desired.y = (loY > hiY) ? (minY + maxY) * 0.5f : Mathf.Clamp(desired.y, loY, hiY);
        }

        desired.z = offset.z;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    // Dibuja el rectangulo de limites en la ventana Scene al seleccionar la camara.
    void OnDrawGizmosSelected()
    {
        if (!limitarALimites) return;
        Gizmos.color = Color.yellow;
        Vector3 centro = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Gizmos.DrawWireCube(centro, new Vector3(maxX - minX, maxY - minY, 0.1f));
    }
}