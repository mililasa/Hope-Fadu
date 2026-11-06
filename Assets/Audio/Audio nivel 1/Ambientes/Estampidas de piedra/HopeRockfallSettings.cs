using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Estampidas de piedra nivel 1")]
public sealed class HopeRockfallSettings : ScriptableObject
{
    public AudioClip sonido;
    [Range(0, 1)] public float volumen = .18f;
    [Min(0)] public float separacionMinima = 12;
    [Range(0, .8f)] public float panoramaMaximo = .35f;
}
