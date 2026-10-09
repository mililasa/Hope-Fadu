using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Configuracion de ambientes")]
public sealed class HopeAmbienceSettings : ScriptableObject
{
    public AudioClip viento, presion, arcos;
    [Range(0f,1f)] public float volumenViento = 0.003801903f;
    [Range(0f,1f)] public float volumenPresion = 0.02611521f;
    [Range(0f,1f)] public float volumenArcos = 0.006067156f;
    [Min(0.1f)] public float alcanceArcos = 4f;
    [Min(0.1f)] public float fundidoArcos = 0.6f;
    public string[] nombresArcos = { "arco_de_sombras_frente", "arco_de_sombras_frente 2" };
}
