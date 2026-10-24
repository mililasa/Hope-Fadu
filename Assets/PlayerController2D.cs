using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
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
    FarolCheckpoint farolCheckpoint;

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
    public string idleStateName = "idle";
    [Tooltip("Clip de idle.")]
    [FormerlySerializedAs("animacionEspera")]
    public AnimationClip animacionIdle;
    [Tooltip("Clip de prender farol (prendefarol).")]
    public AnimationClip animacionPrendeFarol;
    public string prendeFarolStateName = "prendefarol";

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
    [Tooltip("Capa de sorting del halo. Piso queda arriba del escenario Default y abajo de Hope.")]
    public string capaHalo = "Piso";
    [Tooltip("Orden dentro de esa capa. 0 queda atras del personaje (suele estar en 1).")]
    public int ordenHalo = 0;

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
    private bool estabaEnPiso = true;
    private bool reproduciendoSalto;
    private Animator animatorHalo;
    private SpriteRenderer srHalo;
    private enum FaseSalto { Nada, Subiendo, Flotando, Bajando }
    private FaseSalto faseSalto = FaseSalto.Nada;
    private float gravedadCaida;
    private float tiempoFloteRestante;
    private bool enIdle;

    public bool PuedeSonarPaso => isGrounded && !enEscalera && !transicionNivel && !movimientoBloqueado &&
        faseSalto == FaseSalto.Nada && Mathf.Abs(horizontalInput) > 0.01f;
    public bool BloqueaFoleyDeSalto => enEscalera || transicionNivel;
    public bool EnSueloParaFoley => isGrounded && faseSalto != FaseSalto.Subiendo;

    HopeJumpLanding foleySalto;
    HopeDistantSequence secuenciaLejana;
    HopeLevelMusic musicaNivel;
    private float moveSpeedOriginal;
    private bool movimientoBloqueado;
    private Coroutine rutinaBloqueo;
    private bool reproduciendoPrendeFarol;
    private float velocidadPrendeFarol = 1f;
    private float prendeFarolHasta;
    // Solo se rearma al caer en el aire (no en Exit): evita re-impulsar a media altura.
    private bool superSaltoListo = true;
    private float superSaltoAlturaPendiente = -1f;

    public bool IsLocked { get { return movimientoBloqueado; } }

    public void MarcarCheckpoint(Transform punto)
    {
        if (punto == null) return;
        checkPoint = punto;
    }

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

        GameObject farol6 = GameObject.Find("Farol (6)");
        if (farol6 != null)
        {
            farolCheckpoint = farol6.GetComponent<FarolCheckpoint>();
            if (farolCheckpoint == null)
                farolCheckpoint = farol6.AddComponent<FarolCheckpoint>();
        }

        gravedadOriginal = rb.gravityScale;
        moveSpeedOriginal = moveSpeed;
        AsegurarEscaleraTrigger();
        AsegurarColliderNivel2();
        AsegurarColliderNivel3();
        AgregarAudio();
        PrepararHaloSalto();
        AplicarClipsDeAnimacion();
        if (TieneAnimacionEspera() && animator != null)
        {
            animator.Play(NombreEstadoEspera(), 0, 0f);
            animator.speed = 1f;
            enIdle = true;
        }
    }

    void AgregarAudio()
    {
        if (!HopeLegacyAudioPolicy.Permitir(this))
        {
            if (gameObject.scene.name == "Nivel_2")
            {
                foleySalto = GetComponent<HopeJumpLanding>();
                if (foleySalto == null) foleySalto = gameObject.AddComponent<HopeJumpLanding>();
            }
            return;
        }
        if (GetComponent<HopeDeathAudio>() == null) gameObject.AddComponent<HopeDeathAudio>();
        secuenciaLejana = GetComponent<HopeDistantSequence>();
        if (secuenciaLejana == null) secuenciaLejana = gameObject.AddComponent<HopeDistantSequence>();
        if (GetComponent<HopeLampRattle>() == null) gameObject.AddComponent<HopeLampRattle>();
        foleySalto = GetComponent<HopeJumpLanding>();
        if (foleySalto == null) foleySalto = gameObject.AddComponent<HopeJumpLanding>();
        if (GetComponent<HopeFootsteps>() == null) gameObject.AddComponent<HopeFootsteps>();
        if (GetComponent<HopeLevelAmbience>() == null) gameObject.AddComponent<HopeLevelAmbience>();
        musicaNivel = GetComponent<HopeLevelMusic>();
        if (musicaNivel == null) musicaNivel = gameObject.AddComponent<HopeLevelMusic>();
    }

    public void SonarMuerte(bool contactoMano)
    {
        HopeDeathAudio audioMuerte = GetComponent<HopeDeathAudio>();
        if (audioMuerte != null) audioMuerte.Reproducir(contactoMano);
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
        // El clip halo_salto pisa el Sprite con frames rotos (Missing).
        // Queda el sprite estatico de la escena (halo_sprite).
        if (animatorHalo != null)
            animatorHalo.enabled = false;
        haloSalto.SetActive(true);
        AplicarCapaHalo();
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
        if (animacionIdle != null)
        {
            ov["idle"] = animacionIdle;
            ov["espera"] = animacionIdle;
            ov["huh"] = animacionIdle;
        }
        if (animacionSalto != null)
            ov["saltar"] = animacionSalto;
        if (animacionPrendeFarol != null)
            ov["prendefarol"] = animacionPrendeFarol;

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
        // La capa se aplica solo al preparar: si se pisa cada frame,
        // cualquier ajuste del sprite en la escena se pierde al dar Play.
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

    void AsegurarColliderNivel3()
    {
        if (SceneManager.GetActiveScene().name != "Nivel_2") return;

        GameObject n3 = GameObject.Find("NIVEL3");
        if (n3 == null) n3 = GameObject.Find("NIVEL 3");
        if (n3 == null) return;

        Collider2D col = n3.GetComponent<Collider2D>();
        if (col == null)
            col = n3.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (transicionNivel) return;

        if (movimientoBloqueado)
        {
            horizontalInput = 0f;
            verticalInput = 0f;
        }
        else
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
        }

        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        bool sobrePadSalto = PlataformaSuperSaltoBajoLosPies() != null;

        // Rearmar fuera del pad: cayendo hacia el próximo, o ya en piso normal.
        // (Si solo se rearma en el aire, al caminar a Plataforma3 (5) nunca vuelve a saltar.)
        if (!sobrePadSalto && (rb.velocity.y < -0.1f || isGrounded))
            superSaltoListo = true;

        // En pad no se recarga el doble salto (el pad da su altura fija sola).
        if (isGrounded && !sobrePadSalto)
            saltosExtraRestantes = dobleSaltoActivado ? 1 : 0;

        if (isGrounded && rb.velocity.y <= 0.05f)
        {
            reproduciendoSalto = false;
            TerminarSaltoFisico();
        }

        if (!estabaEnPiso && isGrounded && !enEscalera && !reproduciendoPrendeFarol)
            ReanudarCaminar();
        estabaEnPiso = isGrounded;

        // En pad: solo rebota la plataforma (altura fija). Afuera: salto / doble salto normal.
        if (!movimientoBloqueado && Input.GetButtonDown("Jump") && !enEscalera && !sobrePadSalto)
        {
            if (isGrounded)
            {
                IniciarSalto(alturaSalto);
            }
            else if (dobleSaltoActivado && saltosExtraRestantes > 0)
            {
                saltosExtraRestantes--;
                IniciarSalto(alturaSalto, false, true);
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
            if (reproduciendoPrendeFarol)
            {
                // Mantener prendefarol aunque otra lógica intente pisarlo.
                AsegurarEstadoPrendeFarol();
                animator.speed = velocidadPrendeFarol;
                if (Time.time >= prendeFarolHasta)
                    TerminarPrendeFarol();
            }
            else if (enEscalera)
            {
                animator.speed = isMoving ? 1f : 0f;
            }
            else if (reproduciendoSalto || (!isGrounded && TieneAnimacionSalto()))
            {
                animator.speed = 1f;
            }
            else
            {
                if (TieneParametroAnimator("Speed"))
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

        if (farolCheckpoint != null)
            farolCheckpoint.RegistrarPaso(transform.position.x);

        if (transform.position.y < alturaDeCaida && !transicionNivel)
        {
            SonarMuerte(false);
            VolverAlCheckPoint();
        }
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
            superSaltoAlturaPendiente = -1f;
        }
        else
        {
            // Aplicar rebote de pad acá (después de física del frame anterior): vy absoluta.
            if (superSaltoAlturaPendiente > 0f)
            {
                AplicarRebotePlataforma(superSaltoAlturaPendiente);
                superSaltoAlturaPendiente = -1f;
            }

            ActualizarFaseSalto();
            if (faseSalto == FaseSalto.Nada && !transicionNivel)
                rb.gravityScale = gravedadOriginal;
            float vx = movimientoBloqueado ? 0f : horizontalInput * moveSpeed;
            rb.velocity = new Vector2(vx, rb.velocity.y);
        }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    public void RestoreMoveSpeed()
    {
        moveSpeed = moveSpeedOriginal;
    }

    public void LockMovement(float duration)
    {
        if (rutinaBloqueo != null)
            StopCoroutine(rutinaBloqueo);
        rutinaBloqueo = StartCoroutine(BloquearMovimiento(duration));
    }

    IEnumerator BloquearMovimiento(float duration)
    {
        movimientoBloqueado = true;
        horizontalInput = 0f;
        if (rb != null)
            rb.velocity = new Vector2(0f, rb.velocity.y);
        float t = Mathf.Max(0f, duration);
        while (t > 0f)
        {
            t -= Time.deltaTime;
            if (rb != null)
                rb.velocity = new Vector2(0f, rb.velocity.y);
            yield return null;
        }
        movimientoBloqueado = false;
        rutinaBloqueo = null;
    }

    public void UnlockMovement()
    {
        if (rutinaBloqueo != null)
        {
            StopCoroutine(rutinaBloqueo);
            rutinaBloqueo = null;
        }
        movimientoBloqueado = false;
        // Si prendefarol sigue activo, lo deja terminar por tiempo (no lo corta acá).
    }

    public void UnlockMovementYSoltarAnim()
    {
        UnlockMovement();
        TerminarPrendeFarol();
    }

    public void ReproducirPrendeFarol(float duracion = 0f)
    {
        AsegurarClipPrendeFarol();
        reproduciendoPrendeFarol = true;
        reproduciendoSalto = false;
        enIdle = false;
        if (animator == null) return;

        animator.enabled = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        float largoClip = LargoClipPrendeFarol();
        if (largoClip <= 0.01f) largoClip = 0.6f;
        if (duracion <= 0.01f) duracion = largoClip;
        velocidadPrendeFarol = largoClip / duracion;
        prendeFarolHasta = Time.time + duracion;

        string estado = NombreEstadoPrendeFarol();
        animator.speed = velocidadPrendeFarol;
        animator.Play(estado, 0, 0f);
        animator.Update(0f);
    }

    void AsegurarClipPrendeFarol()
    {
        if (animacionPrendeFarol != null) return;
#if UNITY_EDITOR
        animacionPrendeFarol = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/player_sprite/prendefarol.anim");
#endif
        if (animacionPrendeFarol != null)
            AplicarClipsDeAnimacion();
    }

    void AsegurarEstadoPrendeFarol()
    {
        if (animator == null) return;
        string estado = NombreEstadoPrendeFarol();
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(estado))
        {
            animator.Play(estado, 0, 0f);
            animator.Update(0f);
        }
    }

    string NombreEstadoPrendeFarol()
    {
        if (!string.IsNullOrEmpty(prendeFarolStateName))
            return prendeFarolStateName;
        return "prendefarol";
    }

    float LargoClipPrendeFarol()
    {
        if (animacionPrendeFarol != null) return animacionPrendeFarol.length;
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null) return 0f;
        string estado = NombreEstadoPrendeFarol();
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null) continue;
            if (clips[i].name == estado ||
                clips[i].name.IndexOf("prende", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return clips[i].length;
        }
        return 0f;
    }

    public void TerminarPrendeFarol()
    {
        if (!reproduciendoPrendeFarol) return;
        reproduciendoPrendeFarol = false;
        prendeFarolHasta = 0f;
        if (animator == null) return;
        animator.speed = 1f;
        if (TieneAnimacionEspera())
        {
            animator.Play(NombreEstadoEspera(), 0, 0f);
            enIdle = true;
        }
    }

    public void ActivarDobleSalto(bool activo)
    {
        dobleSaltoActivado = activo;
        if (isGrounded)
            saltosExtraRestantes = activo ? 1 : 0;
    }

    public void ImpulsarSuperSalto(float alturaAbsoluta)
    {
        saltosExtraRestantes = 0;
        AplicarRebotePlataforma(alturaAbsoluta);
    }

    // Gravedad constante + v = sqrt(2gh): siempre la misma altura, sin fases de salto.
    void AplicarRebotePlataforma(float altura)
    {
        float h = Mathf.Max(0.05f, altura);
        float g = Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.01f, gravedadOriginal);
        float vy = Mathf.Sqrt(2f * g * h);

        saltosExtraRestantes = 0;
        faseSalto = FaseSalto.Nada;
        tiempoFloteRestante = 0f;
        reproduciendoSalto = true;
        rb.gravityScale = gravedadOriginal;
        rb.velocity = new Vector2(rb.velocity.x, vy);
        ReproducirSalto();
        if (foleySalto != null) foleySalto.Saltar();
    }

    // Solo Enter desde arriba. No rearma en Exit (eso re-impulsaba a media altura).
    void IntentarSuperSaltoPorPlataforma(Collision2D collision)
    {
        if (movimientoBloqueado || enEscalera || transicionNivel) return;
        if (!superSaltoListo) return;
        // Bloquear solo si claramente sube (llegar con salto de Hope suele traer vy~0 o negativa).
        if (rb != null && rb.velocity.y > 0.5f) return;

        PlataformaSuperSalto pad = BuscarPlataformaSuperSalto(collision);
        if (pad == null) return;
        if (!ContactoDesdeArriba(collision)) return;

        float h = pad.alturaSalto;
        if (h < 0.05f && pad.multiplicadorAltura > 0.01f)
            h = alturaSalto * pad.multiplicadorAltura;
        if (h < 0.05f) h = 4f;

        superSaltoListo = false;
        superSaltoAlturaPendiente = h;
    }

    static bool ContactoDesdeArriba(Collision2D collision)
    {
        int n = collision.contactCount;
        for (int i = 0; i < n; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
                return true;
        }
        return n == 0;
    }

    PlataformaSuperSalto PlataformaSuperSaltoBajoLosPies()
    {
        if (groundCheck == null) return null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            PlataformaSuperSalto pad = hits[i].GetComponent<PlataformaSuperSalto>();
            if (pad == null) pad = hits[i].GetComponentInParent<PlataformaSuperSalto>();
            if (pad != null) return pad;
        }
        return null;
    }

    static PlataformaSuperSalto BuscarPlataformaSuperSalto(Collision2D collision)
    {
        if (collision.collider != null)
        {
            PlataformaSuperSalto pad = collision.collider.GetComponent<PlataformaSuperSalto>();
            if (pad == null) pad = collision.collider.GetComponentInParent<PlataformaSuperSalto>();
            if (pad != null) return pad;
        }
        if (collision.otherCollider != null)
        {
            PlataformaSuperSalto pad = collision.otherCollider.GetComponent<PlataformaSuperSalto>();
            if (pad == null) pad = collision.otherCollider.GetComponentInParent<PlataformaSuperSalto>();
            if (pad != null) return pad;
        }
        return null;
    }

    void IniciarSalto(float altura, bool desdePlataforma = false, bool dobleSalto = false)
    {
        float h = Mathf.Max(0.05f, altura);
        float tUp = Mathf.Max(0.05f, tiempoHastaElPico);
        float tDown = Mathf.Max(0.05f, tiempoDeCaida);
        float accelSubida = (2f * h) / (tUp * tUp);
        float velocidadInicial = accelSubida * tUp;
        gravedadCaida = (2f * h) / (tDown * tDown);

        rb.gravityScale = EscalaDeGravedad(accelSubida);
        rb.velocity = new Vector2(rb.velocity.x, velocidadInicial);
        faseSalto = FaseSalto.Subiendo;
        tiempoFloteRestante = Mathf.Max(0f, tiempoFlotando);
        reproduciendoSalto = true;
        ReproducirSalto();
        if (foleySalto != null) foleySalto.Saltar(true, dobleSalto);
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
        if (animacionIdle != null)
            return animacionIdle.name;
        return "idle";
    }

    bool TieneParametroAnimator(string nombre)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == nombre)
                return true;
        }
        return false;
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
        if (reproduciendoPrendeFarol) return;
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
        IntentarSuperSaltoPorPlataforma(collision);
        if (EsNivel2(collision.collider))
            PasarANivel2();
        if (EsNivel3(collision.collider))
            PasarANivel3();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (EsSaltador(other))
            ImpulsarResorte();
        if (EsEscalera(other) && !transicionNivel)
            EntrarEscalera(other);
        if (EsNivel2(other))
            PasarANivel2();
        if (EsNivel3(other))
            PasarANivel3();
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

    bool EsNivel3(Collider2D col)
    {
        if (col == null) return false;
        string n = col.gameObject.name.Replace(" ", "");
        return n.Equals("NIVEL3", System.StringComparison.OrdinalIgnoreCase);
    }

    void PasarANivel3()
    {
        if (transicionNivel) return;
        if (SceneManager.GetActiveScene().name == "Nivel_3") return;

        transicionNivel = true;
        if (musicaNivel != null) musicaNivel.CompletarNivel();
        rb.velocity = Vector2.zero;
        string escena = Application.CanStreamedLevelBeLoaded("Nivel_3") ? "Nivel_3" : "Menu";
        HopeMusicTransition.PrepararCambio(gameObject.scene, escena);
        SceneManager.LoadScene(escena);
    }

    void PasarANivel2()
    {
        if (transicionNivel) return;
        transicionNivel = true;
        if (musicaNivel != null) musicaNivel.CompletarNivel();
        rb.velocity = Vector2.zero;
        // Nivel 1 -> Nivel_2 -> Nivel_3 (el faro) -> Menu.
        // Si Nivel_3 todavia no esta en Build Settings, del Nivel_2 va al Menu como antes.
        string actual = SceneManager.GetActiveScene().name;
        string escena;
        if (actual == "Nivel_2")
            escena = Application.CanStreamedLevelBeLoaded("Nivel_3") ? "Nivel_3" : "Menu";
        else if (actual == "Nivel_3")
            escena = "Menu";
        else
            escena = "Nivel_2";
        HopeMusicTransition.PrepararCambio(gameObject.scene, escena);
        SceneManager.LoadScene(escena);
    }

    void SalirEscalera()
    {
        enEscalera = false;
        rb.gravityScale = gravedadOriginal;
        ReanudarCaminar();
    }

    void ReanudarCaminar()
    {
        if (reproduciendoPrendeFarol) return;
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
        Vector3 destino;
        if (checkPoint != null)
            destino = checkPoint.position;
        else if (farolCheckpoint != null && farolCheckpoint.ConsumirParaReaparecer(out Vector3 puntoFarol))
            destino = puntoFarol;
        else
            return;

        if (musicaNivel != null) musicaNivel.ReiniciarNivel();
        if (foleySalto != null) foleySalto.Reiniciar();
        if (secuenciaLejana != null) secuenciaLejana.Reiniciar();

        UnlockMovementYSoltarAnim();
        transform.position = destino;
        rb.velocity = Vector2.zero;
        rb.gravityScale = gravedadOriginal;
        saltosExtraRestantes = dobleSaltoActivado ? 1 : 0;
        escaleraActual = null;
        enEscalera = false;
        estabaEnPiso = true;
        superSaltoListo = true;
        superSaltoAlturaPendiente = -1f;
        ReanudarCaminar();

        // No re-disparar animaciones ya pasadas (cadáver, soldado, caballo, faroles lit).
        foreach (ChaseZone zona in FindObjectsOfType<ChaseZone>(true))
            zona.ReiniciarPorCheckpoint();

        foreach (Lantern farol in FindObjectsOfType<Lantern>(true))
            farol.ResetLantern();

        CadaverChase[] cadaveres = FindObjectsOfType<CadaverChase>(true);
        for (int i = 0; i < cadaveres.Length; i++)
        {
            if (cadaveres[i] != null)
                cadaveres[i].ConservarEstadoEnCheckpoint();
        }

        foreach (SecuenciaSoldadoFarol secuencia in FindObjectsOfType<SecuenciaSoldadoFarol>(true))
            secuencia.ConservarSiYaPaso();

        foreach (CaballoEmbrujado caballo in FindObjectsOfType<CaballoEmbrujado>(true))
            caballo.ConservarSiYaPaso();
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
