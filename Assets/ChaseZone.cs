using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    [SerializeField] float loseScreenDuration = 5f;
    [SerializeField] [TextArea] string avisoEntrada = "La oscuridad te persigue!\nPrende los faroles con E para alentizarla";
    [SerializeField] float avisoDuracion = 3f;
    [SerializeField] [TextArea] string avisoFinal = "Estas a salvo!\nBusca la salida";
    [SerializeField] float avisoFinalDuracion = 3f;

    PlayerController2D player;
    bool zonaActiva;
    bool zonaGanada;
    bool triggerArmado = true;
    readonly List<Lantern> lanterns = new List<Lantern>();
    GameObject pantallaAviso;
    Text textoAviso;

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

        ReconstruirFaroles();
        CrearAviso();
    }

    void Start()
    {
        ReconstruirFaroles();
    }

    public void ReconstruirFaroles()
    {
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
            l.isFinal = i == faroles.Count - 1;
            l.AsignarZona(this);
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
        StartCoroutine(EntrarConAviso());
    }

    IEnumerator EntrarConAviso()
    {
        if (player != null)
            player.LockMovement(avisoDuracion + 1f);

        MostrarAviso(true);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, avisoDuracion));
        MostrarAviso(false);

        if (player != null)
            player.UnlockMovement();

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
        textoAviso.text = avisoEntrada;
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

    void MostrarAviso(bool visible)
    {
        MostrarAviso(visible, avisoEntrada);
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

    IEnumerator AvisoFinal()
    {
        MostrarAviso(true, avisoFinal);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, avisoFinalDuracion));
        MostrarAviso(false);
    }

    public void OnChaseWon()
    {
        if (!zonaActiva) return;
        zonaActiva = false;
        zonaGanada = true;
        if (player != null)
            player.RestoreMoveSpeed();
        if (hand != null)
            hand.gameObject.SetActive(false);
        triggerArmado = false;
        StartCoroutine(AvisoFinal());
    }

    public void OnPlayerCaught()
    {
        if (!zonaActiva) return;
        if (player != null) player.SonarMuerte(true);
        StartCoroutine(SecuenciaPerdiste());
    }

    IEnumerator SecuenciaPerdiste()
    {
        zonaActiva = false;
        if (pantallaPerdiste != null)
            yield return pantallaPerdiste.MostrarDurante(loseScreenDuration);
        else
            yield return new WaitForSecondsRealtime(loseScreenDuration);

        if (player != null)
            player.VolverAlCheckPoint();

        ResetZone();
    }

    // Al volver al checkpoint la persecucion queda lista para empezar de nuevo, aunque ya se haya ganado.
    public void ReiniciarPorCheckpoint()
    {
        StopAllCoroutines();
        MostrarAviso(false);
        zonaGanada = false;
        ResetZone();
        triggerArmado = true;
    }

    public void ResetZone()
    {
        zonaActiva = false;
        if (player != null)
        {
            player.RestoreMoveSpeed();
            if (player.IsLocked)
                player.UnlockMovement();
        }

        for (int i = 0; i < lanterns.Count; i++)
        {
            if (lanterns[i] != null)
                lanterns[i].ResetLantern();
        }

        if (hand != null)
        {
            hand.GoInactive();
            hand.gameObject.SetActive(false);
        }

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
