using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
[DefaultExecutionOrder(1500)]
public sealed class HopeLighthouseExteriorAudio : MonoBehaviour
{
    static HopeLighthouseExteriorAudio instancia;
    public HopeLighthouseAudioSettings ajustes;
    FaroFez faro;
    float interior;
    readonly List<AudioLowPassFilter> filtros = new List<AudioLowPassFilter>();
#if UNITY_WEBGL && !UNITY_EDITOR
    sealed class VozWeb
    {
        public AudioSource original, pared;
        public float volumenBase, ultimaSalida = -1;
        public bool guerra, pausa;
        public int muestraAnterior;
    }
    readonly List<VozWeb> vocesWeb = new List<VozWeb>();
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { instancia = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar() { SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar; }
    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "Nivel_3") return;
        foreach (var raiz in escena.GetRootGameObjects())
            if (raiz.GetComponentInChildren<HopeLighthouseExteriorAudio>(true) != null) return;
        var go = new GameObject("Audio nivel 3 - Exterior del faro");
        SceneManager.MoveGameObjectToScene(go, escena);
        var controlador = go.AddComponent<HopeLighthouseExteriorAudio>();
        if (controlador.ajustes == null) return;
        foreach (var raiz in escena.GetRootGameObjects())
        {
            foreach (var a in raiz.GetComponentsInChildren<HopeLevelAmbience>(true)) a.ajustes = controlador.ajustes.ambientes;
            foreach (var g in raiz.GetComponentsInChildren<HopeDistantSequence>(true)) g.ajustes = controlador.ajustes.guerra;
        }
    }
    void Awake()
    {
        instancia = this;
        ajustes = Resources.Load<HopeLighthouseAudioSettings>("HopeLighthouseAudioSettings");
        foreach (var f in FindObjectsOfType<FaroFez>())
            if (f.gameObject.scene == gameObject.scene) { faro = f; break; }
    }
    void Start()
    {
        if (ajustes == null || faro == null || ajustes.salidaAmbientes == null || ajustes.salidaGuerra == null)
        { Debug.LogError("Hope: falta configurar el audio exterior/interior del faro.", this); enabled = false; return; }
        interior = faro.MezclaInteriorAudio;
        foreach (var s in FindObjectsOfType<AudioSource>()) Registrar(s);
        Aplicar();
    }
    public static void Registrar(AudioSource fuente)
    {
        var c = instancia;
        if (c == null || c.ajustes == null || fuente == null || fuente.gameObject.scene != c.gameObject.scene || fuente.gameObject.scene.name != "Nivel_3") return;
        if (fuente.name.StartsWith("Interior amortiguado - ")) return;
        // Player components share a parent: classify the actual source, not every
        // child of that parent, so personal Foley and music remain unfiltered.
        bool guerra = (fuente.name == "F - Motor (canal 17)" || fuente.name == "F - Eco lejano de posguerra (canal 18)")
            && fuente.GetComponentInParent<HopeDistantSequence>() != null;
        bool ambienteBase = (fuente.name == "A - Viento" || fuente.name == "A - Presion de oscuridad" || fuente.name == "A - Viento Intenso (arcos)")
            && fuente.GetComponentInParent<HopeLevelAmbience>() != null;
        bool ambiente = ambienteBase || fuente.GetComponent<HopeRockfallAudio>() != null
            || fuente.GetComponent<HopeHandAnticipationAudio>() != null;
        if (!guerra && !ambiente) return;
        var grupo = guerra ? c.ajustes.salidaGuerra : c.ajustes.salidaAmbientes;
        if (grupo != null) fuente.outputAudioMixerGroup = grupo;
#if UNITY_WEBGL && !UNITY_EDITOR
        foreach (var v in c.vocesWeb) if (v.original == fuente) return;
        var go = new GameObject("Interior amortiguado - " + fuente.name);
        go.transform.SetParent(fuente.transform, false);
        var pared = go.AddComponent<AudioSource>(); pared.playOnAwake = false; pared.volume = 0; pared.spatialBlend = 0;
        pared.outputAudioMixerGroup = grupo;
        c.vocesWeb.Add(new VozWeb { original = fuente, pared = pared, guerra = guerra, volumenBase = fuente.volume });
#else
        var filtro = fuente.GetComponent<AudioLowPassFilter>();
        if (filtro == null) filtro = fuente.gameObject.AddComponent<AudioLowPassFilter>();
        filtro.lowpassResonanceQ = 1;
        filtro.cutoffFrequency = c.Corte();
        if (!c.filtros.Contains(filtro)) c.filtros.Add(filtro);
#endif
    }
    float Corte()
    {
        return Mathf.Exp(Mathf.Lerp(Mathf.Log(Mathf.Max(200,ajustes.corteExteriorHz)), Mathf.Log(Mathf.Max(200,ajustes.corteInteriorHz)), interior));
    }
    void LateUpdate()
    {
        if (faro == null || ajustes == null) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Time.timeScale <= 0 || AudioListener.pause) { ActualizarWeb(true); return; }
