using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Golpe de la mano")]
public sealed class HopeHandImpactSettings : ScriptableObject
{
    public AudioClip descarga;
    public AudioClip barrido;
    public AudioClip impacto;
    public AudioClip temblor;
    [Range(0f, 1f)] public float volumenGeneral = 0.65f;
    [Range(0f, 1f)] public float volumenDescarga = 0.4f;
    [Range(0f, 1f)] public float volumenBarrido = 0.85f;
    [Range(0f, 1f)] public float volumenImpacto = 0.5f;
    [Range(0f, 1f)] public float volumenTemblor = 0.42f;
    [Tooltip("Parte del tiempo previo al golpe reservada al aviso electrico.")]
    [Range(0f, 0.6f)] public float fraccionAviso = 0.2f;
}
