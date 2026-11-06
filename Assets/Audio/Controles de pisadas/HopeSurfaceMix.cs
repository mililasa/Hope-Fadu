using System;
using UnityEngine;

public sealed class HopeSurfaceMix : ScriptableObject
{
    [Serializable]
    public sealed class Toma
    {
        public AudioClip original, suave, cola, seco;
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

    [Serializable]
    public sealed class ControlesFaro
    {
        [Range(0f, 1f)] public float ataqueMetal = 1f;
        [Range(0f, 1f)] public float reverbMetal;
        [Range(0f, 1f)] public float ataqueTierra = 1f;
        [Range(0f, 1f)] public float reverbTierra;
        [Range(0f, 1f)] public float ataquePiedra = .74f;
        [Range(0f, 1f)] public float reverbPiedra = .49f;
        [Range(0f, 1f)] public float ataqueApoyos = 1f;
        [Range(0f, 1f)] public float reverbApoyosExterior = .08f;
        [Range(0f, 1f)] public float reverbApoyosInterior = .65f;
        [HideInInspector] public Toma[] apoyosSuaves;
        [HideInInspector] public Superficie metalSeco;
    }

    public ControlesFaro nivel3;

    public static ControlesFaro Nivel3
    {
        get
        {
            Cargar();
            return ajustes != null ? ajustes.nivel3 : null;
        }
    }

    static void Cargar()
    {
        if (cargado) return;
        ajustes = Resources.Load<HopeSurfaceMix>("HopeSurfaceMix");
        cargado = true;
    }

    static HopeSurfaceMix ajustes;
    static bool cargado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        ajustes = null;
        cargado = false;
    }

    public static void Reproducir(AudioSource fuente, AudioClip clip, float volumen, int nivelMaterial = 0)
    {
        if (fuente == null || clip == null) return;
        string escena = fuente.gameObject.scene.name;
        if (nivelMaterial == 1 || nivelMaterial == 2 || escena == "SampleScene" || escena == "Nivel_2")
        {
            Cargar();
            if (escena == "Nivel_3" && ajustes != null && ajustes.nivel3 != null)
            {
                var faro = ajustes.nivel3;
                bool metal = nivelMaterial == 2;
                var superficie = metal ? faro.metalSeco ?? ajustes.metalNivel2 : ajustes.tierraNivel1;
                if (ProbarSuperficie(superficie, fuente, clip, volumen,
                    metal ? faro.ataqueMetal : faro.ataqueTierra,
                    metal ? faro.reverbMetal : faro.reverbTierra))
                    return;
            }
            if (ajustes != null && ajustes.Probar(fuente, clip, volumen, nivelMaterial == 2 || (nivelMaterial == 0 && escena == "Nivel_2")))
                return;
        }
        HopeAjustes.Disparo(fuente, clip, volumen);
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

    static bool ProbarSuperficie(Superficie superficie, AudioSource fuente, AudioClip clip, float volumen, float? ataqueOverride = null, float? reverbOverride = null)
    {
        if (superficie == null || superficie.tomas == null) return false;
        foreach (var toma in superficie.tomas)
        {
            if (toma == null || toma.original != clip) continue;
            float ataque = toma.suave != null ? Mathf.Clamp01(ataqueOverride ?? superficie.ataque) : 1f;
            if (ataque > 0f) HopeAjustes.Disparo(fuente, toma.seco != null ? toma.seco : clip, volumen * ataque);
            if (ataque < 1f) HopeAjustes.Disparo(fuente, toma.suave, volumen * (1f - ataque));
            float reverb = reverbOverride ?? superficie.reverb;
            if (reverb > 0f && toma.cola != null)
                HopeAjustes.Disparo(fuente, toma.cola, volumen * reverb);
            return true;
        }
        return false;
    }
}
