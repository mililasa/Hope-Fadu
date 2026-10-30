using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeHandImpactAudio : MonoBehaviour
{
    public HopeHandImpactSettings ajustes;
    AudioSource descarga, barrido, impacto, temblor;
    Coroutine preparacion;
    bool golpePendiente;

    bool AsegurarFuentes()
    {
        if (ajustes == null)
            ajustes = Resources.Load<HopeHandImpactSettings>("HopeHandImpactSettings");
        if (ajustes == null) return false;
        if (descarga == null)
        {
            descarga = Crear("Descarga - aviso de la mano");
            barrido = Crear("Barrido - bajada de la mano");
            impacto = Crear("Impacto - piedra y madera");
            temblor = Crear("Temblor - rocas y tierra");
        }
        AplicarVolumenes();
        return true;
    }

    AudioSource Crear(string nombre)
    {
        var objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        var fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Mecanicas);
        return fuente;
    }

    void Update() { AplicarVolumenes(); }

    void AplicarVolumenes()
    {
        if (ajustes == null || descarga == null) return;
        descarga.volume = ajustes.volumenGeneral * ajustes.volumenDescarga;
        barrido.volume = ajustes.volumenGeneral * ajustes.volumenBarrido;
        impacto.volume = ajustes.volumenGeneral * ajustes.volumenImpacto;
        temblor.volume = ajustes.volumenGeneral * ajustes.volumenTemblor;
    }

    public void PrepararGolpe(float segundosHastaImpacto)
    {
        Reiniciar();
        if (!isActiveAndEnabled || !AsegurarFuentes()) return;
        golpePendiente = true;
        Reproducir(descarga, ajustes.descarga);
        preparacion = StartCoroutine(Bajar(Mathf.Max(0.02f, segundosHastaImpacto)));
    }

    IEnumerator Bajar(float hastaImpacto)
    {
        float aviso = hastaImpacto * ajustes.fraccionAviso;
        if (aviso > 0f) yield return new WaitForSeconds(aviso);
        if (golpePendiente)
        {
            // The compact clip retains the full original sweep. Fit it to the
            // remaining animation time, so the stone hit lands on contact.
            if (ajustes.barrido != null)
                barrido.pitch = Mathf.Clamp(ajustes.barrido.length / Mathf.Max(0.01f, hastaImpacto - aviso), 0.25f, 3f);
            Reproducir(barrido, ajustes.barrido);
            Reproducir(temblor, ajustes.temblor);
        }
        preparacion = null;
    }

    public void Golpear()
    {
        // Both the animation event and the existing fallback timer can call
        // the hand's hit method. Emit only once per attack.
        if (!golpePendiente) return;
        golpePendiente = false;
        if (preparacion != null) StopCoroutine(preparacion);
        preparacion = null;
        if (barrido != null) barrido.Stop();
        Reproducir(impacto, ajustes.impacto);
        HopeHandSizzle.Reproducir(this);
    }

    static void Reproducir(AudioSource fuente, AudioClip clip)
    {
        if (fuente == null || clip == null) return;
        fuente.clip = clip;
        fuente.Play();
    }

    public void Reiniciar()
    {
        HopeHandSizzle.Detener(this);
        if (preparacion != null) StopCoroutine(preparacion);
        preparacion = null;
        golpePendiente = false;
        if (descarga != null) descarga.Stop();
        if (barrido != null) barrido.Stop();
        if (impacto != null) impacto.Stop();
        if (temblor != null) temblor.Stop();
    }

    void OnDisable() { Reiniciar(); }
}
