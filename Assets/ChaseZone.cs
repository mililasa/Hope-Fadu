using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Una zona de persecucion: el trigger arranca la mano fantasma y los faroles.
public class ChaseZone : MonoBehaviour
{
    [Header("Zona")]
    [SerializeField] string zoneName = "Zona 1";
    [SerializeField] BoxCollider2D startTrigger;
    [SerializeField] Transform endPoint;
    [SerializeField] Transform handSpawnPoint;

    [Header("Faroles (arrastrá Farol, Farol (1)... desde la Hierarchy)")]
    [SerializeField] List<GameObject> faroles = new List<GameObject>();
    [SerializeField] HandChaser hand;
    [SerializeField] CartelZona pantallaPerdiste;
    [SerializeField] float playerChaseSpeed = 6f;
    [SerializeField] float handIntroSpeed = 1.15f;
    [SerializeField] float handBaseSpeed = 2.5f;
    [SerializeField] float handAcceleration = 0.6f;
    [SerializeField] float handMaxSpeed = 7f;
    [Tooltip("Velocidad que gana la mano de golpe por cada farol que Hope pasa sin prender (tambien sube su tope).")]
    [SerializeField] float velocidadExtraPorFarolSaltado = 3f;
    [Tooltip("Aceleracion extra por cada farol que Hope pasa sin prender.")]
    [SerializeField] float aceleracionExtraPorFarolSaltado = 1f;
    [SerializeField] float catchDistance = 0.6f;
    [SerializeField] float handTopOffset = 0.2f;
    [SerializeField] float handReachDistance = 3.5f;
    [SerializeField] float playerLockDuration = 0.7f;
    [Header("Barriles de la zona")]
    [SerializeField] RuntimeAnimatorController animBarrilPrendiendo;
    [SerializeField] RuntimeAnimatorController animBarrilPrendido;
    [SerializeField] Sprite spriteBarrilApagado;
    [SerializeField] [TextArea] string avisoFinal = "Estas a salvo!\nBusca la salida";
    [SerializeField] float avisoFinalDuracion = 3f;
    [SerializeField] SpriteRenderer pilaCadaver;
    [SerializeField] Sprite spritePilaPrendida;

    [Header("Agujero de Hope")]
    [Tooltip("Tamano del agujero al terminar el cierre, como parte del original. 0.4 = queda al 40%.")]
    [SerializeField] [Range(0.2f, 1f)] float radioFinalAgujero = 0.4f;
    [Tooltip("Segundos que tarda en cerrarse, de forma pareja, desde que empieza la persecucion.")]
    [SerializeField] float segundosCierreAgujero = 6f;

    PlayerController2D player;
    bool zonaActiva;
    bool zonaGanada;
    bool triggerArmado = true;
    readonly List<Lantern> lanterns = new List<Lantern>();
    GameObject pantallaAviso;
    Text textoAviso;
    Sprite spritePilaOriginal;
    LanternGlow glowHope;
    Vector3 escalaAgujeroInicial;
    float tiempoCierreAgujero;
    bool escalaAgujeroLista;

    public string ZoneName { get { return zoneName; } }
    public float PlayerChaseSpeed { get { return playerChaseSpeed; } }
    public float HandIntroSpeed { get { return handIntroSpeed; } }
    public float HandBaseSpeed { get { return handBaseSpeed; } }
    public float HandAcceleration { get { return handAcceleration; } }
    public float HandMaxSpeed { get { return handMaxSpeed; } }
    public float VelocidadExtraPorFarolSaltado { get { return velocidadExtraPorFarolSaltado; } }
    public float AceleracionExtraPorFarolSaltado { get { return aceleracionExtraPorFarolSaltado; } }
    public float CatchDistance { get { return catchDistance; } }
    public float HandTopOffset { get { return handTopOffset; } }
    public float HandReachDistance { get { return handReachDistance; } }
    public float PlayerLockDuration { get { return playerLockDuration; } }
    public bool EstaActiva { get { return zonaActiva; } }
    public List<Lantern> Lanterns { get { return lanterns; } }
    public BoxCollider2D StartTrigger { get { return startTrigger; } }
    public HandChaser Hand { get { return hand; } }

