using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Mano fantasma de las zonas de persecucion.
[RequireComponent(typeof(SpriteRenderer))]
public class HandChaser : MonoBehaviour
{
    public enum Estado { Inactive, Chasing, Distracted, Leaving }

    [SerializeField] [Range(0f, 1f)] float alphaFantasma = 0.85f;
    [SerializeField] float leaveDuration = 1.2f;
    [SerializeField] float leaveSpeed = 8f;
    [SerializeField] string sortingLayer = "Foreground";
    [SerializeField] int sortingOrder = 5;
    [SerializeField] string estadoPersigue = "Persigue";
    [SerializeField] string estadoApaga = "ApagaVela";
    [SerializeField] float arriveDistance = 0.4f;
    [Tooltip("Solo el primer farol: segundos que la mano se queda quieta cuando se prende, para que se vea la luz. Los demas los va a apagar enseguida.")]
    [SerializeField] float segundosLuzAntesDeIr = 2f;
    [Tooltip("Si Hope no llega al primer farol en estos segundos, la mano deja de ir lenta y empieza a perseguirla en serio.")]
    [SerializeField] float segundosMaxSinPrimerFarol = 6f;
    [Tooltip("Altura de las puntas de los dedos sobre los pies de Hope mientras la persigue (dentro de su halo).")]
    [SerializeField] float alturaDedos = 1.3f;
    [Tooltip("Altura de las puntas de los dedos sobre los pies de Hope cuando esta por agarrarla.")]
    [SerializeField] float alturaDedosAlAtrapar = 0.5f;
    [Tooltip("Inclinacion de la mano en grados. Se espeja sola cuando la mano mira para el otro lado.")]
    [SerializeField] [Range(-45f, 45f)] float rotacion = 0f;
    [Tooltip("Controller con los estados Persigue y ApagaVela (Assets/mano/ManoFantasma.controller).")]
    [SerializeField] RuntimeAnimatorController controllerMano;

    SpriteRenderer sr;
    Animator animator;
    Transform player;
    ChaseZone zone;
    Estado estado = Estado.Inactive;
    float currentSpeed;
    readonly List<Lantern> farolesPrendidos = new List<Lantern>();
    Lantern objetivoActual;
    bool extinguishPedido;
    Color colorBase = Color.white;

    Collider2D colMano;
    bool yaAgarro;
    bool yaPasoPrimera;
    Lantern farolEncendiendo;
    float tiempoEnPrimera;
    float tiempoSinPasarPrimera;
    readonly HashSet<Lantern> farolesUsados = new HashSet<Lantern>();
    readonly HashSet<Lantern> farolesSaltados = new HashSet<Lantern>();
    readonly Dictionary<Lantern, float> momentoPrendido = new Dictionary<Lantern, float>();

