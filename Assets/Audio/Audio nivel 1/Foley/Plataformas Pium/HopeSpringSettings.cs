using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Plataformas Pium")]
public sealed class HopeSpringSettings : ScriptableObject
{
    public AudioClip[] pium;
    public AudioClip[] metal;
    [Range(0, 1)] public float volumenPium = 0.251189f;
    [Range(0, 1)] public float volumenMetal = 0.057f;
    [Min(0)] public float retrasoMetal = 0.035f;
}
