using UnityEngine;
using UnityEngine.UI;

// Escape/P pausa (pantalla opaca). Cualquier otra tecla reanuda.
public class PausaJuego : MonoBehaviour
{
    public KeyCode teclaPausa = KeyCode.Escape;
    [Range(0.5f, 1f)] public float opacidad = 0.85f;

    bool pausado;
    GameObject pantalla;
    float timeScaleAntes = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCrear()
    {
        if (FindObjectOfType<PausaJuego>() != null) return;
        GameObject go = new GameObject("PausaJuego");
        go.AddComponent<PausaJuego>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        CrearPantalla();
        if (pantalla != null) pantalla.SetActive(false);
    }

    void Update()
    {
        if (!pausado)
        {
            if (Input.GetKeyDown(teclaPausa) || Input.GetKeyDown(KeyCode.P))
                Pausar();
            return;
        }

        if (Input.anyKeyDown && !Input.GetKeyDown(teclaPausa) && !Input.GetKeyDown(KeyCode.P))
            Reanudar();
    }

    void Pausar()
    {
        if (pausado) return;
        pausado = true;
        timeScaleAntes = Time.timeScale;
        Time.timeScale = 0f;
        if (pantalla != null) pantalla.SetActive(true);
    }

    void Reanudar()
    {
        if (!pausado) return;
        pausado = false;
        Time.timeScale = timeScaleAntes > 0.01f ? timeScaleAntes : 1f;
        if (pantalla != null) pantalla.SetActive(false);
    }

    void CrearPantalla()
    {
        if (pantalla != null) return;
        pantalla = new GameObject("PantallaPausa");
        pantalla.transform.SetParent(transform, false);
        Canvas canvas = pantalla.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        CanvasScaler scaler = pantalla.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        pantalla.AddComponent<GraphicRaycaster>();

        GameObject fondoGO = new GameObject("Fondo");
        fondoGO.transform.SetParent(pantalla.transform, false);
        Image fondo = fondoGO.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, opacidad);
        RectTransform fondoRt = fondo.rectTransform;
        fondoRt.anchorMin = Vector2.zero;
        fondoRt.anchorMax = Vector2.one;
        fondoRt.offsetMin = Vector2.zero;
        fondoRt.offsetMax = Vector2.zero;

        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(pantalla.transform, false);
        Text texto = textoGO.AddComponent<Text>();
        texto.text = "PAUSA\n\nTocá cualquier tecla para continuar";
        texto.fontSize = 48;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        texto.font = font;
        RectTransform textoRt = texto.rectTransform;
        textoRt.anchorMin = Vector2.zero;
        textoRt.anchorMax = Vector2.one;
        textoRt.offsetMin = Vector2.zero;
        textoRt.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        if (pausado) Time.timeScale = 1f;
    }
}
