using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Despues del menu: la intro, y al terminar un fade in al juego.
public class CinematicaInicio : MonoBehaviour
{
    const string Archivo = "INTRO-VJ_2.mp4";
    const string EscenaJuego = "SampleScene";
    const float SegundosFadeIn = 1.5f;

    static bool enCurso;

    VideoPlayer video;
    RenderTexture textura;
    RawImage imagen;
    bool yendoAlJuego;
    float segundosEnPantalla;

    public static void Reproducir()
    {
        if (enCurso) return;
        enCurso = true;
        var go = new GameObject("CinematicaInicio");
        go.AddComponent<CinematicaInicio>();
    }

    void Start()
    {
        CrearPantalla();
        string ruta = Path.Combine(Application.streamingAssetsPath, Archivo);
        if (!ruta.Contains("://"))
            ruta = "file:///" + ruta.Replace("\\", "/");

        video = gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.isLooping = false;
        video.source = VideoSource.Url;
        video.url = ruta;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.aspectRatio = VideoAspectRatio.FitInside;
        video.audioOutputMode = VideoAudioOutputMode.AudioSource;
        video.waitForFirstFrame = true;
        video.skipOnDrop = false;

        AudioSource voz = gameObject.AddComponent<AudioSource>();
        voz.playOnAwake = false;
        voz.spatialBlend = 0f;
        try
        {
            video.controlledAudioTrackCount = 1;
            video.EnableAudioTrack(0, true);
            video.SetTargetAudioSource(0, voz);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Hope: la intro sigue, sin el audio del video. " + e.Message);
        }

        video.errorReceived += AlFallar;
        video.prepareCompleted += AlListo;
        video.loopPointReached += AlTerminar;
        video.Prepare();
    }

    void Update()
    {
        if (yendoAlJuego || video == null) return;
        segundosEnPantalla += Time.unscaledDeltaTime;
        if (!video.isPlaying && segundosEnPantalla > 8f)
        {
            IrAlJuego();
            return;
        }
        if (video.isPlaying && segundosEnPantalla > 0.4f && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            IrAlJuego();
    }

    void CrearPantalla()
    {
        var lienzo = new GameObject("CinematicaCanvas");
        lienzo.transform.SetParent(transform, false);
        var canvas = lienzo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        var escala = lienzo.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);

        var negro = new GameObject("Negro");
        negro.transform.SetParent(lienzo.transform, false);
        var fondo = negro.AddComponent<Image>();
        fondo.color = Color.black;
        Estirar(fondo.rectTransform);

        var cuadro = new GameObject("Video");
        cuadro.transform.SetParent(lienzo.transform, false);
        imagen = cuadro.AddComponent<RawImage>();
        imagen.color = Color.white;
        Estirar(imagen.rectTransform);
        var ajuste = cuadro.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ajuste.aspectRatio = 16f / 9f;
    }

    static void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void AlListo(VideoPlayer vp)
    {
        int ancho = Mathf.Max(16, (int)vp.width);
        int alto = Mathf.Max(16, (int)vp.height);
        textura = new RenderTexture(ancho, alto, 0);
        vp.targetTexture = textura;
        if (imagen != null)
        {
            imagen.texture = textura;
            var ajuste = imagen.GetComponent<AspectRatioFitter>();
            if (ajuste != null && alto > 0)
                ajuste.aspectRatio = (float)ancho / alto;
        }
        vp.Play();
    }

    void AlTerminar(VideoPlayer vp)
    {
        IrAlJuego();
    }

    void AlFallar(VideoPlayer vp, string mensaje)
    {
        Debug.LogWarning("Hope: no se pudo reproducir la intro. " + mensaje);
        IrAlJuego();
    }

    void IrAlJuego()
    {
        if (yendoAlJuego) return;
        yendoAlJuego = true;
        if (video != null) video.Stop();
        FundidoAlJuego.Entrar(EscenaJuego, SegundosFadeIn);
    }

    void OnDestroy()
    {
        if (video != null)
            video.targetTexture = null;
        if (textura != null)
        {
            textura.Release();
            Destroy(textura);
        }
        enCurso = false;
    }
}

public class FundidoAlJuego : MonoBehaviour
{
    string escena;
    float segundos;
    Image negro;

    public static void Entrar(string escena, float segundos)
    {
        var go = new GameObject("FundidoAlJuego");
        DontDestroyOnLoad(go);
        var fundido = go.AddComponent<FundidoAlJuego>();
        fundido.escena = escena;
        fundido.segundos = Mathf.Max(0.2f, segundos);
        fundido.CrearCortina();
        fundido.StartCoroutine(fundido.Abrir());
    }

    void CrearCortina()
    {
        var lienzo = new GameObject("Cortina");
        lienzo.transform.SetParent(transform, false);
        var canvas = lienzo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        var escala = lienzo.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);

        var cuadro = new GameObject("Negro");
        cuadro.transform.SetParent(lienzo.transform, false);
        negro = cuadro.AddComponent<Image>();
        negro.color = Color.black;
        var rect = negro.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    IEnumerator Abrir()
    {
        yield return null;
        SceneManager.LoadScene(escena);
        yield return null;
        float t = 0f;
        while (t < segundos)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / segundos));
            if (negro != null)
                negro.color = new Color(0f, 0f, 0f, 1f - k);
            yield return null;
        }
        Destroy(gameObject);
    }
}