    public Estado EstadoActual { get { return estado; } }
    public bool PersecucionAudible => estado == Estado.Chasing || estado == Estado.Distracted;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (animator == null && controllerMano != null)
            animator = gameObject.AddComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController == null && controllerMano != null)
            animator.runtimeAnimatorController = controllerMano;
        if (animator == null)
            Debug.LogWarning("ManoFantasma no tiene Animator: no se van a ver Persigue ni ApagaVela.", this);
        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        AsegurarToqueChico();
        if (GetComponent<HopeHandAudio>() == null) gameObject.AddComponent<HopeHandAudio>();
        if (sr != null)
        {
            colorBase = sr.color;
            if (sortingLayer == "Oscuridad")
            {
                sortingLayer = "Foreground";
                sortingOrder = 5;
            }
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            AplicarAlpha(alphaFantasma);
        }
        GoInactive();
    }

    void AsegurarToqueChico()
    {
        if (GetComponent<ChaseZone>() == null)
        {
            Collider2D[] propios = GetComponents<Collider2D>();
            for (int i = 0; i < propios.Length; i++)
            {
                if (propios[i] != null)
                    propios[i].enabled = false;
            }

            if (GetComponent<Rigidbody2D>() == null)
            {
                Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        Transform t = transform.Find("ToqueMano");
        GameObject toque = t != null ? t.gameObject : new GameObject("ToqueMano");
        if (t == null)
            toque.transform.SetParent(transform, false);
        toque.transform.localPosition = new Vector3(0.15f, -0.1f, 0f);

        CircleCollider2D cir = toque.GetComponent<CircleCollider2D>();
        if (cir == null)
            cir = toque.AddComponent<CircleCollider2D>();
        cir.isTrigger = true;
        cir.radius = 0.28f;
        cir.enabled = true;
        colMano = cir;
    }

    public void StartChase(Transform objetivo, ChaseZone zona)
    {
        player = objetivo;
        zone = zona;
        farolesPrendidos.Clear();
        objetivoActual = null;
        extinguishPedido = false;
        yaAgarro = false;
        yaPasoPrimera = false;
        farolEncendiendo = null;
        tiempoEnPrimera = 0f;
        farolesUsados.Clear();
        farolesSaltados.Clear();
        momentoPrendido.Clear();
        tiempoSinPasarPrimera = 0f;
        currentSpeed = zone != null ? zone.HandIntroSpeed : 1.15f;
        AplicarAlpha(alphaFantasma);
        gameObject.SetActive(true);
        ColocarArribaDetras();
        CambiarEstado(Estado.Chasing);
        if (animator != null)
            animator.Play(estadoPersigue, 0, 0f);
    }

    void ColocarArribaDetras()
    {
        if (player == null) return;
        float dir = 1f;
        Lantern primera = PrimeraLinterna();
        if (primera != null)
            dir = Mathf.Sign(primera.transform.position.x - player.position.x);
        if (Mathf.Abs(dir) < 0.01f) dir = 1f;

        Vector3 p = transform.position;
        p.y = YConDedosA(alturaDedos);
        if (Mathf.Abs(p.x - player.position.x) < 3.5f)
            p.x = player.position.x - dir * 5f;
        transform.position = p;
    }

    void UbicarToqueEnDedos()
    {
        if (colMano == null) return;
        Vector2 d = OffsetDedos();
        colMano.transform.position = new Vector3(transform.position.x + d.x, transform.position.y + d.y, transform.position.z);
    }

    // Distancia del pivote de la mano a las puntas de los dedos.
    Vector2 OffsetDedos()
    {
        if (sr == null) return new Vector2(0.15f, -0.1f);
        Bounds b = sr.bounds;
        float haciaDedos = sr.flipX ? -1f : 1f;
        return new Vector2(b.center.x + haciaDedos * b.extents.x * 0.4f - transform.position.x,
            b.min.y + 0.25f - transform.position.y);
    }

    Vector2 CentroDeHope()
    {
        Collider2D c = player.GetComponent<Collider2D>();
        if (c != null) return c.bounds.center;
        return (Vector2)player.position + Vector2.up * 0.5f;
    }

    float YConDedosA(float alturaSobreHope)
    {
        float pivoteADedos = sr != null ? transform.position.y - sr.bounds.min.y : 0.5f;
        return player.position.y + alturaSobreHope + pivoteADedos;
    }

    public void GoInactive()
    {
        HopeHandAudio audioMano = GetComponent<HopeHandAudio>();
        if (audioMano != null) audioMano.Reiniciar();
        estado = Estado.Inactive;
        farolesPrendidos.Clear();
        objetivoActual = null;
        player = null;
        yaAgarro = false;
        yaPasoPrimera = false;
        farolEncendiendo = null;
        tiempoEnPrimera = 0f;
        AplicarAlpha(0f);
    }

    // Compatibilidad con el respawn y las luces viejas de PrimeraLuz.
    public void Reiniciar()
    {
        if (zone != null && zone.EstaActiva)
            zone.ResetZone();
        else
            GoInactive();
    }

    public void RetractarPorLuz()
    {
        // La mano vieja se retraia al prender luzfondo. La fantasma no usa esto.
    }

    public void NotifyLightingStarted(Lantern l)
    {
        if (l == null) return;
        farolesUsados.Add(l);
        yaPasoPrimera = true;
    }

    public void NotifyLanternLit(Lantern l)
    {
        if (l == null) return;
        farolEncendiendo = null;
        yaPasoPrimera = true;
        if (l.isFinal)
        {
            CambiarEstado(Estado.Leaving);
            return;
        }

        if (!farolesPrendidos.Contains(l))
            farolesPrendidos.Add(l);
        momentoPrendido[l] = Time.time;

        if (estado == Estado.Distracted && extinguishPedido)
            return;

        objetivoActual = l;
        extinguishPedido = false;
        currentSpeed = VelocidadApagado();
        CambiarEstado(Estado.Distracted);
        if (animator != null)
            animator.Play(estadoPersigue, 0, 0f);
    }

    public void NotifyLightingCancelado(Lantern l)
    {
        if (farolEncendiendo == l)
            farolEncendiendo = null;
    }

    void Update()
    {
        switch (estado)
        {
            case Estado.Chasing:
                ActualizarChase();
                break;
            case Estado.Distracted:
                ActualizarDistracted();
                break;
            case Estado.Leaving:
                break;
        }
    }

    void ActualizarChase()
    {
        if (player == null || zone == null) return;

        if (!yaPasoPrimera)
        {
            currentSpeed = zone.HandIntroSpeed;
            if (PrimeraLinternaSaltada())
            {
                yaPasoPrimera = true;
                currentSpeed = zone.HandBaseSpeed;
            }
        }
        RevisarFarolesSaltados();
        if (yaPasoPrimera)
        {
            int saltados = farolesSaltados.Count;
            float tope = zone.HandMaxSpeed + zone.VelocidadExtraPorFarolSaltado * saltados;
            float aceleracion = zone.HandAcceleration + zone.AceleracionExtraPorFarolSaltado * saltados;
            currentSpeed = Mathf.Min(tope, currentSpeed + aceleracion * Time.deltaTime);
        }

        Vector3 pos = transform.position;
        Vector2 dedos = OffsetDedos();
        Vector2 cuerpo = CentroDeHope();
        float distX = Mathf.Abs(cuerpo.x - (pos.x + dedos.x));

        tiempoSinPasarPrimera = yaPasoPrimera ? 0f : tiempoSinPasarPrimera + Time.deltaTime;
        if (!yaPasoPrimera && (distX <= zone.HandReachDistance || tiempoSinPasarPrimera >= segundosMaxSinPrimerFarol))
        {
            yaPasoPrimera = true;
            currentSpeed = Mathf.Max(currentSpeed, zone.HandBaseSpeed);
        }

        // Las puntas de los dedos (donde mata) son las que persiguen el cuerpo de Hope.
        pos.x = Mathf.MoveTowards(pos.x, cuerpo.x - dedos.x, currentSpeed * Time.deltaTime);

        float yObjetivo = YConDedosA(alturaDedos);
        if (yaPasoPrimera && distX <= zone.HandReachDistance)
        {
            float t = 1f - Mathf.Clamp01(distX / Mathf.Max(0.01f, zone.HandReachDistance));
            yObjetivo = Mathf.Lerp(yObjetivo, player.position.y + alturaDedosAlAtrapar - dedos.y, t);
        }
        pos.y = Mathf.MoveTowards(pos.y, yObjetivo, Mathf.Max(currentSpeed, 3f) * Time.deltaTime);

        float haciaHope = cuerpo.x - transform.position.x;
        if (Mathf.Abs(haciaHope) > 1.2f)
            FlipHacia(haciaHope);
        transform.position = pos;
        UbicarToqueEnDedos();

        if (TocaAlJugador())
            Atrapar();
    }

    void RevisarFarolesSaltados()
    {
        if (zone == null || zone.Lanterns == null || zone.Lanterns.Count == 0) return;
        float dir = DireccionPersecucion();
        for (int i = 0; i < zone.Lanterns.Count; i++)
        {
            Lantern l = zone.Lanterns[i];
            if (l == null || l.isFinal || farolesUsados.Contains(l) || farolesSaltados.Contains(l)) continue;
            if (l.EstadoActual != Lantern.Estado.Off) continue;
            if ((player.position.x - l.transform.position.x) * dir <= 1.1f) continue;

            farolesSaltados.Add(l);
            yaPasoPrimera = true;
            currentSpeed = Mathf.Max(currentSpeed, zone.HandBaseSpeed) + zone.VelocidadExtraPorFarolSaltado;
        }
    }

    float DireccionPersecucion()
    {
        List<Lantern> ls = zone.Lanterns;
        Lantern primera = null, ultima = null;
        for (int i = 0; i < ls.Count; i++)
        {
            if (ls[i] == null) continue;
            if (primera == null) primera = ls[i];
            ultima = ls[i];
        }
        if (primera != null && ultima != null && primera != ultima)
        {
            float d = Mathf.Sign(ultima.transform.position.x - primera.transform.position.x);
            if (Mathf.Abs(d) > 0.01f) return d;
        }
        float p = Mathf.Sign(player.position.x - transform.position.x);
        return Mathf.Abs(p) > 0.01f ? p : 1f;
    }

    bool PrimeraLinternaSaltada()
    {
        Lantern primera = PrimeraLinterna();
        if (primera == null) return false;
        if (primera.EstadoActual != Lantern.Estado.Off) return true;

        if (primera.JugadorEnRango)
        {
            tiempoEnPrimera += Time.deltaTime;
            return tiempoEnPrimera >= 2.4f;
        }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        if (Mathf.Abs(dir) < 0.01f) dir = 1f;
        return (player.position.x - primera.transform.position.x) * dir > 1.1f;
    }

    Lantern PrimeraLinterna()
    {
        if (zone == null || zone.Lanterns == null) return null;
        for (int i = 0; i < zone.Lanterns.Count; i++)
        {
            Lantern l = zone.Lanterns[i];
            if (l != null) return l;
        }
        return null;
    }

    bool TocaAlJugador()
    {
        if (!PuedeAtrapar()) return false;
        Collider2D cuerpo = player.GetComponent<Collider2D>();
        if (colMano == null || cuerpo == null) return false;
        return colMano.IsTouching(cuerpo);
    }

    bool PuedeAtrapar()
    {
        if (player == null || zone == null || yaAgarro) return false;
        if (!yaPasoPrimera) return false;
        if (estado != Estado.Chasing) return false;
        return true;
    }

    void Atrapar()
    {
        if (yaAgarro || zone == null) return;
        yaAgarro = true;
        CambiarEstado(Estado.Inactive);
        zone.OnPlayerCaught();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!PuedeAtrapar()) return;
        if (colMano != null && !colMano.IsTouching(other)) return;
        PlayerController2D pp = other.GetComponent<PlayerController2D>();
        if (pp == null) pp = other.GetComponentInParent<PlayerController2D>();
        if (pp == null) return;
        Atrapar();
    }

    void ActualizarDistracted()
    {
        if (objetivoActual == null)
            objetivoActual = FarolMasCercano();
        if (objetivoActual == null)
        {
            VolverAPerseguir();
            return;
        }

        if (extinguishPedido)
            return;

        if (!LlevaPrendidoLoSuficiente(objetivoActual))
            return;

        Vector3 destino = objetivoActual.PuntoApagado;
        currentSpeed = VelocidadApagado();
        transform.position = Vector3.MoveTowards(transform.position, destino, currentSpeed * Time.deltaTime);
        FlipHacia(destino.x - transform.position.x);

        Vector2 a = transform.position;
        Vector2 b = destino;
        if (Vector2.Distance(a, b) <= arriveDistance)
            EmpezarApagar();
    }

    bool LlevaPrendidoLoSuficiente(Lantern l)
    {
        float desde;
        if (l == null || l != PrimeraLinterna()) return true;
        if (!momentoPrendido.TryGetValue(l, out desde)) return true;
        return Time.time - desde >= segundosLuzAntesDeIr;
    }

    float VelocidadApagado()
    {
        if (zone == null) return 9f;
        return Mathf.Max(zone.HandMaxSpeed, zone.HandBaseSpeed * 3f, 8f);
    }

    void EmpezarApagar()
    {
        if (extinguishPedido) return;
        extinguishPedido = true;
        if (animator != null)
            animator.Play(estadoApaga, 0, 0f);
        StartCoroutine(ApagarPorTiempo());
    }

    IEnumerator ApagarPorTiempo()
    {
        float duracion = 1.05f;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].name.IndexOf("apaga", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    duracion = Mathf.Max(0.4f, clips[i].length);
                    break;
                }
            }
        }

        yield return new WaitForSeconds(duracion * 0.45f);
        if (extinguishPedido)
            OnExtinguishHit();
        yield return new WaitForSeconds(duracion * 0.55f);
        if (extinguishPedido)
            OnExtinguishEnd();
    }

    // Animation Event: frame donde cierra sobre la llama.
    public void OnExtinguishHit()
    {
        if (objetivoActual != null)
            objetivoActual.TurnOff();
    }

    // Animation Event: ultimo frame de mano_apaga_vela.
    public void OnExtinguishEnd()
    {
        if (!extinguishPedido)
            return;

        if (objetivoActual != null)
            farolesPrendidos.Remove(objetivoActual);
        objetivoActual = null;
        extinguishPedido = false;

        if (farolesPrendidos.Count > 0)
        {
            objetivoActual = FarolMasCercano();
            currentSpeed = VelocidadApagado();
            if (animator != null)
                animator.Play(estadoPersigue, 0, 0f);
        }
        else
            VolverAPerseguir();
    }

    void VolverAPerseguir()
    {
        yaPasoPrimera = true;
        currentSpeed = zone != null ? zone.HandBaseSpeed : 2.5f;
        extinguishPedido = false;
        objetivoActual = null;
        if (animator != null)
            animator.Play(estadoPersigue, 0, 0f);
        CambiarEstado(Estado.Chasing);
    }

    IEnumerator SalirDeCamara()
    {
        float t = 0f;
        float yFuera = YBordeSuperiorCamara() + 4f;
        while (t < leaveDuration)
        {
            t += Time.deltaTime;
            Vector3 p = transform.position;
            p.y = Mathf.MoveTowards(p.y, yFuera, leaveSpeed * Time.deltaTime);
            transform.position = p;
            AplicarAlpha(Mathf.Lerp(alphaFantasma, 0f, t / leaveDuration));
            yield return null;
        }
        AplicarAlpha(0f);
        if (zone != null)
            zone.OnChaseWon();
        GoInactive();
        gameObject.SetActive(false);
    }

    Lantern FarolMasCercano()
    {
        Lantern mejor = null;
        float mejorDist = float.MaxValue;
        for (int i = 0; i < farolesPrendidos.Count; i++)
        {
            Lantern l = farolesPrendidos[i];
            if (l == null) continue;
            float d = Vector2.Distance(transform.position, l.PuntoApagado);
            if (d < mejorDist)
            {
                mejorDist = d;
                mejor = l;
            }
        }
        return mejor;
    }

    float YBordeSuperiorCamara()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return transform.position.y + 4f;
        return cam.transform.position.y + cam.orthographicSize;
    }

    void FlipHacia(float dirX)
    {
        if (sr == null || Mathf.Abs(dirX) < 0.01f) return;
        sr.flipX = dirX < 0f;
    }

    void LateUpdate()
    {
        AplicarRotacion();
    }

    void AplicarRotacion()
    {
        bool espejada = sr != null && sr.flipX;
        transform.rotation = Quaternion.Euler(0f, 0f, espejada ? -rotacion : rotacion);
    }

    void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        AplicarRotacion();
#if UNITY_EDITOR
        if (controllerMano == null)
            controllerMano = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/mano/ManoFantasma.controller");
#endif
    }

    void AplicarAlpha(float a)
    {
        if (sr == null) return;
        Color c = colorBase;
        c.a = Mathf.Clamp01(a);
        sr.color = c;
    }

    void CambiarEstado(Estado nuevo)
    {
        if (estado == Estado.Leaving && nuevo != Estado.Inactive)
            return;
        estado = nuevo;
        if (nuevo == Estado.Leaving)
            StartCoroutine(SalirDeCamara());
        if (nuevo == Estado.Inactive)
            AplicarAlpha(0f);
        else
            AplicarAlpha(alphaFantasma);
    }
}
