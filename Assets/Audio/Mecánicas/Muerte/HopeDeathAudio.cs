using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeDeathAudio : MonoBehaviour
{
    public HopeDeathSettings ajustes;
    AudioSource fuente;
    float ultimoDisparo = -10f;
    void Awake()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeDeathSettings>("HopeDeathSettings");
        var objeto = new GameObject("M - Muerte - 8bit Death Whirl");
        objeto.transform.SetParent(transform, false);
        fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
    }
    public void Reproducir(bool contactoMano = false)
    {
        AudioClip clip = ajustes == null ? null : (contactoMano ? ajustes.sonidoMano : ajustes.sonidoCaida);
        float volumen = ajustes == null ? 0f : (contactoMano ? ajustes.volumenMano : ajustes.volumenCaida);
        if (clip == null)
        {
            Debug.LogWarning("Hope: falta el sonido de muerte.", this);
            return;
        }
        if (Time.time - ultimoDisparo < 0.15f) return;
        ultimoDisparo = Time.time;
        // La fuente permanece en Pp al reaparecer: el efecto puede terminar.
        fuente.Stop();
        fuente.volume = volumen;
        fuente.PlayOneShot(clip);
    }
}
