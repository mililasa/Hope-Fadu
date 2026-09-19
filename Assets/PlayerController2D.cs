using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 4f;

    [Header("Tiempo de salto")]
    [Tooltip("Que tan alto llega, en unidades del mundo.")]
    public float alturaSalto = 1.8f;
    [Tooltip("Segundos desde que se lanza hasta el punto mas alto.")]
    public float tiempoHastaElPico = 0.35f;
    [Tooltip("Segundos que se queda arriba, casi quieta, antes de caer.")]
    public float tiempoFlotando = 0.05f;
    [Tooltip("Segundos desde el pico hasta volver a la altura de salida.")]
    public float tiempoDeCaida = 0.4f;

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
    [Tooltip("Nombre del estado de caminar en el Animator. Podes cambiarlo.")]
    public string runStateName = "caminar";
    [Tooltip("Arrastra aca otro clip para reemplazar la caminata. El estado se tiene que llamar igual (caminar), o cambia Run State Name.")]
    public AnimationClip animacionCaminar;
    public string climbStateName = "ppescalera";

    [Header("Animacion idle")]
    [Tooltip("Nombre del estado de idle en el Animator.")]
    public string idleStateName = "espera";
    [Tooltip("Clip de idle (espera).")]
    public AnimationClip animacionEspera;

    [Header("Animacion salto")]
    [Tooltip("Arrastra aca el clip de salto. En el Animator crea un estado con el MISMO nombre que el clip.")]
    public AnimationClip animacionSalto;
    [Tooltip("Si el estado en el Animator tiene otro nombre, escribil o aca. Si esta vacio, usa el nombre del clip.")]
    public string jumpStateName = "saltar";
    [Tooltip("1 = velocidad normal. 0.5 = la mitad de rapida. 0.3 = bien lenta.")]
    public float velocidadAnimacionSalto = 0.55f;
    [Tooltip("Objeto hijo del halo (halo_de_saltar-Sheet_0). Si esta vacio, lo busca solo.")]
    public GameObject haloSalto;
    [Range(0f, 1f)]
    [Tooltip("0 = invisible, 1 = opaco.")]
    public float opacidadHalo = 1f;
    [Tooltip("Capa de sorting del halo. Default queda atras del Foreground.")]
    public string capaHalo = "Default";
    [Tooltip("Orden dentro de esa capa. Mas bajo = mas atras.")]
    public int ordenHalo = -1;

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
    private bool reproduciendoSalto;
    private Animator animatorHalo;
    private SpriteRenderer srHalo;
    private bool tieneParametroSpeed;
    private enum FaseSalto { Nada, Subiendo, Flotando, Bajando }
    private FaseSalto faseSalto = FaseSalto.Nada;
    private float gravedadCaida;
    private float tiempoFloteRestante;
    private bool enIdle;

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
        PrepararAnimator();
        PrepararHaloSalto();
        AplicarClipsDeAnimacion();
        if (TieneAnimacionEspera() && animator != null)
        {
            animator.Play(NombreEstadoEspera(), 0, 0f);
            animator.speed = 1f;
            enIdle = true;
        }
    }

    void PrepararAnimator()
    {
        if (animator == null) return;
        tieneParametroSpeed = false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == "Speed")
            {
                tieneParametroSpeed = true;
                break;
            }
        }
    }

    void PrepararHaloSalto()
    {
        if (haloSalto == null)
        {
            Transform t = transform.Find("halo_de_saltar-Sheet_0");
            if (t != null) haloSalto = t.gameObject;
        }

        if (haloSalto == null) return;
        animatorHalo = haloSalto.GetComponent<Animator>();
        srHalo = haloSalto.GetComponent<SpriteRenderer>();
        haloSalto.SetActive(true);
        AplicarCapaHalo();
        if (animatorHalo != null)
            animatorHalo.Play("halo_salto", 0, 0f);
    }

    void AplicarCapaHalo()
    {
        if (srHalo == null) return;
        srHalo.sortingLayerName = capaHalo;
        srHalo.sortingOrder = ordenHalo;
    }

    void AplicarClipsDeAnimacion()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        RuntimeAnimatorController original = animator.runtimeAnimatorController;
        AnimatorOverrideController ov = original as AnimatorOverrideController;
        if (ov == null)
            ov = new AnimatorOverrideController(original);

        if (animacionCaminar != null)
            ov["caminar"] = animacionCaminar;
        if (animacionEspera != null)
        {
            ov["espera"] = animacionEspera;
            ov["huh"] = animacionEspera;
        }
        if (animacionSalto != null)
            ov["saltar"] = animacionSalto;

        animator.runtimeAnimatorController = ov;
    }

    void AplicarOpacidadHalo()
    {
        if (srHalo == null) return;
        Color c = srHalo.color;
        c.a = Mathf.Clamp01(opacidadHalo);
        srHalo.color = c;
    }

    void LateUpdate()
    {
        AplicarOpacidadHalo();
        AplicarCapaHalo();
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

        if (isGrounded && rb.velocity.y <= 0.05f)
        {
            reproduciendoSalto = false;
            TerminarSaltoFisico();
        }

        if (!estabaEnPiso && isGrounded && !enEscalera)
            ReanudarCaminar();
        estabaEnPiso = isGrounded;

        if (Input.GetButtonDown("Jump") && !enEscalera)
        {
            if (isGrounded)
            {
                IniciarSalto(alturaSalto);
            }
            else if (dobleSaltoActivado && saltosExtraRestantes > 0)
            {
                saltosExtraRestantes--;
                IniciarSalto(alturaSalto);
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
            else if (reproduciendoSalto || (!isGrounded && TieneAnimacionSalto()))
            {
                animator.speed = 1f;
            }
            else
            {
                if (tieneParametroSpeed)
                    animator.SetFloat("Speed", Mathf.Abs(horizontalInput));

                if (isMoving)
                {
                    if (!wasMoving || enIdle)
                        animator.Play(NombreEstadoCaminar(), 0, 0f);
                    animator.speed = 1f;
                    enIdle = false;
                }
                else
                {
                    if (TieneAnimacionEspera())
                    {
                        if (!enIdle)
                        {
                            animator.Play(NombreEstadoEspera(), 0, 0f);
                            enIdle = true;
                        }
                        animator.speed = 1f;
                    }
                    else
                    {
                        if (wasMoving)
                            animator.Play(NombreEstadoCaminar(), 0, 0f);
                        animator.speed = 0f;
                    }
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
            faseSalto = FaseSalto.Nada;
        }
        else
        {
            ActualizarFaseSalto();
            if (faseSalto == FaseSalto.Nada && !transicionNivel)
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

    void IniciarSalto(float altura)
    {
        float h = Mathf.Max(0.05f, altura);
        float tUp = Mathf.Max(0.05f, tiempoHastaElPico);
        float tDown = Mathf.Max(0.05f, tiempoDeCaida);
        float accelSubida = (2f * h) / (tUp * tUp);
        float velocidadInicial = accelSubida * tUp;
        gravedadCaida = (2f * h) / (tDown * tDown);

        rb.velocity = new Vector2(rb.velocity.x, velocidadInicial);
        rb.gravityScale = EscalaDeGravedad(accelSubida);
        faseSalto = FaseSalto.Subiendo;
        tiempoFloteRestante = Mathf.Max(0f, tiempoFlotando);
        reproduciendoSalto = true;
        ReproducirSalto();
    }

    float EscalaDeGravedad(float aceleracion)
    {
        float gMundo = Mathf.Abs(Physics2D.gravity.y);
        if (gMundo < 0.01f) return gravedadOriginal;
        return aceleracion / gMundo;
    }

    void ActualizarFaseSalto()
    {
        if (faseSalto == FaseSalto.Nada || transicionNivel) return;

        if (faseSalto == FaseSalto.Subiendo && rb.velocity.y <= 0f)
        {
            if (tiempoFloteRestante > 0f)
            {
                faseSalto = FaseSalto.Flotando;
                rb.gravityScale = 0f;
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }
            else
            {
                EmpezarCaida();
            }
        }
        else if (faseSalto == FaseSalto.Flotando)
        {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            tiempoFloteRestante -= Time.fixedDeltaTime;
            if (tiempoFloteRestante <= 0f)
                EmpezarCaida();
        }
    }

    void EmpezarCaida()
    {
        faseSalto = FaseSalto.Bajando;
        rb.gravityScale = EscalaDeGravedad(gravedadCaida);
    }

    void TerminarSaltoFisico()
    {
        if (faseSalto == FaseSalto.Nada) return;
        faseSalto = FaseSalto.Nada;
        rb.gravityScale = gravedadOriginal;
    }

    bool TieneAnimacionSalto()
    {
        return !string.IsNullOrEmpty(NombreEstadoSalto());
    }

    string NombreEstadoCaminar()
    {
        if (!string.IsNullOrEmpty(runStateName))
            return runStateName;
        if (animacionCaminar != null)
            return animacionCaminar.name;
        return "caminar";
    }

    bool TieneAnimacionEspera()
    {
        return !string.IsNullOrEmpty(NombreEstadoEspera());
    }

    string NombreEstadoEspera()
    {
        if (!string.IsNullOrEmpty(idleStateName))
            return idleStateName;
        if (animacionEspera != null)
            return animacionEspera.name;
        return "";
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
        if (animator != null && TieneAnimacionSalto())
        {
            animator.Play(NombreEstadoSalto(), 0, 0f);
            animator.speed = 1f;
        }
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
        TerminarSaltoFisico();
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
        reproduciendoSalto = false;
        TerminarSaltoFisico();
        if (animator == null) return;
        if (TieneAnimacionEspera())
        {
            animator.Play(NombreEstadoEspera(), 0, 0f);
            animator.speed = 1f;
            enIdle = true;
        }
        else
        {
            animator.Play(NombreEstadoCaminar(), 0, 0f);
            animator.speed = 0f;
            enIdle = false;
        }
        wasMoving = false;
    }

    bool EsSaltador(Collider2D col)
    {
        if (col == null || string.IsNullOrEmpty(nombreSaltador)) return false;
        return col.gameObject.name.Equals(nombreSaltador, System.StringComparison.OrdinalIgnoreCase);
    }

    void ImpulsarResorte()
    {
        float extra = Mathf.Max(1.5f, fuerzaResorte / 10f);
        IniciarSalto(alturaSalto * extra);
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
