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

    [Tooltip("Si esta tildado, grosor, brillo y umbral son los de este objeto, no los del escenario.")]
    public bool usarParametrosPropios;
    [Tooltip("Si esta tildado, el borde iluminado usa colorContorno en vez del color de la luz.")]
    public bool usarColorContorno;
    public Color colorContorno = new Color(1f, 0.84f, 0.12f, 1f);
    [Range(0f, 3f)]
    public float brilloBorde = 1.15f;
    [Range(0f, 0.6f)]
    public float umbralContorno = 0.035f;

    static readonly int IdUseBump = Shader.PropertyToID("_UseBumpMap");
    static readonly int IdBump = Shader.PropertyToID("_BumpMap");
    static readonly int IdLocal = Shader.PropertyToID("_RelieveLocal");
    static readonly int IdGrosorLocal = Shader.PropertyToID("_GrosorLocal");
    static readonly int IdUsarColor = Shader.PropertyToID("_UsarColorContorno");
    static readonly int IdColorContorno = Shader.PropertyToID("_ColorContorno");
    static readonly int IdPropios = Shader.PropertyToID("_UsarParametrosPropios");
    static readonly int IdRimLocal = Shader.PropertyToID("_RimLocal");
    static readonly int IdUmbralLocal = Shader.PropertyToID("_UmbralLocal");

    SpriteRenderer sr;
    MaterialPropertyBlock block;

    void Awake()
    {
        Asegurar();
    }

    void OnEnable()
    {
        Asegurar();
    }

    void Asegurar()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (block == null) block = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        Asegurar();
        if (sr == null || block == null) return;
        sr.GetPropertyBlock(block);
        block.SetFloat(IdLocal, recibirLuz ? intensidadRelieve : 0f);
        block.SetFloat(IdGrosorLocal, grosorContorno);
        block.SetFloat(IdUsarColor, usarColorContorno ? 1f : 0f);
        block.SetColor(IdColorContorno, colorContorno);
        block.SetFloat(IdPropios, usarParametrosPropios ? 1f : 0f);
        block.SetFloat(IdRimLocal, brilloBorde);
        block.SetFloat(IdUmbralLocal, umbralContorno);
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
