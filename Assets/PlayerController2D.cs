using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 4f;
    public float jumpForce = 4f;

    [Header("Doble salto")]
    // Tildalo en el Inspector para poder saltar una vez mas en el aire.
    public bool dobleSaltoActivado = true;

    [Header("Resorte (Saltador)")]
    public float fuerzaResorte = 20f;
    public string nombreSaltador = "Saltador";

    [Header("Check point")]
    public Transform checkPoint;
    public float alturaDeCaida = -7f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Sprite")]
    // Con el sheet nuevo (player.png, fila 5) el dibujo mira a la DERECHA:
    // dejalo TILDADO. Si lo ves al reves, destildalo.
    public bool spriteMiraALaDerecha = true;

    [Header("Animacion")]
    public string runStateName = "Run_Player";
    public string climbStateName = "ppescalera";

    [Header("Animacion salto")]
    [Tooltip("Arrastra aca el clip de salto. En el Animator crea un estado con el MISMO nombre que el clip.")]
    public AnimationClip animacionSalto;
    [Tooltip("Si el estado en el Animator tiene otro nombre, escribil o aca. Si esta vacio, usa el nombre del clip.")]
    public string jumpStateName = "";

    [Header("Escalera")]
    public float climbSpeed = 3f;
    public string nombreEscalera = "Escalera";

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private bool isGrounded;
    private float horizontalInput;
    private float verticalInput;
    private bool wasMoving;
    private int saltosExtraRestantes;
    private Collider2D escaleraActual;
    private bool enEscalera;
    private float gravedadOriginal = 3f;
    private bool transicionNivel;
    private GameObject pantallaNivel2;
    private bool estabaEnPiso = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Ahora giramos con la escala del objeto (para que los hijos,
        // como la linterna, giren tambien). Anulamos el flipX viejo.
        if (sr != null) sr.flipX = false;

        if (checkPoint == null)
        {
            GameObject cp = GameObject.Find("CheckPoint");
            if (cp != null) checkPoint = cp.transform;
        }

        gravedadOriginal = rb.gravityScale;
        AsegurarEscaleraTrigger();
        AsegurarColliderNivel2();
        CrearPantallaNivel2();
    }

    void AsegurarEscaleraTrigger()
    {
        GameObject e = GameObject.Find("Escalera");
        if (e == null) return;
        BoxCollider2D box = e.GetComponent<BoxCollider2D>();
        if (box != null) box.isTrigger = true;
    }

    void AsegurarColliderNivel2()
    {
        GameObject n2 = GameObject.Find("NIVEL2");
        if (n2 == null) n2 = GameObject.Find("NIVEL 2");
        if (n2 == null) return;

        Collider2D col = n2.GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D box = n2.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
        }
    }

    void Update()
    {
        if (transicionNivel) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
            saltosExtraRestantes = dobleSaltoActivado ? 1 : 0;

        if (!estabaEnPiso && isGrounded && !enEscalera)
            ReanudarCaminar();
        estabaEnPiso = isGrounded;

        if (Input.GetButtonDown("Jump") && !enEscalera)
        {
            if (isGrounded)
            {
                Saltar(jumpForce);
            }
            else if (dobleSaltoActivado && saltosExtraRestantes > 0)
            {
                saltosExtraRestantes--;
                Saltar(jumpForce);
            }
        }

        bool isMoving = enEscalera
            ? Mathf.Abs(verticalInput) > 0.01f
            : Mathf.Abs(horizontalInput) > 0.01f;

        if (!enEscalera && Mathf.Abs(horizontalInput) > 0.01f)
        {
            float baseDir = spriteMiraALaDerecha ? 1f : -1f;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (horizontalInput > 0f ? baseDir : -baseDir);
            transform.localScale = s;
        }

        if (animator != null)
        {
            if (enEscalera)
            {
                animator.speed = isMoving ? 1f : 0f;
            }
            else if (!isGrounded && TieneAnimacionSalto())
            {
                // No pisar el clip de salto con la caminata mientras esta en el aire.
                animator.speed = 1f;
            }
            else
            {
                animator.SetFloat("Speed", Mathf.Abs(horizontalInput));

                if (isMoving)
                {
                    animator.speed = 1f;
                }
                else
                {
                    if (wasMoving) animator.Play(runStateName, 0, 0f);
                    animator.speed = 0f;
                }
            }
        }

        wasMoving = isMoving;

        if (transform.position.y < alturaDeCaida && !transicionNivel)
            VolverAlCheckPoint();
    }

    void FixedUpdate()
    {
        if (transicionNivel)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (enEscalera && escaleraActual != null)
        {
            Vector2 pos = rb.position;
            pos.x = escaleraActual.bounds.center.x;
            rb.position = pos;
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(0f, verticalInput * climbSpeed);
        }
        else
        {
            if (!transicionNivel)
                rb.gravityScale = gravedadOriginal;
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
        }
    }

    public void ActivarDobleSalto(bool activo)
    {
        dobleSaltoActivado = activo;
        if (isGrounded)
            saltosExtraRestantes = activo ? 1 : 0;
    }

    void Saltar(float fuerza)
    {
        rb.velocity = new Vector2(rb.velocity.x, fuerza);
        ReproducirSalto();
    }

    bool TieneAnimacionSalto()
    {
        return !string.IsNullOrEmpty(NombreEstadoSalto());
    }

    string NombreEstadoSalto()
    {
        if (!string.IsNullOrEmpty(jumpStateName))
            return jumpStateName;
        if (animacionSalto != null)
            return animacionSalto.name;
        return "";
    }

    void ReproducirSalto()
    {
        if (animator == null || !TieneAnimacionSalto()) return;
        animator.Play(NombreEstadoSalto(), 0, 0f);
        animator.speed = 1f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (EsSaltador(collision.collider))
            ImpulsarResorte();
        if (EsNivel2(collision.collider))
            StartCoroutine(PasarANivel2());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (EsSaltador(other))
            ImpulsarResorte();
        if (EsEscalera(other) && !transicionNivel)
            EntrarEscalera(other);
        if (EsNivel2(other))
            StartCoroutine(PasarANivel2());
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (EsEscalera(other) && !transicionNivel)
            EntrarEscalera(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!EsEscalera(other)) return;
        if (escaleraActual == other)
            escaleraActual = null;
        SalirEscalera();
    }

    bool EsEscalera(Collider2D col)
    {
        if (col == null) return false;
        return col.gameObject.name.IndexOf("escalera", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void EntrarEscalera(Collider2D col)
    {
        escaleraActual = col;
        if (enEscalera) return;

        enEscalera = true;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(0f, 0f);
        if (animator != null)
        {
            animator.Play(climbStateName, 0, 0f);
            animator.speed = 1f;
        }
    }

    bool EsNivel2(Collider2D col)
    {
        if (col == null) return false;
        string n = col.gameObject.name.Replace(" ", "");
        return n.Equals("NIVEL2", System.StringComparison.OrdinalIgnoreCase);
    }

    IEnumerator PasarANivel2()
    {
        if (transicionNivel) yield break;
        transicionNivel = true;
        SalirEscalera();
        rb.velocity = Vector2.zero;
        rb.gravityScale = gravedadOriginal;
        ReanudarCaminar();

        if (pantallaNivel2 != null)
            pantallaNivel2.SetActive(true);

        yield return new WaitForSeconds(2f);

        if (pantallaNivel2 != null)
            pantallaNivel2.SetActive(false);

        rb.gravityScale = gravedadOriginal;
        rb.velocity = Vector2.zero;
        transicionNivel = false;
        VolverAlCheckPoint();
    }

    void CrearPantallaNivel2()
    {
        pantallaNivel2 = new GameObject("PantallaNivel2");
        Canvas canvas = pantallaNivel2.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        pantallaNivel2.AddComponent<CanvasScaler>();

        GameObject fondoGO = new GameObject("Fondo");
        fondoGO.transform.SetParent(pantallaNivel2.transform, false);
        Image fondo = fondoGO.AddComponent<Image>();
        fondo.color = Color.black;
        RectTransform fondoRt = fondo.rectTransform;
        fondoRt.anchorMin = Vector2.zero;
        fondoRt.anchorMax = Vector2.one;
        fondoRt.offsetMin = Vector2.zero;
        fondoRt.offsetMax = Vector2.zero;

        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(pantallaNivel2.transform, false);
        Text texto = textoGO.AddComponent<Text>();
        texto.text = "NIVEL2";
        texto.fontSize = 72;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        texto.font = font;
        RectTransform textoRt = texto.rectTransform;
        textoRt.anchorMin = Vector2.zero;
        textoRt.anchorMax = Vector2.one;
        textoRt.offsetMin = Vector2.zero;
        textoRt.offsetMax = Vector2.zero;

        pantallaNivel2.SetActive(false);
    }

    void SalirEscalera()
    {
        enEscalera = false;
        rb.gravityScale = gravedadOriginal;
        ReanudarCaminar();
    }

    void ReanudarCaminar()
    {
        if (animator == null) return;
        animator.Play(runStateName, 0, 0f);
        animator.speed = 0f;
        wasMoving = false;
    }

    bool EsSaltador(Collider2D col)
    {
        if (col == null || string.IsNullOrEmpty(nombreSaltador)) return false;
        return col.gameObject.name.Equals(nombreSaltador, System.StringComparison.OrdinalIgnoreCase);
    }

    void ImpulsarResorte()
    {
        Saltar(fuerzaResorte);
        if (dobleSaltoActivado)
            saltosExtraRestantes = 1;
    }

    public void VolverAlCheckPoint()
    {
        if (checkPoint == null) return;

        transform.position = checkPoint.position;
        rb.velocity = Vector2.zero;
        rb.gravityScale = gravedadOriginal;
        saltosExtraRestantes = dobleSaltoActivado ? 1 : 0;
        escaleraActual = null;
        enEscalera = false;
        estabaEnPiso = true;
        ReanudarCaminar();

        LuzFondoInteractiva luces = GetComponent<LuzFondoInteractiva>();
        if (luces == null) luces = FindObjectOfType<LuzFondoInteractiva>();
        if (luces != null) luces.Reiniciar();

        HandChaser mano = FindObjectOfType<HandChaser>();
        if (mano != null) mano.Reiniciar();
    }

    // Verde = esta pisando piso (puede saltar). Rojo = en el aire.
    // Seleccionar el Pp con el juego corriendo para verlo en la ventana Scene.
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
