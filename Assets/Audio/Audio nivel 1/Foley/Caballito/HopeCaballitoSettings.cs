using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Audio del caballito")]
public sealed class HopeCaballitoSettings : ScriptableObject
{
    public AudioClip[] rechinidos;
    public AudioClip apoyo;
    [Range(0f, 1f)] public float volumen = 0.5f;
    [Range(0f, 1f)] public float volumenApoyo = 0.12f;
    [Min(0f)] public float distanciaPlena = 2f;
    [Min(0.1f)] public float distanciaMaxima = 11f;
    [Range(0f, 0.8f)] public float panoramaMaximo = 0.6f;
    [Tooltip("Segundos del ciclo normal en los que cambia el balanceo. La pausa final queda en silencio.")]
    public float[] momentosNormal = { 0.16667f, 0.5f, 0.83333f, 1.16667f, 1.5f, 1.83333f, 2.16667f, 2.75f };
}
