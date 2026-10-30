using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Explosion pintura")]
public sealed class HopeExplosionSettings : ScriptableObject
{
    public AudioClip detonacion;
    public AudioClip energia;
    public AudioClip pintura;
    [Range(0f, 1f)] public float volumenGeneral = 0.7f;
    [Range(0f, 1f)] public float volumenDetonacion = 0.85f;
    [Range(0f, 1f)] public float volumenEnergia = 0.32f;
    [Range(0f, 1f)] public float volumenPintura = 0.4f;
    [Min(0f)] public float distanciaPlena = 4f;
    [Min(0.1f)] public float distanciaMaxima = 18f;
    [Range(0f, 0.8f)] public float panoramaMaximo = 0.5f;
}
