using System;
using UnityEngine;

public sealed class HopeSurfaceMix : ScriptableObject
{
    [Serializable]
    public sealed class Toma
    {
        public AudioClip original, suave, cola;
    }

    [Serializable]
    public sealed class Superficie
    {
        [Range(0f, 1f)] public float ataque = 1f;
        [Range(0f, 1f)] public float reverb;
        public Toma[] tomas;
    }

    public Superficie tierraNivel1, maderaNivel1, apoyosNivel1;
    public Superficie metalNivel2, cajasNivel2, apoyosNivel2;

    static HopeSurfaceMix ajustes;
    static bool cargado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        ajustes = null;
        cargado = false;
    }

    public static void Reproducir(AudioSource fuente, AudioClip clip, float volumen)
    {
        if (fuente == null || clip == null) return;
        string escena = fuente.gameObject.scene.name;
        if (escena == "SampleScene" || escena == "Nivel_2")
        {
            if (!cargado)
            {
                ajustes = Resources.Load<HopeSurfaceMix>("HopeSurfaceMix");
                cargado = true;
            }
            if (ajustes != null && ajustes.Probar(fuente, clip, volumen, escena == "Nivel_2"))
                return;
        }
        fuente.PlayOneShot(clip, volumen);
    }

    bool Probar(AudioSource fuente, AudioClip clip, float volumen, bool nivelDos)
    {
        return nivelDos
            ? ProbarSuperficie(metalNivel2, fuente, clip, volumen) ||
              ProbarSuperficie(cajasNivel2, fuente, clip, volumen) ||
              ProbarSuperficie(apoyosNivel2, fuente, clip, volumen)
            : ProbarSuperficie(tierraNivel1, fuente, clip, volumen) ||
              ProbarSuperficie(maderaNivel1, fuente, clip, volumen) ||
              ProbarSuperficie(apoyosNivel1, fuente, clip, volumen);
    }

    static bool ProbarSuperficie(Superficie superficie, AudioSource fuente, AudioClip clip, float volumen)
    {
        if (superficie == null || superficie.tomas == null) return false;
        foreach (var toma in superficie.tomas)
        {
            if (toma == null || toma.original != clip) continue;
            float ataque = toma.suave != null ? Mathf.Clamp01(superficie.ataque) : 1f;
            if (ataque > 0f) fuente.PlayOneShot(clip, volumen * ataque);
            if (ataque < 1f) fuente.PlayOneShot(toma.suave, volumen * (1f - ataque));
            if (superficie.reverb > 0f && toma.cola != null)
                fuente.PlayOneShot(toma.cola, volumen * superficie.reverb);
            return true;
        }
        return false;
    }
}
