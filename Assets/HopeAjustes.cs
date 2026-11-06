using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Resolucion, fps y volumenes elegidos en el menu. Se aplican tambien
// si el juego arranca directo en un nivel.
public static class HopeAjustes
{
    static readonly int[] opcionesFps = { 30, 60, 120, 144, 0 };

    static List<Vector2Int> resoluciones;
    static int indiceResolucion;
    static int indiceFps = 1;
    static bool resolucionElegida;
    static bool fpsElegido;
    static bool pantallaCompleta = true;
    static float master = 1f;
    static float musica = 1f;
    static float efectos = 1f;

    struct Marca
    {
        public float disenado;
        public float escrito;
        public bool lista;
    }

    static readonly Dictionary<int, Marca> marcas = new Dictionary<int, Marca>();
    static HopeAjustesVivo vivo;

    public static float Master => master;
    public static float Musica => musica;
    public static float Efectos => efectos;
    public static bool PantallaCompleta => pantallaCompleta;
    public static bool PuedeElegirResolucion => Application.platform != RuntimePlatform.WebGLPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        marcas.Clear();
        vivo = null;
        resoluciones = null;
        resolucionElegida = false;
        fpsElegido = false;
        master = 1f;
        musica = 1f;
        efectos = 1f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CargarAlInicio()
    {
        Cargar();
        if (resolucionElegida) AplicarResolucion();
        if (fpsElegido) AplicarFps();
        AudioListener.volume = master;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AsegurarVivo()
    {
        if (vivo != null) return;
        vivo = Object.FindObjectOfType<HopeAjustesVivo>();
        if (vivo != null) return;
        var go = new GameObject("HopeAjustes");
        Object.DontDestroyOnLoad(go);
        vivo = go.AddComponent<HopeAjustesVivo>();
    }

    public static void Cargar()
    {
        master = PlayerPrefs.GetFloat("hope.audio.master", 1f);
        musica = PlayerPrefs.GetFloat("hope.audio.musica", 1f);
        efectos = PlayerPrefs.GetFloat("hope.audio.efectos", 1f);
        if (PlayerPrefs.HasKey("hope.video.w"))
        {
            resolucionElegida = true;
            pantallaCompleta = PlayerPrefs.GetInt("hope.video.full", 1) == 1;
            AsegurarResoluciones();
            int w = PlayerPrefs.GetInt("hope.video.w");
            int h = PlayerPrefs.GetInt("hope.video.h");
            indiceResolucion = IndiceDe(w, h);
        }
        if (PlayerPrefs.HasKey("hope.video.fps"))
        {
            fpsElegido = true;
            int fps = PlayerPrefs.GetInt("hope.video.fps", 60);
            indiceFps = 1;
            for (int i = 0; i < opcionesFps.Length; i++)
                if (opcionesFps[i] == fps) indiceFps = i;
        }
    }

    public static void PonerMaster(float valor)
    {
        master = Mathf.Clamp01(valor);
        AudioListener.volume = master;
        GuardarAudio();
    }

    public static void PonerMusica(float valor)
    {
        musica = Mathf.Clamp01(valor);
        GuardarAudio();
    }

    public static void PonerEfectos(float valor)
    {
        efectos = Mathf.Clamp01(valor);
        GuardarAudio();
    }

    static void GuardarAudio()
    {
        PlayerPrefs.SetFloat("hope.audio.master", master);
        PlayerPrefs.SetFloat("hope.audio.musica", musica);
        PlayerPrefs.SetFloat("hope.audio.efectos", efectos);
        PlayerPrefs.Save();
    }

    public static string TextoResolucion()
    {
        if (!resolucionElegida) return Screen.width + " x " + Screen.height;
        AsegurarResoluciones();
        Vector2Int r = resoluciones[indiceResolucion];
        return r.x + " x " + r.y;
    }

    public static string TextoFps()
    {
        if (!fpsElegido)
        {
            if (QualitySettings.vSyncCount > 0 && Application.targetFrameRate <= 0) return "VSYNC";
            if (Application.targetFrameRate <= 0) return "SIN LIMITE";
            return Application.targetFrameRate.ToString();
        }
        int fps = opcionesFps[indiceFps];
        return fps <= 0 ? "SIN LIMITE" : fps.ToString();
    }

    public static string TextoPantalla()
    {
        bool completa = resolucionElegida ? pantallaCompleta : Screen.fullScreen;
        return completa ? "SI" : "NO";
    }

    public static void PasoResolucion(int delta)
    {
        if (!PuedeElegirResolucion) return;
        AsegurarResoluciones();
        if (!resolucionElegida)
        {
            resolucionElegida = true;
            pantallaCompleta = Screen.fullScreen;
            indiceResolucion = IndiceDe(Screen.width, Screen.height);
        }
        indiceResolucion = Mod(indiceResolucion + delta, resoluciones.Count);
        AplicarResolucion();
        GuardarVideo();
    }

    public static void PasoFps(int delta)
    {
        if (!fpsElegido)
        {
            fpsElegido = true;
            indiceFps = IndiceFpsDe(Application.targetFrameRate);
        }
        indiceFps = Mod(indiceFps + delta, opcionesFps.Length);
        AplicarFps();
        GuardarVideo();
    }

    public static void AlternarPantallaCompleta()
    {
        if (!PuedeElegirResolucion) return;
        AsegurarResoluciones();
        if (!resolucionElegida)
        {
            resolucionElegida = true;
            indiceResolucion = IndiceDe(Screen.width, Screen.height);
            pantallaCompleta = Screen.fullScreen;
        }
        pantallaCompleta = !pantallaCompleta;
        AplicarResolucion();
        GuardarVideo();
    }

    static void GuardarVideo()
    {
        if (resolucionElegida && resoluciones != null && resoluciones.Count > 0)
        {
            Vector2Int r = resoluciones[indiceResolucion];
            PlayerPrefs.SetInt("hope.video.w", r.x);
            PlayerPrefs.SetInt("hope.video.h", r.y);
            PlayerPrefs.SetInt("hope.video.full", pantallaCompleta ? 1 : 0);
        }
        if (fpsElegido)
            PlayerPrefs.SetInt("hope.video.fps", opcionesFps[indiceFps]);
        PlayerPrefs.Save();
    }

    static void AplicarResolucion()
    {
        if (Application.isEditor) return;
        if (!PuedeElegirResolucion || resoluciones == null || resoluciones.Count == 0) return;
        Vector2Int r = resoluciones[indiceResolucion];
        QualitySettings.antiAliasing = 0;
        FullScreenMode modo = pantallaCompleta ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        if (Screen.width == r.x && Screen.height == r.y && Screen.fullScreenMode == modo) return;
        Screen.SetResolution(r.x, r.y, modo);
    }

    static void AplicarFps()
    {
        int fps = opcionesFps[indiceFps];
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps <= 0 ? -1 : fps;
    }

    static void AsegurarResoluciones()
    {
        if (resoluciones != null) return;
        resoluciones = new List<Vector2Int>();
        var vistas = new HashSet<long>();
        foreach (Resolution r in Screen.resolutions)
        {
            long clave = ((long)r.width << 32) | (uint)r.height;
            if (!vistas.Add(clave)) continue;
            resoluciones.Add(new Vector2Int(r.width, r.height));
        }
        if (resoluciones.Count == 0)
        {
            resoluciones.Add(new Vector2Int(1280, 720));
            resoluciones.Add(new Vector2Int(1600, 900));
            resoluciones.Add(new Vector2Int(1920, 1080));
            resoluciones.Add(new Vector2Int(2560, 1440));
        }
        resoluciones.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
        indiceResolucion = IndiceDe(Screen.width, Screen.height);
    }

    static int IndiceDe(int ancho, int alto)
    {
        AsegurarResoluciones();
        for (int i = 0; i < resoluciones.Count; i++)
            if (resoluciones[i].x == ancho && resoluciones[i].y == alto) return i;
        resoluciones.Add(new Vector2Int(ancho, alto));
        resoluciones.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
        for (int i = 0; i < resoluciones.Count; i++)
            if (resoluciones[i].x == ancho && resoluciones[i].y == alto) return i;
        return 0;
    }

    static int IndiceFpsDe(int actual)
    {
        if (actual <= 0) return 0;
        int mejor = 1;
        int distancia = int.MaxValue;
        for (int i = 0; i < opcionesFps.Length; i++)
        {
            if (opcionesFps[i] <= 0) continue;
            int d = Mathf.Abs(opcionesFps[i] - actual);
            if (d < distancia) { distancia = d; mejor = i; }
        }
        return mejor;
    }

    static int Mod(int valor, int cantidad)
    {
        if (cantidad <= 0) return 0;
        int r = valor % cantidad;
        return r < 0 ? r + cantidad : r;
    }

    public static bool EsMusica(AudioSource fuente)
    {
        if (fuente == null) return false;
        if (ContieneMusica(fuente.gameObject.name)) return true;
        AudioMixerGroup grupo = fuente.outputAudioMixerGroup;
        if (grupo == null) return false;
        if (ContieneMusica(grupo.name)) return true;
        if (grupo.audioMixer != null && ContieneMusica(grupo.audioMixer.name)) return true;
        return false;
    }

    static bool ContieneMusica(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return false;
        string n = texto.ToLowerInvariant()
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
        return n.Contains("musica") || n.Contains("music") || n.Contains("cinematica")
            || n.Contains("pulso") || n.Contains("piano") || n.Contains("marimba");
    }

    public static float Factor(AudioSource fuente)
    {
        return EsMusica(fuente) ? musica : efectos;
    }

    public static float VolumenBase(AudioSource fuente)
    {
        if (fuente == null) return 1f;
        Marca marca;
        if (marcas.TryGetValue(fuente.GetInstanceID(), out marca) && marca.lista &&
            Mathf.Approximately(fuente.volume, marca.escrito))
            return marca.disenado;
        return fuente.volume;
    }

    // One-shot: el volumen se congela al disparar, asi que hay que escalarlo ahi.
    public static void Disparo(AudioSource fuente, AudioClip clip, float escala = 1f)
    {
        if (fuente == null || clip == null) return;
        float antes = fuente.volume;
        fuente.volume = VolumenBase(fuente) * Factor(fuente);
        try { fuente.PlayOneShot(clip, escala); }
        finally { fuente.volume = antes; }
    }

    public static void AplicarVolumenes()
    {
        AudioListener.volume = master;
        AudioSource[] fuentes = Object.FindObjectsOfType<AudioSource>();
        for (int i = 0; i < fuentes.Length; i++)
        {
            AudioSource fuente = fuentes[i];
            if (fuente == null) continue;
            int id = fuente.GetInstanceID();
            Marca marca;
            if (!marcas.TryGetValue(id, out marca)) marca = new Marca();
            if (!marca.lista || !Mathf.Approximately(fuente.volume, marca.escrito))
                marca.disenado = fuente.volume;
            float nuevo = marca.disenado * Factor(fuente);
            fuente.volume = nuevo;
            marca.escrito = nuevo;
            marca.lista = true;
            marcas[id] = marca;
        }
    }
}

[DefaultExecutionOrder(20000)]
public class HopeAjustesVivo : MonoBehaviour
{
    void LateUpdate()
    {
        HopeAjustes.AplicarVolumenes();
    }
}