    void Awake()
    {
        if (startTrigger == null)
            startTrigger = GetComponent<BoxCollider2D>();
        if (startTrigger != null)
            startTrigger.isTrigger = true;

        if (pantallaPerdiste == null)
        {
            GameObject go = GameObject.Find("Perdiste");
            if (go != null) pantallaPerdiste = go.GetComponent<CartelZona>();
        }

        if (startTrigger != null && startTrigger.gameObject != gameObject)
        {
            ChaseZoneTrigger relay = startTrigger.GetComponent<ChaseZoneTrigger>();
            if (relay == null)
                relay = startTrigger.gameObject.AddComponent<ChaseZoneTrigger>();
            relay.zona = this;
        }

        if (hand != null)
            hand.gameObject.SetActive(false);

        AsegurarBarriles();
        AsegurarPila();
        ReconstruirFaroles();
        CrearAviso();
    }

    void Start()
    {
        ReconstruirFaroles();
    }

    void AsegurarBarriles()
    {
#if UNITY_EDITOR
        if (animBarrilPrendiendo == null)
            animBarrilPrendiendo = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/farol/Barri-Encendiendosel-Sheet_0.controller");
        if (animBarrilPrendido == null)
            animBarrilPrendido = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/farol/Barril-Prendido-Sheet_0.controller");
        if (spriteBarrilApagado == null)
            spriteBarrilApagado = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/farol/Barril1.png");
#endif
    }

