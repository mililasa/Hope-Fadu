using UnityEngine;

// Ponelo en un sprite del escenario para que reciba la luz de Oscuridad.
// Si no le pones Normal Map, el relieve se arma solo con las luces/sombras
// que ya tiene el dibujo. Si le pones un normal map, usa ese.
[RequireComponent(typeof(SpriteRenderer))]
public class RelieveSprite : MonoBehaviour
{
    [Tooltip("Si esta tildado, este objeto se ilumina cuando pasa Hope.")]
    public bool recibirLuz = true;

    [Tooltip("Normal map opcional. Si esta vacio, el relieve sale del propio sprite.")]
    public Texture2D normalMap;

    [Range(0f, 3f)]
    [Tooltip("0 = plano. 1 = normal. 2 = relieve muy marcado.")]
    public float intensidadRelieve = 1.2f;

    [Range(0.25f, 8f)]
    [Tooltip("Grosor del contorno solo en este objeto. 1 = el global.")]
    public float grosorContorno = 1f;

    static readonly int IdUseBump = Shader.PropertyToID("_UseBumpMap");
    static readonly int IdBump = Shader.PropertyToID("_BumpMap");
    static readonly int IdLocal = Shader.PropertyToID("_RelieveLocal");
    static readonly int IdGrosorLocal = Shader.PropertyToID("_GrosorLocal");

    SpriteRenderer sr;
    MaterialPropertyBlock block;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        block = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (sr == null) return;
        sr.GetPropertyBlock(block);
        block.SetFloat(IdLocal, recibirLuz ? intensidadRelieve : 0f);
        block.SetFloat(IdGrosorLocal, grosorContorno);
        if (normalMap != null)
        {
            block.SetFloat(IdUseBump, 1f);
            block.SetTexture(IdBump, normalMap);
        }
        else
        {
            block.SetFloat(IdUseBump, 0f);
        }
        sr.SetPropertyBlock(block);
    }
}
