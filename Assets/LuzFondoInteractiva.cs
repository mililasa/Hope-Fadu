using UnityEngine;
using UnityEngine.UI;

// Cerca de PrimeraLuz: E activa luzfondo y muestra Oscuridad (1).
// Cerca de SegundaLuz: E activa luzfondo (1).
// Oscuridad (1) empieza oculta y no choca con Oscuridad del personaje.
// No modifica layers ni los objetos, solo los prende.
public class LuzFondoInteractiva : MonoBehaviour
{
    public Transform primeraLuz;
    public GameObject luzfondo;
    public Transform segundaLuz;
    public GameObject luzfondo1;
    public GameObject oscuridad1;
    public float radioInteraccion = 2.5f;
    public string textoPrompt = "E para interactuar";

    private Transform pp;
    private Text prompt;
    private GameObject oscuridadJugador;

    void Start()
    {
        pp = transform;
        if (pp.name != "Pp")
        {
            GameObject player = GameObject.Find("Pp");
            if (player != null) pp = player.transform;
        }

        if (primeraLuz == null)
        {
            GameObject go = GameObject.Find("PrimeraLuz");
            if (go != null) primeraLuz = go.transform;
        }

        if (segundaLuz == null)
        {
            GameObject go = GameObject.Find("SegundaLuz");
            if (go != null) segundaLuz = go.transform;
        }

        if (luzfondo == null) luzfondo = GameObject.Find("luzfondo");
        if (luzfondo1 == null)
        {
            luzfondo1 = GameObject.Find("luzfondo (1)");
            if (luzfondo1 == null) luzfondo1 = GameObject.Find("luzfondo(1)");
        }

        if (oscuridad1 == null)
        {
            oscuridad1 = GameObject.Find("Oscuridad (1)");
            if (oscuridad1 == null) oscuridad1 = GameObject.Find("Oscuridad(1)");
        }

        GameObject ppGO = GameObject.Find("Pp");
        if (ppGO != null)
        {
            Transform t = ppGO.transform.Find("Oscuridad");
            if (t != null) oscuridadJugador = t.gameObject;
        }
        if (oscuridadJugador == null) oscuridadJugador = GameObject.Find("Oscuridad");

        if (luzfondo != null) luzfondo.SetActive(false);
        if (luzfondo1 != null) luzfondo1.SetActive(false);
        if (oscuridad1 != null)
        {
            oscuridad1.SetActive(false);
            HacerNoColisionable(oscuridad1);
        }

        if (oscuridadJugador != null && oscuridadJugador.GetComponent<OscuridadFusion>() == null)
        {
            OscuridadFusion fusion = oscuridadJugador.AddComponent<OscuridadFusion>();
            if (oscuridad1 != null)
                fusion.Configurar(oscuridad1.GetComponent<SpriteRenderer>());
        }

        CrearPrompt();
    }

    void Update()
    {
        if (pp == null) return;

        GameObject objetivo = null;
        if (luzfondo != null && !luzfondo.activeSelf && Cerca(primeraLuz))
            objetivo = luzfondo;
        else if (luzfondo1 != null && !luzfondo1.activeSelf && Cerca(segundaLuz))
            objetivo = luzfondo1;

        if (prompt != null)
            prompt.gameObject.SetActive(objetivo != null);

        if (objetivo != null && Input.GetKeyDown(KeyCode.E))
            Activar(objetivo);
    }

    bool Cerca(Transform zona)
    {
        if (zona == null) return false;
        return Vector2.Distance(pp.position, zona.position) <= radioInteraccion;
    }

    public void Reiniciar()
    {
        if (luzfondo != null) luzfondo.SetActive(false);
        if (luzfondo1 != null) luzfondo1.SetActive(false);
        if (oscuridad1 != null) oscuridad1.SetActive(false);
        if (prompt != null) prompt.gameObject.SetActive(false);
    }

    void Activar(GameObject luz)
    {
        luz.SetActive(true);

        if (luz == luzfondo)
        {
            if (oscuridad1 != null)
            {
                oscuridad1.SetActive(true);
                HacerNoColisionable(oscuridad1);
                IgnorarChoqueConOscuridad();
            }

            HandChaser mano = FindObjectOfType<HandChaser>();
            if (mano != null) mano.RetractarPorLuz();
        }
    }

    void HacerNoColisionable(GameObject go)
    {
        if (go == null) return;
        Collider2D[] cols = go.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].isTrigger = true;
    }

    void IgnorarChoqueConOscuridad()
    {
        if (oscuridad1 == null || oscuridadJugador == null) return;

        Collider2D[] a = oscuridad1.GetComponentsInChildren<Collider2D>(true);
        Collider2D[] b = oscuridadJugador.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < a.Length; i++)
        {
            for (int j = 0; j < b.Length; j++)
            {
                if (a[i] != null && b[j] != null)
                    Physics2D.IgnoreCollision(a[i], b[j], true);
            }
        }
    }

    void CrearPrompt()
    {
        GameObject canvasGO = new GameObject("CanvasPromptE");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject textGO = new GameObject("TextoE");
        textGO.transform.SetParent(canvasGO.transform, false);

        prompt = textGO.AddComponent<Text>();
        prompt.text = textoPrompt;
        prompt.fontSize = 42;
        prompt.alignment = TextAnchor.LowerCenter;
        prompt.color = new Color(1f, 0.92f, 0.65f, 1f);
        prompt.horizontalOverflow = HorizontalWrapMode.Overflow;
        prompt.verticalOverflow = VerticalWrapMode.Overflow;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        prompt.font = font;

        RectTransform rt = prompt.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 48f);
        rt.sizeDelta = new Vector2(800f, 80f);
        prompt.gameObject.SetActive(false);
    }
}
