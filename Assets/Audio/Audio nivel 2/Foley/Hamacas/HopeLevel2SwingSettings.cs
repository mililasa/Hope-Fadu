using UnityEngine;
using UnityEngine.Audio;
[CreateAssetMenu(menuName = "Hope/Audio/Hamacas Nivel 2")]
public sealed class HopeLevel2SwingSettings : ScriptableObject
{
    public AudioClip hamacas;
    public AudioMixerGroup salida;
    public string[] superficies;
    [Min(.001f)] public float fadeEntrada = .04f;
    [Min(.001f)] public float fadeSalida = .75f;
    [Min(.1f)] public float segundosAnticipacion = 1f;
    [Min(0)] public float alturaExtraZona = 3f;
}
