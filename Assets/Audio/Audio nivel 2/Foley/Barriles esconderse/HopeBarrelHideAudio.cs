using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeBarrelHideAudio : MonoBehaviour
{
    public HopeBarrelHideSettings ajustes;
    AudioSource entrada, salida;
    bool pausado;

    void Awake()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeBarrelHideSettings>("HopeBarrelHideSettings");
        if (ajustes == null || ajustes.entrada == null || ajustes.salida == null)
        { Debug.LogError("Hope: faltan los motivos de entrada/salida del barril.", this); enabled = false; return; }
        entrada = Crear("F - Barril esconderse - Entrada", ajustes.entrada);
        salida = Crear("F - Barril esconderse - Salida", ajustes.salida);
    }
    AudioSource Crear(string nombre, AudioClip clip)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = false; a.clip = clip;
        a.spatialBlend = 0; a.dopplerLevel = 0;
        if (ajustes.salidaFoley != null) a.outputAudioMixerGroup = ajustes.salidaFoley;
        else HopeAudioRouting.Asignar(a, HopeAudioBus.Foley);
        return a;
    }
    public static void Reproducir(Transform barril, bool entrando)
    {
        if (barril == null || barril.gameObject.scene.name != "Nivel_2") return;
        var audio = barril.GetComponent<HopeBarrelHideAudio>();
        if (audio == null) audio = barril.gameObject.AddComponent<HopeBarrelHideAudio>();
        if (!audio.enabled || audio.ajustes == null) return;
        audio.Detener();
        var a = entrando ? audio.entrada : audio.salida;
        if (a == null) return;
        a.volume = entrando ? audio.ajustes.volumenEntrada : audio.ajustes.volumenSalida;
        a.Play();
    }
    public static void Cancelar(Transform barril)
    {
        if (barril == null) return;
        var audio = barril.GetComponent<HopeBarrelHideAudio>();
        if (audio != null) audio.Detener();
    }
    void Update()
    {
        bool pausa = Time.timeScale == 0;
        if (pausa == pausado) return;
        if (entrada != null) { if (pausa) entrada.Pause(); else entrada.UnPause(); }
        if (salida != null) { if (pausa) salida.Pause(); else salida.UnPause(); }
        pausado = pausa;
    }
    void Detener()
    {
        if (entrada != null) entrada.Stop();
        if (salida != null) salida.Stop();
    }
    void OnDisable() { Detener(); }
}
