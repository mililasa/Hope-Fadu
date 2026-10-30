using UnityEngine;

// Va y vuelve entre puntos. Mientras Hope está parada encima, hereda
// la velocidad de la plataforma. Al saltar (va hacia arriba) la suelta.
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(50)]
public class PlataformaMovilHorizontal : MonoBehaviour
{
    public GameObject platform;

    public float moveSpeed;

    public Transform currentPoint;

    public Transform[] points;

    public int pointSelection;

    Rigidbody2D rb;
    Collider2D superficie;
    PlayerController2D hope;
    Rigidbody2D cuerpoHope;
    bool pisando;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.useFullKinematicContacts = true;
        superficie = GetComponent<Collider2D>();
    }

    void Start()
    {
        hope = FindObjectOfType<PlayerController2D>();
        if (hope != null)
            cuerpoHope = hope.GetComponent<Rigidbody2D>();

        if (points == null || points.Length == 0) return;
        pointSelection = Mathf.Clamp(pointSelection, 0, points.Length - 1);
        currentPoint = points[pointSelection];
    }

    void FixedUpdate()
    {
        if (rb == null || currentPoint == null) return;

        Vector2 origen = rb.position;
        float paso = Mathf.Max(0f, moveSpeed) * Time.fixedDeltaTime;
        Vector2 destino = Vector2.MoveTowards(origen, currentPoint.position, paso);
        Vector2 delta = destino - origen;

        LlevarHope(delta);
        rb.MovePosition(destino);

        if (points == null || points.Length == 0) return;
        if (Vector2.Distance(destino, currentPoint.position) > 0.01f) return;

        pointSelection++;
        if (pointSelection >= points.Length)
            pointSelection = 0;
        currentPoint = points[pointSelection];
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (cuerpoHope == null || collision.rigidbody != cuerpoHope) return;
        if (HopeVieneDeArriba(collision))
            pisando = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (cuerpoHope == null || collision.rigidbody != cuerpoHope) return;
        pisando = false;
    }

    void LlevarHope(Vector2 delta)
    {
        if (cuerpoHope == null || Mathf.Abs(delta.x) < 0.000001f) return;
        if (cuerpoHope.velocity.y > 1f)
        {
            pisando = false;
            return;
        }
        if (!pisando && !HopeEstaParadaAca()) return;

        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;
        cuerpoHope.velocity = new Vector2(
            cuerpoHope.velocity.x + delta.x / dt,
            cuerpoHope.velocity.y);
    }

    bool HopeEstaParadaAca()
    {
        if (hope == null || hope.groundCheck == null || superficie == null) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            hope.groundCheck.position,
            hope.groundCheckRadius,
            hope.groundLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.isTrigger) continue;
            if (hit == superficie || hit.attachedRigidbody == rb)
                return true;
        }
        return false;
    }

    bool HopeVieneDeArriba(Collision2D collision)
    {
        if (superficie == null || collision.collider == null) return false;
        float pies = collision.collider.bounds.min.y;
        float techo = superficie.bounds.max.y;
        return pies >= techo - 0.2f;
    }
}
