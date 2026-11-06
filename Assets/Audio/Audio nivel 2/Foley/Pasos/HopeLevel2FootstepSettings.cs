using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Hope/Audio/Pasos nivel 2")]
public sealed class HopeLevel2FootstepSettings : ScriptableObject
{
    public AudioClip[] pasos, pasosCajas;
    [Range(0, 1)] public float volumenCajas = .82f;
    public AudioMixerGroup salida, salidaCajas;
    [Min(.08f)] public float segundosEntrePasos = .42f;
    [Range(0, 1)] public float intensidadMinima = .95f;
    [Range(0, 1)] public float intensidadMaxima = 1;
}
