using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Muerte melancolica")]
public sealed class HopeDeathNarrativeSettings : ScriptableObject
{
    public AudioClip piano, soplo, lampara, entradaMano, entradaCaida;
    public AudioClip pulsoRoto, electricidadMano;
    public AudioClip pianoRoto;
    public AudioClip roturaLampara;
    [Range(0, 1)] public float volumenRoturaLampara = 1;
    [Range(0, 1)] public float volumenPianoRoto = 1;
    [Range(0, 1)] [InspectorName("Volumen Pulso Glitch")] public float volumenPulsoRoto = 1;
    [Range(0, 1)] public float volumenElectricidadMano = 1;
    [Range(0, 1)] public float fondoMusicaMano = .12f;
    [Range(0, 1)] public float volumenGeneral = .24f;
    [Range(0, 1)] public float volumenPiano = 1, volumenSoplo = 1, volumenLampara = 1, volumenEntrada = 1;
    [Range(0, 1)] public float fondoMusica = .32f, fondoAmbientes = .55f;
    [Min(.01f)] public float bajada = .07f;
    [Min(0)] public float sostener = .65f;
    [Min(.01f)] public float recuperacion = 1.2f;
}
