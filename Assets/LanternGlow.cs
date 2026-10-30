using UnityEngine;

// Va en el objeto "Oscuridad" (el que tiene la mascara negra).
// Hace latir apenas el charco de luz, como una llama real.
[DefaultExecutionOrder(50)]
public class LanternGlow : MonoBehaviour
{
    [Header("Tamano del halo")]
    [Tooltip("Activado: usa la escala que pusiste en el Transform (la Oscuridad de Hope). Desactivado: usa Escala Base (los faroles).")]
    public bool respetarEscalaDelTransform = true;
    public float escalaBase = 1f;
    [Tooltip("Radio del circulo de luz del centro, como parte del sprite. Es lo que se junta con los otros halos y alumbra el escenario. En Oscuridad_Linterna el dibujo termina en ~0.22.")]
    [Range(0.05f, 1.2f)]
    public float radioDelHalo = 0.22f;
    [Tooltip("Solo faroles: cuanto oscurece su propia mascara. 0 = el farol solo da luz y no tapa el halo de Hope.")]
    [Range(0f, 1f)]
    public float opacidadOscuridad = 0f;
    [Tooltip("Solo faroles: hasta donde llega su oscuridad, en veces el radio del halo. Mas alla se desvanece y deja ver la oscuridad normal de Hope.")]
    [Range(1.05f, 4f)]
    public float alcanceOscuridad = 1.8f;

    [Header("Parpadeo")]
    public float amplitud = 0.05f;   // 0.05 = varia un 5%
    public float velocidad = 4f;     // que tan rapido tiembla

    [Header("Apagar / prender")]
    public bool encendida = true;
    [HideInInspector] public float factorRadio = 1f;
    public float escalaApagada = 0.15f;
    public float suavidad = 4f;

    private float seed;
    private float escalaActual;
    private Vector3 escalaDelTransform;
    SpriteRenderer dibujo;
    SpriteRenderer relleno;
    Material materialRelleno;
    static Sprite spriteNegro;

    void Awake()
    {
        escalaDelTransform = transform.localScale;
        dibujo = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        seed = Random.value * 100f;
        escalaActual = encendida ? escalaBase : escalaApagada;
    }

    void Update()
    {
        float objetivo = encendida ? escalaBase : escalaApagada;
        escalaActual = Mathf.Lerp(escalaActual, objetivo, Time.deltaTime * suavidad);

        float ruido = Mathf.PerlinNoise(seed, Time.time * velocidad); // 0..1
        float factor = 1f + (ruido - 0.5f) * 2f * amplitud;
        if (!encendida) factor = 1f;

        float cierre = Mathf.Clamp(factorRadio, 0.05f, 1f);
        if (respetarEscalaDelTransform)
        {
            float relativo = escalaActual / Mathf.Max(0.0001f, escalaBase);
            Vector3 e = escalaDelTransform * (relativo * factor * cierre);
            transform.localScale = new Vector3(e.x, e.y, escalaDelTransform.z);
            return;
        }

        float s = escalaActual * factor * cierre;
        transform.localScale = new Vector3(s, s, 1f);
    }

    // Podes llamar a esto desde otro script, o probarlo con la tecla L.
    public void Alternar()
    {
        encendida = !encendida;
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.L)) Alternar();
        ActualizarRelleno();
    }

    void OnDestroy()
    {
        if (relleno != null) Destroy(relleno.gameObject);
        if (materialRelleno != null) Destroy(materialRelleno);
    }

    // El sprite de oscuridad es un cuadrado. Al achicarlo se ven los bordes:
    // esta capa queda abajo, negra, y conserva el agujero del centro.
    void ActualizarRelleno()
    {
        bool haceFalta = factorRadio < 0.999f && isActiveAndEnabled;
        if (!haceFalta)
        {
            if (relleno != null) relleno.enabled = false;
            return;
        }

        if (dibujo == null) dibujo = GetComponent<SpriteRenderer>();
        if (dibujo == null) return;
        AsegurarRelleno();
        if (relleno == null) return;

        Camera cam = Camera.main;
        float lado = 40f;
        Vector3 centro = transform.position;
        if (cam != null)
        {
            centro = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);
            if (cam.orthographic)
            {
                float alto = cam.orthographicSize * 2f;
                float ancho = alto * Mathf.Max(1f, cam.aspect);
                lado = Mathf.Max(alto, ancho) * 3f;
            }
        }

        relleno.enabled = true;
        relleno.sortingLayerID = dibujo.sortingLayerID;
        relleno.sortingOrder = dibujo.sortingOrder - 1;
        Transform rt = relleno.transform;
        rt.position = centro;
        rt.localScale = new Vector3(lado, lado, 1f);

        float mitad = Mathf.Max(dibujo.bounds.extents.x, dibujo.bounds.extents.y);
        materialRelleno.SetVector("_Centro", transform.position);
        materialRelleno.SetFloat("_RadioClaro", mitad * 0.10f);
        materialRelleno.SetFloat("_RadioOpaco", mitad * 0.30f);
    }

    void AsegurarRelleno()
    {
        if (relleno != null) return;
        Shader shader = Shader.Find("Hope/OscuridadRelleno");
        if (shader == null) return;

        var objeto = new GameObject("RellenoOscuridad");
        relleno = objeto.AddComponent<SpriteRenderer>();
        relleno.sprite = SpriteNegro();
        materialRelleno = new Material(shader);
        relleno.sharedMaterial = materialRelleno;
        relleno.color = Color.white;
    }

    static Sprite SpriteNegro()
    {
        if (spriteNegro != null) return spriteNegro;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixeles = new Color[16];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = Color.white;
        tex.SetPixels(pixeles);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        spriteNegro = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        spriteNegro.name = "RellenoOscuridad";
        return spriteNegro;
    }
}
