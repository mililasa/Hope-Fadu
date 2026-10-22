using UnityEngine;

[System.Serializable]
public sealed class HopeCorpsePileSound
{
    public string nombre;
    public AudioClip sonido;
    [Range(0f, 2f)] public float volumen = 1f;
}

[System.Serializable]
public sealed class HopeCorpsePileLayer
{
    public string nombre;
    [Range(0f, 2f)] public float volumen = 1f;
    public HopeCorpsePileSound[] sonidos;
}

[CreateAssetMenu(menuName = "Hope/Pila de cadaveres")]
public sealed class HopeCorpsePileSettings : ScriptableObject
{
    [Header("Apoyos al caminar y aterrizar")]
    [Range(0f, 1f)] public float volumenContactos = 0.18f;
    public HopeCorpsePileLayer[] capasContacto;
    [Range(0f, 1f)] public float intensidadCaminata = 0.82f;
    [Min(0.01f)] public float intervaloMinimoContacto = 0.12f;

    [Header("Voces y respiraciones de proximidad")]
    [Range(0f, 1f)] public float volumenRespiraciones = 0.16f;
    public HopeCorpsePileSound[] respiraciones;
    [Min(0.1f)] public float distanciaInicio = 6f;
    [Min(0f)] public float distanciaPlena = 1.5f;
    [Min(0.01f)] public float fadeEntrada = 1.1f;
    [Min(0.01f)] public float fadeSalida = 1.3f;
    [Min(0.2f)] public float intervaloVozMin = 1.2f;
    [Min(0.2f)] public float intervaloVozMax = 2.1f;
    [Range(0f, 0.8f)] public float panoramaMaximo = 0.45f;

    [Header("Superficie de la pila en el escenario actual")]
    public string nombreColliderSuelo = "Piso8";
    [Tooltip("Limites horizontales de los escalones de la pila, como fraccion del ancho del dibujo.")]
    [Range(0f, 1f)] public float bordeIzquierdo = 0.045f;
    [Range(0f, 1f)] public float bordeDerecho = 0.885f;
}