#else
        if (Time.timeScale <= 0 || AudioListener.pause) return;
#endif
        interior = Mathf.MoveTowards(interior, faro.MezclaInteriorAudio, Time.deltaTime / Mathf.Max(.05f,ajustes.fundido));
        Aplicar();
    }
    void Aplicar()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ActualizarWeb(false);
#else
        ajustes.salidaAmbientes.audioMixer.SetFloat("ParedExteriorDb", interior * ajustes.volumenInteriorAmbientesDb);
        ajustes.salidaGuerra.audioMixer.SetFloat("ParedExteriorDb", interior * ajustes.volumenInteriorGuerraDb);
        float corte = Corte();
        foreach (var f in filtros) if (f != null) f.cutoffFrequency = corte;
#endif
    }
#if UNITY_WEBGL && !UNITY_EDITOR
    void ActualizarWeb(bool pausa)
    {
        foreach (var v in vocesWeb)
        {
            if (v.original == null || v.pared == null) continue;
            if (pausa) { if (!v.pausa) v.pared.Pause(); v.pausa = true; continue; }
            if (v.pausa) { v.pared.UnPause(); v.pausa = false; }
            if (Mathf.Abs(v.original.volume - v.ultimaSalida) > .000001f) v.volumenBase = v.original.volume;
            AudioClip clip = null;
            if (ajustes.versionesInterior != null)
                foreach (var par in ajustes.versionesInterior) if (par.exterior == v.original.clip) { clip = par.interior; break; }
            if (v.pared.clip != clip) { v.pared.Stop(); v.pared.clip = clip; }
            v.pared.loop = v.original.loop; v.pared.pitch = v.original.pitch;
            v.pared.panStereo = v.original.panStereo * .25f;
            if (clip != null && v.original.isPlaying)
            {
                int muestra = v.original.timeSamples;
                if (!v.pared.isPlaying || muestra + 4096 < v.muestraAnterior)
                { v.pared.timeSamples = Mathf.Clamp(muestra,0,clip.samples-1); v.pared.Play(); }
                v.muestraAnterior = muestra;
            }
            else if (v.pared.isPlaying) v.pared.Stop();
            if (clip == null) { v.original.volume = v.volumenBase; v.ultimaSalida = v.volumenBase; continue; }
            float atenuacion = Mathf.Pow(10, (v.guerra ? ajustes.volumenInteriorGuerraDb : ajustes.volumenInteriorAmbientesDb) / 20);
            v.original.volume = v.volumenBase * (1-interior); v.ultimaSalida = v.original.volume;
            v.pared.volume = v.volumenBase * interior * atenuacion;
        }
    }
#endif
    void OnDestroy()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ajustes != null)
        {
            if (ajustes.salidaAmbientes != null) ajustes.salidaAmbientes.audioMixer.ClearFloat("ParedExteriorDb");
            if (ajustes.salidaGuerra != null) ajustes.salidaGuerra.audioMixer.ClearFloat("ParedExteriorDb");
        }
#endif
        if (instancia == this) instancia = null;
    }
}
