using UnityEngine;
using UnityEngine.Audio;
[CreateAssetMenu(menuName = "Hope/Exterior e interior del faro")]
public sealed class HopeLighthouseAudioSettings : ScriptableObject
{
    public HopeAmbienceSettings ambientes;
    public HopeDistantSettings guerra;
    public AudioMixerGroup salidaAmbientes, salidaGuerra;
    public HopeLighthouseClipPair[] versionesInterior;
    [Range(-50,0)] public float volumenInteriorAmbientesDb = -18;
    [Range(-50,0)] public float volumenInteriorGuerraDb = -21;
    [Range(200,22000)] public float corteInteriorHz = 850;
    [Range(200,22000)] public float corteExteriorHz = 22000;
    [Min(.05f)] public float fundido = .6f;
}
[System.Serializable]
public sealed class HopeLighthouseClipPair
{
    public AudioClip exterior, interior;
}
