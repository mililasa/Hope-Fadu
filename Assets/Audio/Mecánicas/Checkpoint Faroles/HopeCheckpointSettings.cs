using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Checkpoint faroles")]
public sealed class HopeCheckpointSettings : ScriptableObject
{
    public AudioClip motivo, segundaVoz;
    [Range(0,1)] public float volumenMotivo = 0.070795f;
    [Range(0,1)] public float volumenSegundaVoz = 0.235294f;
    [Min(0)] public float entradaMotivo = 0.22f;
    [Min(0)] public float segundaVozPrimerFarol = 1.0290774f;
    [Min(0)] public float segundaVozSegundoFarol = 0.6371257f;
}
