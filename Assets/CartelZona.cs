using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Trigger de zona: muestra una pantalla negra con texto (como NIVEL2) y vuelve al check.
public class CartelZona : MonoBehaviour
{
    [Tooltip("Texto de la pantalla. Para perder: Perdiste")]
    public string textoPantalla = "Perdiste";
    public float segundosVisible = 2f;
    [Tooltip("Si esta tildado, Hope vuelve al check point despues del cartel.")]
    public bool volverAlCheck = true;

    [Tooltip("Si esta destildado, solo se usa para que otras zonas (persecucion) muestren el cartel.")]
    public bool dispararPorTrigger = true;

    GameObject pantalla;
    bool activo;

    public bool EstaVisible
    {
        get { return pantalla != null && pantalla.activeSelf; }
    }

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        CrearPantalla();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!dispararPorTrigger || activo) return;
        PlayerController2D pp = other.GetComponent<PlayerController2D>();
        if (pp == null) pp = other.GetComponentInParent<PlayerController2D>();
        if (pp == null) return;
        StartCoroutine(Mostrar(pp));
    }

    IEnumerator Mostrar(PlayerController2D pp)
    {
        activo = true;
        Rigidbody2D rb = pp.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
        pp.enabled = false;
        if (pantalla != null) pantalla.SetActive(true);

        yield return new WaitForSecondsRealtime(segundosVisible);

        if (pantalla != null) pantalla.SetActive(false);
        if (rb != null) rb.simulated = true;
        pp.enabled = true;
        if (volverAlCheck) pp.VolverAlCheckPoint();
        activo = false;
    }

    void CrearPantalla()
    {
        pantalla = new GameObject("Pantalla_" + textoPantalla);
        Canvas canvas = pantalla.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        pantalla.AddComponent<CanvasScaler>();

        GameObject fondoGO = new GameObject("Fondo");
        fondoGO.transform.SetParent(pantalla.transform, false);
        Image fondo = fondoGO.AddComponent<Image>();
        fondo.color = Color.black;
        RectTransform fondoRt = fondo.rectTransform;
        fondoRt.anchorMin = Vector2.zero;
        fondoRt.anchorMax = Vector2.one;
        fondoRt.offsetMin = Vector2.zero;
        fondoRt.offsetMax = Vector2.zero;

        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(pantalla.transform, false);
        Text texto = textoGO.AddComponent<Text>();
        texto.text = textoPantalla;
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

        pantalla.SetActive(false);
    }

    public void Mostrar()
    {
        if (pantalla != null) pantalla.SetActive(true);
    }

    public void Ocultar()
    {
        if (pantalla != null) pantalla.SetActive(false);
    }

    public IEnumerator MostrarDurante(float duracion)
    {
        Mostrar();
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, duracion));
        Ocultar();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0.1f, 0.15f, 0.35f);
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Vector3 centro = transform.TransformPoint(box.offset);
        Vector3 tam = Vector3.Scale(box.size, transform.lossyScale);
        Gizmos.DrawCube(centro, tam);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(centro, tam);
    }
}
