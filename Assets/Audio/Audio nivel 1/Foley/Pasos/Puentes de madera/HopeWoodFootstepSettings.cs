using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Pasos de puentes de madera")]
public sealed class HopeWoodFootstepSettings : ScriptableObject
{
    public Sprite frente;
    public AudioClip[] pasos;
    public Vector4[] zonas;
    [Min(0)] public float toleranciaVerticalPixeles = 18;
    [Range(0, 1)] public float volumenCanal = 0.351f;
    [Range(0, 1)] public float volumenFragmentoMin = 0.194984f;
    [Range(0, 1)] public float volumenFragmentoMax = 0.25704f;
}
