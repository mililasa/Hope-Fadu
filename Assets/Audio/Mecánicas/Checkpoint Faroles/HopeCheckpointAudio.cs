using UnityEngine;
using System.Collections;
[DisallowMultipleComponent]
public sealed class HopeCheckpointAudio : MonoBehaviour
{
    public HopeCheckpointSettings ajustes;
    AudioSource motivo, segundaVoz;
    GameObject luz;
    bool activo;
    void Awake()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeCheckpointSettings>("HopeCheckpointSettings");
        motivo = Fuente("M - Checkpoint Farol (canal 24)");
        segundaVoz = Fuente("M - Checkpoint Farol 2 (canal 25)");
    }
    AudioSource Fuente(string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var fuente = go.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        return fuente;
    }
    public void Activar(GameObject objetivo, bool primerFarol)
    {
        if (activo || objetivo == null) return;
        if (ajustes == null || ajustes.motivo == null || ajustes.segundaVoz == null)
        {
            Debug.LogWarning("Hope: faltan clips o ajustes de checkpoint.", this);
            return;
        }
        luz = objetivo;
        activo = true;
        StartCoroutine(Disparar(motivo, ajustes.motivo, ajustes.volumenMotivo, ajustes.entradaMotivo));
        StartCoroutine(Disparar(segundaVoz, ajustes.segundaVoz, ajustes.volumenSegundaVoz,
            primerFarol ? ajustes.segundaVozPrimerFarol : ajustes.segundaVozSegundoFarol));
    }
    IEnumerator Disparar(AudioSource fuente, AudioClip clip, float volumen, float demora)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, demora));
        if (!activo || luz == null || !luz.activeInHierarchy) yield break;
        fuente.volume = volumen;
        fuente.PlayOneShot(clip);
    }
    void Update()
    {
        if (activo && (luz == null || !luz.activeInHierarchy)) Reiniciar();
    }
    public void Reiniciar()
    {
        StopAllCoroutines();
        activo = false;
        if (motivo != null) motivo.Stop();
        if (segundaVoz != null) segundaVoz.Stop();
    }
    void OnDisable() { Reiniciar(); }
}
