using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Configuracion de clip Foley")]
public sealed class HopeFoleyClipSettings : ScriptableObject
{
    public bool activarCapa = true;
    public AudioClip sonido;
    public AudioClip[] variaciones;
    [Range(0f, 1f)] public float volumenCanal = 1f;
    [Range(0f, 1f)] public float volumenFragmento = 1f;
    public AudioClip[] reverbsApoyo;
    [Range(0f, 1f)] public float reverbApoyoExterior = .08f;
    [Range(0f, 1f)] public float reverbApoyoInterior = .65f;
}
