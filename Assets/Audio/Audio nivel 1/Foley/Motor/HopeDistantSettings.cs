using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Motor y eco lejano")]
public sealed class HopeDistantSettings : ScriptableObject
{
    public AudioClip motor, eco;
    [Range(0f,1f)] public float volumenMotor = 0.025119f;
    [Range(0f,1f)] public float volumenEco = 0.044668f;
    public float esperaInicial = 5f;
    public float retrasoEco = 5.15f;
    [Min(0f)] public float intervaloMinimo = 10f;
    [Min(0f)] public float intervaloMaximo = 20f;
}
