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
    [Tooltip("Offset para que la mano apunte a la llama, no al pivot.")]
    public Vector3 extinguishOffset = new Vector3(0f, 0.4f, 0f);
    [Tooltip("Cuantas veces se ve la chispita antes de soltar a Hope. Mas = mas tension.")]
    public float ciclosChispita = 2f;
    [Tooltip("Cartel que aparece al acercarse a un farol que no es de una ChaseZone.")]
    public string textoCartelE = "Encender con E";
    [Tooltip("Segundos que queda prendido un farol que no es de una ChaseZone antes de apagarse solo (0 = no se apaga).")]
    public float segundosPrendidoLibre = 6f;
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

    public Estado EstadoActual { get { return estado; } }
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

        Transform t = transform.Find("Chispita");
        if (t != null)
        {
            chispita = t.gameObject;
            chispita.SetActive(false);
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

    // Sin ChaseZone el farol es libre: se prende con E en cualquier momento.
    bool EsLibre { get { return zona == null; } }

    void Update()
    {
        bool puedePrender = playerEnRango != null && estado == Estado.Off &&
            !playerEnRango.IsLocked && (EsLibre || zona.EstaActiva);
        MostrarCartelE(puedePrender && EsLibre);
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
        if (chispita != null) chispita.SetActive(true);

        float duracionChispa = DuracionClipChispita() * Mathf.Max(1f, ciclosChispita);
        if (playerEnRango != null)
        {
            playerEnRango.LockMovement(30f);
            playerEnRango.ReproducirPrendeFarol(duracionChispa);
        }

        if (zona != null && zona.Hand != null)
            zona.Hand.NotifyLightingStarted(this);

        if (playerEnRango != null)
        {
            HopeLanternAudio audioFarol = GetComponent<HopeLanternAudio>();
            if (audioFarol == null) audioFarol = gameObject.AddComponent<HopeLanternAudio>();
            audioFarol.Encender(gameObject, playerEnRango.transform);
        }

        yield return new WaitForSeconds(duracionChispa);

        TerminarEncendido();
        encendido = null;
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
        if (chispita != null) chispita.SetActive(false);
        if (animator != null)
        {
            animator.ResetTrigger("Off");
            animator.SetTrigger("Light");
        }
        if (luzOscuridad != null)
        {
            luzOscuridad.SetActive(true);
            if (brilloLuz != null) brilloLuz.encendida = true;
        }

        if (playerEnRango != null)
            playerEnRango.UnlockMovement();

        if (zona != null && zona.Hand != null)
            zona.Hand.NotifyLanternLit(this);

        if (EsLibre && segundosPrendidoLibre > 0f)
        {
            if (apagadoLibre != null) StopCoroutine(apagadoLibre);
            apagadoLibre = StartCoroutine(ApagarLibre());
        }
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
        if (encendido != null)
        {
            StopCoroutine(encendido);
            encendido = null;
        }
        estado = Estado.Off;
        HopeLanternAudio audioFarol = GetComponent<HopeLanternAudio>();
        if (audioFarol != null) audioFarol.Reiniciar();
        if (chispita != null) chispita.SetActive(false);
        if (luzOscuridad != null)
        {
            if (brilloLuz != null) brilloLuz.encendida = false;
            luzOscuridad.SetActive(false);
        }
        if (animator != null)
        {
            animator.ResetTrigger("Light");
            animator.SetTrigger("Off");
        }
        if (playerEnRango != null && playerEnRango.IsLocked)
            playerEnRango.UnlockMovement();
        if (zona != null && zona.Hand != null)
            zona.Hand.NotifyLightingCancelado(this);
    }

    public void ResetLantern()
    {
        if (encendido != null)
        {
            StopCoroutine(encendido);
            encendido = null;
        }
        estado = Estado.Off;
        HopeLanternAudio audioFarol = GetComponent<HopeLanternAudio>();
        if (audioFarol != null) audioFarol.Reiniciar();
        if (chispita != null) chispita.SetActive(false);
        if (luzOscuridad != null)
        {
            if (brilloLuz != null) brilloLuz.encendida = false;
            luzOscuridad.SetActive(false);
        }
        if (animator != null)
        {
            animator.ResetTrigger("Light");
            animator.ResetTrigger("Off");
            animator.Play("Apagado", 0, 0f);
        }
        if (playerEnRango != null && playerEnRango.IsLocked)
            playerEnRango.UnlockMovement();
        if (zona != null && zona.Hand != null)
            zona.Hand.NotifyLightingCancelado(this);
        playerEnRango = null;
    }
}
