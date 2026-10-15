using UnityEngine;

// Sube y baja todo el tiempo. La animacion "plataforma" se ve en el sprite.
[RequireComponent(typeof(Rigidbody2D))]
public class PlataformaMovil : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Unidades por segundo.")]
    public float velocidad = 1.5f;
    [Tooltip("Que tanto se aleja para arriba y para abajo desde donde esta ahora.")]
    public float alcance = 2f;
    [Tooltip("Empieza yendo para arriba desde donde esta colocada.")]
    public bool empezarHaciaArriba = true;

    [Header("Animacion")]
    public string estadoAnimacion = "plataforma";

    Rigidbody2D rb;
    Animator animator;
    Vector2 origen;
    float fase;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        animator = GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(estadoAnimacion))
            animator.Play(estadoAnimacion, 0, 0f);
    }

    void Start()
    {
        origen = rb.position;
        fase = 0f;
    }

    void FixedUpdate()
    {
        if (alcance < 0.01f || velocidad < 0.01f) return;

        float omega = velocidad / alcance;
        fase += omega * Time.fixedDeltaTime;
        float dir = empezarHaciaArriba ? 1f : -1f;
        rb.MovePosition(new Vector2(origen.x, origen.y + Mathf.Sin(fase) * alcance * dir));
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = Application.isPlaying ? (Vector3)origen : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(c + Vector3.up * alcance, c + Vector3.down * alcance);
        Gizmos.DrawWireSphere(c + Vector3.up * alcance, 0.12f);
        Gizmos.DrawWireSphere(c + Vector3.down * alcance, 0.12f);
    }
}
