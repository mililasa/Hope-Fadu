using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeSpringAudio : MonoBehaviour
{
    public HopeSpringSettings ajustes;
    AudioSource pium, metal;
    int ultimoPium = -1, ultimoMetal = -1;
    public static void Impulsar(PlayerController2D jugador)
    {
        if (jugador == null || jugador.gameObject.scene.name != "SampleScene") return;
        var audio = jugador.GetComponent<HopeSpringAudio>();
        if (audio == null) audio = jugador.gameObject.AddComponent<HopeSpringAudio>();
        audio.Reproducir();
    }
    void Awake()
    {
        ajustes = Resources.Load<HopeSpringSettings>("HopeSpringSettings");
        if (ajustes == null) return;
        pium = Crear("F - Plataforma Pium");
        metal = Crear("F - Plataforma Pium Metalico");
    }
    AudioSource Crear(string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false; s.loop = false; s.spatialBlend = 0;
        HopeAudioRouting.Asignar(s, HopeAudioBus.Foley);
        return s;
    }
    static AudioClip Elegir(AudioClip[] clips, ref int ultimo)
    {
        if (clips == null || clips.Length == 0) return null;
        int i = Random.Range(0, clips.Length);
        if (clips.Length > 1 && i == ultimo) i = (i + Random.Range(1, clips.Length)) % clips.Length;
        ultimo = i; return clips[i];
    }
    void Reproducir()
    {
        if (ajustes == null) return;
        var a = Elegir(ajustes.pium, ref ultimoPium);
        var b = Elegir(ajustes.metal, ref ultimoMetal);
        double inicio = AudioSettings.dspTime + 0.015;
        if (a != null) { pium.Stop(); pium.clip = a; pium.volume = ajustes.volumenPium; pium.PlayScheduled(inicio); }
        if (b != null) { metal.Stop(); metal.clip = b; metal.volume = ajustes.volumenMetal; metal.PlayScheduled(inicio + ajustes.retrasoMetal); }
    }
}
