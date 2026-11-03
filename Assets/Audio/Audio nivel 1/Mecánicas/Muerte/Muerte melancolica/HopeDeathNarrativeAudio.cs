using UnityEngine;
using UnityEngine.SceneManagement;
[DisallowMultipleComponent]
public sealed class HopeDeathNarrativeAudio : MonoBehaviour
{
    static HopeDeathNarrativeAudio instancia;
    public HopeDeathNarrativeSettings ajustes;
    AudioSource[] fuentes;
    float inicio = -100;
    bool pausado, muerteMano;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { instancia = null; }
    void Awake()
    {
        instancia = this;
        ajustes = Resources.Load<HopeDeathNarrativeSettings>("HopeDeathNarrativeSettings");
        fuentes = new AudioSource[6];
        string[] nombres = { "Muerte caida - piano", "Muerte caida - soplo", "Muerte caida - lampara", "Muerte caida - golpe suave", "Muerte mano - pulso desarmado", "Muerte mano - electricidad" };
        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject(nombres[i]); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.loop = false; s.spatialBlend = 0;
            HopeAudioRouting.Asignar(s, HopeAudioBus.Mecanicas); fuentes[i] = s;
        }
    }
    public void Reproducir(bool mano)
    {
        if (ajustes == null || Time.time - inicio < .15f) return;
        inicio = Time.time;
        muerteMano = mano;
        AudioClip[] clips = { mano ? null : ajustes.piano, mano ? null : ajustes.soplo, mano ? null : ajustes.lampara, mano ? null : ajustes.entradaMano, mano ? ajustes.pulsoRoto : null, mano ? ajustes.electricidadMano : null };
        double cuando = AudioSettings.dspTime + .015;
        for (int i = 0; i < 6; i++)
        {
            fuentes[i].Stop(); fuentes[i].clip = clips[i]; fuentes[i].volume = Volumen(i);
            if (clips[i] != null) fuentes[i].PlayScheduled(cuando);
        }
    }
    float Volumen(int i)
    {
        float v = i == 0 ? ajustes.volumenPiano : i == 1 ? ajustes.volumenSoplo : i == 2 ? ajustes.volumenLampara : i == 3 ? ajustes.volumenEntrada : i == 4 ? ajustes.volumenPulsoRoto : ajustes.volumenElectricidadMano;
        return ajustes.volumenGeneral * v;
    }
    void Update()
    {
        if (ajustes == null) return;
        bool pausa = Time.timeScale <= 0;
        if (pausa != pausado)
        {
            foreach (var f in fuentes) { if (pausa) f.Pause(); else f.UnPause(); }
            pausado = pausa;
        }
        for (int i = 0; i < fuentes.Length; i++) fuentes[i].volume = Volumen(i);
    }
    public static float GananciaFondo(Scene escena, bool musica)
    {
        var t = instancia;
        if (t == null || t.ajustes == null || escena.name != "SampleScene" || t.gameObject.scene != escena) return 1;
        var a = t.ajustes;
        float dt = Time.time - t.inicio;
        float ataque = Mathf.Max(.01f, a.bajada), salida = Mathf.Max(.01f, a.recuperacion);
        float cantidad = dt < ataque ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(dt / ataque))
            : 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((dt - ataque - a.sostener) / salida));
        return Mathf.Lerp(1, musica ? (t.muerteMano ? a.fondoMusicaMano : a.fondoMusica) : a.fondoAmbientes, cantidad);
    }
    void OnDisable()
    {
        if (fuentes != null) foreach (var f in fuentes) if (f != null) f.Stop();
        if (instancia == this) instancia = null;
    }
}
