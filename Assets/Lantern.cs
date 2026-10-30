using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Farol de zona de persecucion. E para prenderlo si Hope esta en el trigger.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class Lantern : MonoBehaviour
{
    public enum Estado { Off, Lighting, Lit }

    public bool isFinal;
    [Tooltip("Offset para que la mano apunte a la llama, no al pivot. Subí Y si ApagaVela tapa el piso (Inspector del farol).")]
    public Vector3 extinguishOffset = new Vector3(0f, 2.8f, 0f);
    [Tooltip("Queda prendido y es checkpoint (Farol 4, 5, 6).")]
    public bool esCheckpointPermanente = false;
    [Tooltip("Cuantas veces se ve la chispita antes de soltar a Hope. Mas = mas tension.")]
    public float ciclosChispita = 2f;
    [Tooltip("Cartel que aparece al acercarse a un farol libre. Desactivar en Farol (5)/(6).")]
    public bool mostrarCartelEncender = true;
    [Tooltip("Cartel que aparece al acercarse a un farol que no es de una ChaseZone.")]
    public string textoCartelE = "Encender con E";
    [Tooltip("Segundos que queda prendido un farol que no es de una ChaseZone antes de apagarse solo (0 = no se apaga).")]
    public float segundosPrendidoLibre = 6f;
    [Header("Reflejo propio (no usa el grosor del escenario)")]
    [Tooltip("Fuerza del volumen. Es el valor final, no se multiplica por el del piso.")]
    [Range(0f, 3f)] public float intensidadRelieve = 1f;
    [Tooltip("Grosor del borde iluminado, en pixeles del sprite. Es el valor final.")]
    [Range(0.25f, 8f)] public float grosorContornoRelieve = 1.8f;
    [Tooltip("Que tan brillante es la linea del borde.")]
    [Range(0f, 3f)] public float brilloBordeRelieve = 1.15f;
    [Tooltip("Que tan marcado tiene que ser el borde para dibujarse. Mas alto = menos linea.")]
    [Range(0f, 0.6f)] public float umbralContornoRelieve = 0.035f;
    [Tooltip("Color del borde que refleja la luz.")]
    public Color colorBordeRelieve = new Color(1f, 0.84f, 0.12f, 1f);
    Animator animator;
    ChaseZone zona;
    PlayerController2D playerEnRango;
    GameObject chispita;
    GameObject luzOscuridad;
    LanternGlow brilloLuz;
    Estado estado = Estado.Off;
    Coroutine encendido;
    GameObject cartelE;
    Coroutine apagadoLibre;
    bool esBarril;
    RuntimeAnimatorController ctrlBarrilPrendiendo;
    RuntimeAnimatorController ctrlBarrilPrendido;
    Sprite spriteBarrilApagado;

    public Estado EstadoActual { get { return estado; } }
    public bool EsBarril { get { return esBarril; } }
    public bool JugadorEnRango { get { return playerEnRango != null; } }
    public Vector3 PuntoApagado
    {
        get { return transform.position + extinguishOffset; }
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        AsegurarExplosionesHijas();
        AsegurarRelieveDelFarol();

        Transform t = transform.Find("Chispita");
        if (t != null)
        {
            chispita = t.gameObject;
            // Indicador de que se puede prender: visible mientras el farol esta apagado.
            MostrarChispita(true);
        }

        t = transform.Find("LuzFarol");
        if (t == null) t = transform.Find("Oscuridad");
        if (t != null)
        {
            luzOscuridad = t.gameObject;
            bool glowNuevo = luzOscuridad.GetComponent<LanternGlow>() == null;
            AplicarOscuridadDeHope(luzOscuridad);
            brilloLuz = luzOscuridad.GetComponent<LanternGlow>();
            if (luzOscuridad.GetComponent<HaloLuz>() == null)
                luzOscuridad.AddComponent<HaloLuz>();
            HaloLuz halo = luzOscuridad.GetComponent<HaloLuz>();
            if (halo != null) halo.limitarOscuridadAlHalo = true;
            if (brilloLuz != null)
            {
                if (glowNuevo) brilloLuz.escalaBase = 0.5f;
                brilloLuz.respetarEscalaDelTransform = false;
                brilloLuz.encendida = true;
            }
            luzOscuridad.SetActive(false);
        }
    }

    // Mismo material que el piso, pero con relieve mas chico (valores del Inspector).
    void AsegurarRelieveDelFarol()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (sr.sortingLayerName == "Default" || string.IsNullOrEmpty(sr.sortingLayerName))
        {
            sr.sortingLayerName = "Foreground";
            if (sr.sortingOrder < 1) sr.sortingOrder = 2;
        }

        LuzRelieveEntorno luz = FindObjectOfType<LuzRelieveEntorno>();
        if (luz != null)
            luz.AplicarA(sr);
        else if (sr.GetComponent<RelieveSprite>() == null)
            sr.gameObject.AddComponent<RelieveSprite>();

        CopiarRelieve(sr.GetComponent<RelieveSprite>());
    }

    void CopiarRelieve(RelieveSprite relieve)
    {
        if (relieve == null) return;
        relieve.recibirLuz = true;
        relieve.usarParametrosPropios = true;
        relieve.usarColorContorno = true;
        relieve.intensidadRelieve = intensidadRelieve;
        relieve.grosorContorno = grosorContornoRelieve;
        relieve.brilloBorde = brilloBordeRelieve;
        relieve.umbralContorno = umbralContornoRelieve;
        relieve.colorContorno = colorBordeRelieve;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        RelieveSprite relieve = GetComponent<RelieveSprite>();
        if (relieve == null) return;
        CopiarRelieve(relieve);
    }
