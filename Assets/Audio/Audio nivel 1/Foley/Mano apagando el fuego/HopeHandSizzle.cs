using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeHandSizzle : MonoBehaviour
{
    public HopeHandSizzleSettings ajustes;
    AudioSource fuente;
    public static void Reproducir(Component mano)
    {
        if (mano == null || mano.gameObject.scene.name != "SampleScene") return;
        var s = mano.GetComponent<HopeHandSizzle>();
        if (s == null) s = mano.gameObject.AddComponent<HopeHandSizzle>();
        s.Tocar();
    }
    public static void Detener(Component mano)
    {
        var s = mano.GetComponent<HopeHandSizzle>();
        if (s != null && s.fuente != null) s.fuente.Stop();
    }
    void Awake()
    {
        ajustes = Resources.Load<HopeHandSizzleSettings>("HopeHandSizzleSettings");
        var go = new GameObject("F - Mano apagando el fuego - Sizzle");
        go.transform.SetParent(transform, false);
        fuente = go.AddComponent<AudioSource>();
        fuente.playOnAwake = false; fuente.loop = false; fuente.spatialBlend = 0;
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Foley);
    }
    void Update() { if (ajustes != null) fuente.volume = ajustes.volumen; }
    void Tocar()
    {
        if (ajustes == null || ajustes.sonido == null) return;
        fuente.Stop(); fuente.clip = ajustes.sonido; fuente.volume = ajustes.volumen;
        fuente.PlayScheduled(AudioSettings.dspTime + Mathf.Max(.015f, ajustes.retrasoTrasImpacto));
    }
    void OnDisable() { if (fuente != null) fuente.Stop(); }
}
