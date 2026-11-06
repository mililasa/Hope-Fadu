using UnityEngine;

public sealed class HopeMenuAudioSettings : ScriptableObject
{
    [Header("Volúmenes")]
    [Range(0f, 1f)] public float volumenChispa = .32f;
    [Range(0f, 1f)] public float volumenChispaJugar = .32f;
    [Range(0f, 1f)] public float volumenCheckpoint = .22f;
    [Range(0f, 1f)] public float volumenGas = .18f;
    [Range(0f, 1f)] public float volumenMusica = .22f;
    [Range(0f, 1f)] public float volumenIntro = 1f;

    [Header("Motivo intermitente de la mano")]
    public AudioClip motivoMano;
    [Range(0f, 1f)] public float volumenMano = .08f;
    [Min(0f)] public float silencioMinimo = 4f;
    [Min(0f)] public float silencioMaximo = 7f;
    [Min(.1f)] public float duracionMotivo = 2.8f;
    [Min(.01f)] public float fundidoMotivo = .4f;
}
