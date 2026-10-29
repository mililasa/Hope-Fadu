using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum HopeAudioBus
{
    Ambientes,
    Foley,
    Mecanicas,
    Musica
}

public static class HopeAudioRouting
{
    static readonly Dictionary<HopeAudioBus, AudioMixerGroup> grupos = new Dictionary<HopeAudioBus, AudioMixerGroup>();
    static readonly HashSet<HopeAudioBus> avisados = new HashSet<HopeAudioBus>();

    public static void Asignar(AudioSource fuente, HopeAudioBus bus)
    {
        if (fuente == null) return;
        if (!grupos.TryGetValue(bus, out AudioMixerGroup grupo) || grupo == null)
        {
            string nombre = "HopeMixer" + bus.ToString();
            AudioMixer mixer = Resources.Load<AudioMixer>(nombre);
            AudioMixerGroup[] master = mixer != null ? mixer.FindMatchingGroups("Master") : null;
            grupo = master != null && master.Length == 1 ? master[0] : null;
            grupos[bus] = grupo;
        }
        if (grupo != null) fuente.outputAudioMixerGroup = grupo;
        else if (avisados.Add(bus))
            Debug.LogWarning("Hope: no se pudo cargar el submixer para " + bus + ". RevisÃ¡ HopeMixer" + bus + " en Resources.", fuente);
    }
}