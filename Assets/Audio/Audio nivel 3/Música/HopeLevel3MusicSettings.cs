using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Musica nivel 3")]
public sealed class HopeLevel3MusicSettings : ScriptableObject
{
    public AudioClip pulsoInterior, marimbaInterior, cierreLuz;
    [Range(0, 1)] public float volumenGeneral = .19952623f;
    [Range(0, 2)] public float volumenPulso = 1;
    [Range(0, 2)] public float volumenMarimba = 1;
    [Range(0, 2)] public float volumenCierre = 1;
    [Min(.05f)] public float fundidoPuerta = .7f;
    [Min(.01f)] public float fundidoFinal = .12f;
}