#endif

    public static void AplicarOscuridadDeHope(GameObject luz)
    {
        if (luz == null) return;
        SpriteRenderer sr = luz.GetComponent<SpriteRenderer>();
        HaloLuz.AplicarMaterial(sr);
        if (luz.GetComponent<LanternGlow>() == null)
            luz.AddComponent<LanternGlow>();
        HaloLuz.AsegurarHope();
    }

    public void AsignarZona(ChaseZone z)
    {
        zona = z;
    }

    void AsegurarExplosionesHijas()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo.name.IndexOf("explosion", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (hijo.GetComponent<ExplosionesChase>() == null)
                hijo.gameObject.AddComponent<ExplosionesChase>();
        }
    }

    // Solo los faroles de una ChaseZone: barrilprendiendose al prender, barrilprendido mientras queda prendido.
    public void ConfigurarComoBarril(RuntimeAnimatorController prendiendo, RuntimeAnimatorController prendido, Sprite apagado)
    {
        if (prendiendo != null) ctrlBarrilPrendiendo = prendiendo;
        if (prendido != null) ctrlBarrilPrendido = prendido;
        if (apagado != null) spriteBarrilApagado = apagado;
        if (spriteBarrilApagado == null && ctrlBarrilPrendiendo == null) return;
        esBarril = true;
        if (animator == null)
            animator = GetComponent<Animator>();
        if (estado == Estado.Lit)
            MostrarBarrilPrendido();
        else if (estado != Estado.Lighting)
            MostrarBarrilApagado();
    }

    void LateUpdate()
    {
        CopiarRelieve(GetComponent<RelieveSprite>());
        if (!esBarril || estado != Estado.Off) return;
        MostrarBarrilApagado();
    }

    // Sin ChaseZone el farol es libre: se prende con E en cualquier momento.
    bool EsLibre { get { return zona == null; } }

    void Update()
    {
        bool puedePrender = playerEnRango != null && estado == Estado.Off &&
            !playerEnRango.IsLocked && (EsLibre || zona.EstaActiva);
        MostrarCartelE(puedePrender && EsLibre && mostrarCartelEncender);
        if (!puedePrender) return;

        if (Input.GetKeyDown(KeyCode.E))
            Light();
    }

    void OnDisable()
    {
        MostrarCartelE(false);
    }

    void MostrarCartelE(bool visible)
    {
        if (cartelE == null)
        {
            if (!visible) return;
            CrearCartelE();
        }
        if (cartelE.activeSelf != visible)
            cartelE.SetActive(visible);
    }

    void CrearCartelE()
    {
        cartelE = new GameObject("CartelE_" + name);
        Canvas canvas = cartelE.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 470;
        CanvasScaler scaler = cartelE.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(cartelE.transform, false);
        Text texto = textoGO.AddComponent<Text>();
        texto.text = textoCartelE;
        texto.fontSize = 40;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        texto.font = font;
        Outline borde = textoGO.AddComponent<Outline>();
        borde.effectColor = new Color(0f, 0f, 0f, 0.85f);
        borde.effectDistance = new Vector2(2f, -2f);
        RectTransform rt = texto.rectTransform;
        rt.anchorMin = new Vector2(0.2f, 0.08f);
        rt.anchorMax = new Vector2(0.8f, 0.18f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        cartelE.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController2D pp = other.GetComponent<PlayerController2D>();
        if (pp == null) pp = other.GetComponentInParent<PlayerController2D>();
        if (pp != null) playerEnRango = pp;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PlayerController2D pp = other.GetComponent<PlayerController2D>();
        if (pp == null) pp = other.GetComponentInParent<PlayerController2D>();
        if (pp != null && pp == playerEnRango)
            playerEnRango = null;
    }

    public void Light()
    {
        if (estado != Estado.Off) return;
        if (encendido != null) StopCoroutine(encendido);
        encendido = StartCoroutine(EncenderConTension());
    }

    IEnumerator EncenderConTension()
    {
        estado = Estado.Lighting;
        // La chispa ya estaba como indicador; al prender pasa a la animacion del farol.
        MostrarChispita(false);
        float duracionChispa = DuracionClipChispita() * Mathf.Max(1f, ciclosChispita);
        if (esBarril)
            MostrarBarrilPrendiendo();
        else if (animator != null)
        {
            animator.ResetTrigger("Off");
            animator.SetTrigger("Light");
        }
        PlayerController2D pp = playerEnRango;
        if (pp == null) pp = FindObjectOfType<PlayerController2D>();
        if (pp != null)
        {
            playerEnRango = pp;
            pp.LockMovement(Mathf.Max(0.5f, duracionChispa + 0.15f));
            pp.ReproducirPrendeFarol(duracionChispa);
        }

        if (zona != null && zona.Hand != null)
            zona.Hand.NotifyLightingStarted(this);

        if (pp != null && HopeLegacyAudioPolicy.Permitir(this))
        {
            HopeLanternAudio audioFarol = GetComponent<HopeLanternAudio>();
            if (audioFarol == null) audioFarol = gameObject.AddComponent<HopeLanternAudio>();
            audioFarol.Encender(gameObject, pp.transform);
            if (gameObject.scene.name == "SampleScene")
            {
                HopeCheckpointAudio checkpoint = GetComponent<HopeCheckpointAudio>();
                if (checkpoint == null) checkpoint = gameObject.AddComponent<HopeCheckpointAudio>();
                bool primero = true;
                foreach (Lantern otro in FindObjectsOfType<Lantern>())
                    if (otro.gameObject.scene == gameObject.scene && otro.transform.position.x < transform.position.x)
                    { primero = false; break; }
                checkpoint.Activar(gameObject, primero);
            }
        }

        if (esBarril)
            yield return SostenerBarrilPrendiendo(duracionChispa);
        else
            yield return new WaitForSeconds(duracionChispa);

        TerminarEncendido();
        encendido = null;
    }

    void MostrarChispita(bool visible)
    {
        if (chispita == null) return;
        if (chispita.activeSelf != visible)
            chispita.SetActive(visible);
    }

    float DuracionClipChispita()
    {
        if (chispita == null) return 0.6f;
        Animator a = chispita.GetComponent<Animator>();
        if (a == null || a.runtimeAnimatorController == null) return 0.6f;
        AnimationClip[] clips = a.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0) return 0.6f;
        return Mathf.Max(0.2f, clips[0].length);
    }

    void TerminarEncendido()
    {
        estado = Estado.Lit;
        MostrarChispita(false);
        if (esBarril)
            MostrarBarrilPrendido();

        if (luzOscuridad != null)
        {
            luzOscuridad.SetActive(true);
            if (brilloLuz != null) brilloLuz.encendida = true;
        }

        PlayerController2D ppFin = playerEnRango != null ? playerEnRango : FindObjectOfType<PlayerController2D>();
        if (ppFin != null)
            ppFin.UnlockMovementYSoltarAnim();

        if (esCheckpointPermanente)
            RegistrarCheckpoint();

        // Cadáver solo al prender Farol (4).
        if (esCheckpointPermanente && isFinal && zona != null)
            zona.OnUltimoBarrilPrendido(ppFin != null ? ppFin : playerEnRango);

        if (zona != null && zona.Hand != null)
            zona.Hand.NotifyLanternLit(this);

        if (EsLibre && !esCheckpointPermanente && segundosPrendidoLibre > 0f)
        {
            if (apagadoLibre != null) StopCoroutine(apagadoLibre);
            apagadoLibre = StartCoroutine(ApagarLibre());
        }
    }

    void RegistrarCheckpoint()
    {
        PlayerController2D pp = playerEnRango;
        if (pp == null) pp = FindObjectOfType<PlayerController2D>();
        if (pp != null)
            pp.MarcarCheckpoint(transform);
    }

    IEnumerator ApagarLibre()
    {
        yield return new WaitForSeconds(segundosPrendidoLibre);
        apagadoLibre = null;
        if (estado != Estado.Lit) yield break;
        // TurnOff suelta a Hope si esta bloqueada; aca no debe cortarle otra accion.
        PlayerController2D pp = playerEnRango;
        playerEnRango = null;
        TurnOff();
        playerEnRango = pp;
    }

    // Animation Event del clip farolprendido (el dibujo ya quedo prendido).
    public void OnLit()
    {
        if (estado == Estado.Lighting)
            return;
        if (estado != Estado.Lit)
            TerminarEncendido();
    }

    public void TurnOff()
    {
        // Checkpoint permanente: la mano hace la animación/explosión pero la luz queda.
        if (esCheckpointPermanente && estado == Estado.Lit)
        {
            if (zona != null && zona.Hand != null)
                zona.Hand.NotifyLightingCancelado(this);
            return;
        }

        if (encendido != null)
        {
            StopCoroutine(encendido);
            encendido = null;
        }
        estado = Estado.Off;
        HopeLanternAudio audioFarol = GetComponent<HopeLanternAudio>();
        if (audioFarol != null) audioFarol.Reiniciar();
        HopeCheckpointAudio checkpointAudio = GetComponent<HopeCheckpointAudio>();
        if (checkpointAudio != null) checkpointAudio.Reiniciar();
        MostrarChispita(true);
        if (luzOscuridad != null)
        {
            if (brilloLuz != null) brilloLuz.encendida = false;
            luzOscuridad.SetActive(false);
        }
        if (esBarril)
            MostrarBarrilApagado();
        else if (animator != null)
        {
            animator.ResetTrigger("Light");
            animator.SetTrigger("Off");
        }
        if (playerEnRango != null && playerEnRango.IsLocked)
            playerEnRango.UnlockMovementYSoltarAnim();
        if (zona != null && zona.Hand != null)
            zona.Hand.NotifyLightingCancelado(this);
    }

    public void ResetLantern()
    {
        // Checkpoint permanente lit: se conserva sin re-animar.
        if (esCheckpointPermanente && estado == Estado.Lit)
        {
            MostrarChispita(false);
            if (esBarril)
                MostrarBarrilPrendido();
            if (luzOscuridad != null)
            {
                luzOscuridad.SetActive(true);
                if (brilloLuz != null) brilloLuz.encendida = true;
            }
            if (playerEnRango != null && playerEnRango.IsLocked)
                playerEnRango.UnlockMovementYSoltarAnim();
            playerEnRango = null;
            return;
        }

        bool yaEstabaApagado = estado == Estado.Off;
        if (encendido != null)
        {
            StopCoroutine(encendido);
            encendido = null;
        }
        estado = Estado.Off;
        HopeLanternAudio audioFarol = GetComponent<HopeLanternAudio>();
        if (audioFarol != null) audioFarol.Reiniciar();
        HopeCheckpointAudio checkpointAudio = GetComponent<HopeCheckpointAudio>();
        if (checkpointAudio != null) checkpointAudio.Reiniciar();
        MostrarChispita(true);
        if (luzOscuridad != null)
        {
            if (brilloLuz != null) brilloLuz.encendida = false;
            luzOscuridad.SetActive(false);
        }
        if (esBarril)
            MostrarBarrilApagado();
        else if (animator != null && !yaEstabaApagado)
        {
            // Solo re-anima si estaba prendido; si ya estaba apagado no repite Apagado.
            animator.ResetTrigger("Light");
            animator.ResetTrigger("Off");
            animator.Play("Apagado", 0, 0f);
        }
        if (playerEnRango != null && playerEnRango.IsLocked)
            playerEnRango.UnlockMovementYSoltarAnim();
        if (zona != null && zona.Hand != null)
            zona.Hand.NotifyLightingCancelado(this);
        playerEnRango = null;
    }

    // Apaga sin ser checkpoint (post-persecución): luz off + chispa on.
    public void ApagarTrasPersecucion()
    {
        if (esCheckpointPermanente && estado == Estado.Lit)
            return;
        if (encendido != null)
        {
            StopCoroutine(encendido);
            encendido = null;
        }
        estado = Estado.Off;
        MostrarChispita(true);
        if (luzOscuridad != null)
        {
            if (brilloLuz != null) brilloLuz.encendida = false;
            luzOscuridad.SetActive(false);
        }
        if (esBarril)
            MostrarBarrilApagado();
        else if (animator != null)
        {
            animator.ResetTrigger("Light");
            animator.ResetTrigger("Off");
            animator.Play("Apagado", 0, 1f);
            animator.Update(0f);
        }
    }

    void MostrarBarrilApagado()
    {
        if (animator != null)
            animator.enabled = false;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && spriteBarrilApagado != null)
            sr.sprite = spriteBarrilApagado;
        MostrarChispita(true);
    }

    void MostrarBarrilPrendiendo()
    {
        if (animator == null || ctrlBarrilPrendiendo == null) return;
        animator.enabled = true;
        animator.runtimeAnimatorController = ctrlBarrilPrendiendo;
        animator.speed = 1f;
        animator.Play("barrilprendiendose", 0, 0f);
    }

    void MostrarBarrilPrendido()
    {
        if (animator == null || ctrlBarrilPrendido == null) return;
        animator.enabled = true;
        animator.runtimeAnimatorController = ctrlBarrilPrendido;
        animator.speed = 1f;
        animator.Play("barrilprendido", 0, 0f);
    }

    IEnumerator SostenerBarrilPrendiendo(float duracion)
    {
        float largo = DuracionController(ctrlBarrilPrendiendo, "barrilprendiendose");
        if (largo > 0f)
            yield return new WaitForSeconds(Mathf.Min(largo, duracion));
        if (esBarril && estado == Estado.Lighting && animator != null)
        {
            animator.Play("barrilprendiendose", 0, 0.999f);
            animator.Update(0f);
            animator.speed = 0f;
        }
        float resto = duracion - largo;
        if (resto > 0f)
            yield return new WaitForSeconds(resto);
    }

    static float DuracionController(RuntimeAnimatorController ctrl, string estado)
    {
        if (ctrl == null) return 0f;
        AnimationClip[] clips = ctrl.animationClips;
        if (clips == null) return 0f;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == estado)
                return clips[i].length;
        }
        return 0f;
    }
}
