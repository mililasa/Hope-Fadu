using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Configuracion de clip Foley")]
public sealed class HopeFoleyClipSettings : ScriptableObject
{
    public AudioClip sonido;
    [Range(0f, 1f)] public float volumenCanal = 1f;
    [Range(0f, 1f)] public float volumenFragmento = 1f;
}
