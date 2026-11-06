using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Configuracion de musica")]
public sealed class HopeMusicSettings : ScriptableObject
{
    public AudioClip musicaMain;
    public AudioClip pasoDeNivel;
    [Range(0f, 1f)] public float volumenMain = 0.19952623f;
    [Range(0f, 1f)] public float volumenPaso = 0.25703958f;
    [Min(0.05f)] public float segundosFundido = 2f;
    [Min(0f)] public float segundosAnticipacion = 2f;
    [Min(0f)] public float inicioPaso = 34.048993f;
    [Min(0f)] public float margenRetirada = 1f;
    public string nombreSalida = "Foreground";
    [Tooltip("Punto de la puerta relativo al objeto del escenario. Unidades locales de Unity.")]
    public Vector2 puntoPuertaLocal = new Vector2(72.4375f, -5.3125f);
}