    public void ReconstruirFaroles()
    {
        AsegurarBarriles();
        if (faroles == null)
            faroles = new List<GameObject>();

        if (faroles.Count == 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform t = transform.GetChild(i);
                if (t.name.IndexOf("Farol", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    faroles.Add(t.gameObject);
            }
        }

        lanterns.Clear();
        for (int i = 0; i < faroles.Count; i++)
        {
            GameObject go = faroles[i];
            if (go == null) continue;
            Lantern l = go.GetComponent<Lantern>();
            if (l == null)
                l = go.AddComponent<Lantern>();
            bool esFarol4 = go.name == "Farol (4)";
            // isFinal: último de la lista (gana la zona). Checkpoint permanente: Farol (4).
            // Farol (5)/(6) son checkpoints libres (fuera de esta lista).
            l.isFinal = i == faroles.Count - 1 || esFarol4;
            l.esCheckpointPermanente = esFarol4;
            l.AsignarZona(this);
            l.ConfigurarComoBarril(animBarrilPrendiendo, animBarrilPrendido, spriteBarrilApagado);
            lanterns.Add(l);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (startTrigger != null && startTrigger.gameObject != gameObject)
            return;
        IntentarArrancar(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (startTrigger != null && startTrigger.gameObject != gameObject)
            return;
        IntentarArrancar(other);
    }

    public void IntentarArrancar(Collider2D other)
    {
        if (zonaGanada || zonaActiva || !triggerArmado) return;

        PlayerController2D pp = other.GetComponent<PlayerController2D>();
        if (pp == null) pp = other.GetComponentInParent<PlayerController2D>();
        if (pp == null) return;

        OnEnter(pp);
    }

    public void OnEnter(PlayerController2D pp)
    {
        if (zonaGanada || zonaActiva) return;
        player = pp;
        zonaActiva = true;
        triggerArmado = false;
        ArrancarPersecucion();
    }

    void ArrancarPersecucion()
    {
        if (player == null) return;

        player.SetMoveSpeed(playerChaseSpeed);

        ReconstruirFaroles();
        for (int i = 0; i < lanterns.Count; i++)
        {
            if (lanterns[i] != null)
                lanterns[i].AsignarZona(this);
        }

        Vector3 spawn = handSpawnPoint != null ? handSpawnPoint.position : transform.position;
        if (hand != null)
        {
            hand.gameObject.SetActive(true);
            hand.transform.position = spawn;
            hand.StartChase(player.transform, this);
        }

        EmpezarCierreAgujero();
    }

    void LateUpdate()
    {
        if (!zonaActiva || player == null) return;
        AsegurarGlow();
        if (glowHope == null) return;

        if (!escalaAgujeroLista)
        {
            escalaAgujeroInicial = glowHope.transform.localScale;
            escalaAgujeroLista = true;
        }

        tiempoCierreAgujero += Time.deltaTime;
        float duracion = Mathf.Max(0.2f, segundosCierreAgujero);
        float lineal = Mathf.Clamp01(tiempoCierreAgujero / duracion);
        // t^2: casi no se mueve al principio y aprieta hacia el final del tiempo.
        float t = lineal * lineal;
        float factor = Mathf.Lerp(1f, radioFinalAgujero, t);
        glowHope.factorRadio = factor;
        Vector3 escala = escalaAgujeroInicial * factor;
        glowHope.transform.localScale = new Vector3(escala.x, escala.y, escalaAgujeroInicial.z);
    }

    void EmpezarCierreAgujero()
    {
        tiempoCierreAgujero = 0f;
        escalaAgujeroLista = false;
        AsegurarGlow();
        if (glowHope != null) glowHope.factorRadio = 1f;
    }

    void AsegurarGlow()
    {
        if (glowHope != null || player == null) return;
        Transform oscuridad = player.transform.Find("Oscuridad");
        if (oscuridad != null) glowHope = oscuridad.GetComponent<LanternGlow>();
    }

    void RestaurarAgujero()
    {
        tiempoCierreAgujero = 0f;
        escalaAgujeroLista = false;
        AsegurarGlow();
        if (glowHope != null) glowHope.factorRadio = 1f;
    }

    void CrearAviso()
    {
        if (pantallaAviso != null) return;

        pantallaAviso = new GameObject("AvisoChase_" + zoneName);
        Canvas canvas = pantallaAviso.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 480;
        CanvasScaler scaler = pantallaAviso.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        pantallaAviso.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject fondoGO = new GameObject("Fondo");
        fondoGO.transform.SetParent(pantallaAviso.transform, false);
        UnityEngine.UI.Image fondo = fondoGO.AddComponent<UnityEngine.UI.Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform fondoRt = fondo.rectTransform;
        fondoRt.anchorMin = Vector2.zero;
        fondoRt.anchorMax = Vector2.one;
        fondoRt.offsetMin = Vector2.zero;
        fondoRt.offsetMax = Vector2.zero;

        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(pantallaAviso.transform, false);
        textoAviso = textoGO.AddComponent<Text>();
        textoAviso.text = avisoFinal;
        textoAviso.fontSize = 42;
        textoAviso.alignment = TextAnchor.MiddleCenter;
        textoAviso.color = Color.white;
        textoAviso.horizontalOverflow = HorizontalWrapMode.Wrap;
        textoAviso.verticalOverflow = VerticalWrapMode.Overflow;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textoAviso.font = font;
        RectTransform textoRt = textoAviso.rectTransform;
        textoRt.anchorMin = new Vector2(0.08f, 0.2f);
        textoRt.anchorMax = new Vector2(0.92f, 0.8f);
        textoRt.offsetMin = Vector2.zero;
        textoRt.offsetMax = Vector2.zero;

        pantallaAviso.SetActive(false);
    }

    void MostrarAviso(bool visible, string texto)
    {
        if (pantallaAviso == null)
            CrearAviso();
        if (textoAviso != null)
            textoAviso.text = texto;
        if (pantallaAviso != null)
            pantallaAviso.SetActive(visible);
    }

    public void OnChaseWon()
    {
        if (!zonaActiva && zonaGanada) return;
        zonaActiva = false;
        zonaGanada = true;
        if (player != null)
            player.RestoreMoveSpeed();
        if (hand != null)
            hand.gameObject.SetActive(false);
        triggerArmado = false;
        MostrarAviso(false, avisoFinal);
        // Los barriles de la persecución quedan apagados + chispa, salvo Farol (4) checkpoint.
        for (int i = 0; i < lanterns.Count; i++)
        {
            if (lanterns[i] != null)
                lanterns[i].ApagarTrasPersecucion();
        }
        RestaurarAgujero();
    }

    public void OnPlayerCaught()
    {
        if (!zonaActiva) return;
        zonaActiva = false;
        if (player != null)
        {
            player.SonarMuerte(true);
            player.VolverAlCheckPoint();
        }
    }

    // Al morir: no re-arma la persecución si ya se ganó; conserva cadáver y Farol (4).
    public void ReiniciarPorCheckpoint()
    {
        StopAllCoroutines();
        MostrarAviso(false, avisoFinal);
        bool yaGano = zonaGanada;
        ResetZone();
        if (yaGano)
        {
            zonaGanada = true;
            triggerArmado = false;
        }
        else
            triggerArmado = true;
    }

    public void OnUltimoBarrilPrendido(PlayerController2D pp)
    {
        AsegurarPila();
        CadaverChase cadaver = null;
        if (pilaCadaver != null)
            cadaver = pilaCadaver.GetComponent<CadaverChase>();
        if (cadaver == null)
        {
            GameObject go = GameObject.Find("pilacadaver");
            if (go != null)
            {
                pilaCadaver = go.GetComponent<SpriteRenderer>();
                cadaver = go.GetComponent<CadaverChase>();
                if (cadaver == null)
                    cadaver = go.AddComponent<CadaverChase>();
            }
        }
        if (cadaver != null)
            cadaver.Despertar();
        else
            Debug.LogWarning("Hope: no se encontro pilacadaver para despertar.", this);
        if (pp != null)
            pp.LockMovement(1f);
    }

    void AsegurarPila()
    {
        if (pilaCadaver == null)
        {
            GameObject go = GameObject.Find("pilacadaver");
            if (go != null) pilaCadaver = go.GetComponent<SpriteRenderer>();
        }
        if (pilaCadaver != null && pilaCadaver.GetComponent<CadaverChase>() == null)
            pilaCadaver.gameObject.AddComponent<CadaverChase>();
    }

    public void ResetZone()
    {
        zonaActiva = false;
        if (player != null)
        {
            player.RestoreMoveSpeed();
            if (player.IsLocked)
                player.UnlockMovementYSoltarAnim();
        }

        for (int i = 0; i < lanterns.Count; i++)
        {
            if (lanterns[i] != null)
                lanterns[i].ResetLantern(); // permanentes lit se conservan sin re-animar
        }

        // Cadáver: no volver atrás si ya despertó.
        AsegurarPila();
        if (pilaCadaver != null)
        {
            CadaverChase cadaver = pilaCadaver.GetComponent<CadaverChase>();
            if (cadaver != null)
                cadaver.ConservarEstadoEnCheckpoint();
        }

        if (hand != null)
        {
            hand.GoInactive();
            hand.gameObject.SetActive(false);
        }

        RestaurarAgujero();

        if (!zonaGanada)
            triggerArmado = true;
    }

    void OnDrawGizmos()
    {
        BoxCollider2D box = startTrigger != null ? startTrigger : GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = new Color(0.9f, 0.3f, 0.1f, 0.25f);
            Vector3 centro = box.transform.TransformPoint(box.offset);
            Vector3 tam = Vector3.Scale(box.size, box.transform.lossyScale);
            Gizmos.DrawCube(centro, tam);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(centro, tam);
        }

        Vector3 origenMano = handSpawnPoint != null ? handSpawnPoint.position : transform.position;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(origenMano, 0.2f);
        if (endPoint != null)
        {
            Gizmos.DrawLine(origenMano, endPoint.position);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(endPoint.position, 0.18f);
        }

        for (int i = 0; i < lanterns.Count; i++)
        {
            if (lanterns[i] == null) continue;
            bool esFinal = lanterns[i].isFinal;
            Gizmos.color = esFinal ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(lanterns[i].transform.position, esFinal ? 0.35f : 0.22f);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        string label = zoneName + "\nchase " + playerChaseSpeed + "  intro " + handIntroSpeed + "  acc " + handAcceleration;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, label);
        for (int i = 0; i < lanterns.Count; i++)
        {
            if (lanterns[i] == null) continue;
            string n = (i + 1).ToString();
            if (lanterns[i].isFinal) n += " FINAL";
            UnityEditor.Handles.Label(lanterns[i].transform.position + Vector3.up * 0.6f, n);
        }
#endif
    }
}

public class ChaseZoneTrigger : MonoBehaviour
{
    public ChaseZone zona;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (zona != null)
            zona.IntentarArrancar(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (zona != null)
            zona.IntentarArrancar(other);
    }
}
