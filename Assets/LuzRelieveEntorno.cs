using UnityEngine;

// Va en Oscuridad. Publica la posicion de la falsa luz y le pone el
// material de relieve a los sprites del escenario (o solo a los que
// tengan RelieveSprite, si tildas esa opcion).
public class LuzRelieveEntorno : MonoBehaviour
{
    [Header("Quienes reciben la luz")]
    [Tooltip("Si esta tildado, solo objetos con el script RelieveSprite.")]
    public bool soloObjetosMarcados = false;
    [Tooltip("Capas de sorting del escenario. El halo, Pp y Oscuridad no se tocan.")]
    public string[] capasQueRecibenLuz =
    {
        "Foreground", "Piso", "MidBuildings", "Bridge", "FarBuildings", "Default"
    };
    [Tooltip("La Mano tambien recibe relieve cuando la luz le pega.")]
    public bool incluirMano = true;

    [Header("Luz (sigue a Oscuridad)")]
    [Tooltip("0 = usa el tamano de Oscuridad. Si lo pones a mano, es el radio en unidades.")]
    public float radioManual = 0f;
    [Range(0.15f, 1.2f)]
    [Tooltip("Que parte del recuadro de Oscuridad cuenta como luz (el agujero del centro).")]
    public float radioDelAgujero = 0.55f;
    [Range(0f, 1f)]
    [Tooltip("Que tan oscuro queda el escenario fuera de la luz.")]
    public float oscuridadAmbiente = 0.55f;
    public Color colorAmbiente = new Color(0.22f, 0.1f, 0.38f, 1f);
    public Color colorLuz = new Color(0.82f, 0.74f, 1f, 1f);

    [Header("Relieve")]
    [Range(0f, 3f)]
    [Tooltip("Que tan marcado se ve el volumen. 0 = apagado.")]
    public float relieve = 1.6f;
    [Range(0f, 2f)]
    [Tooltip("Brillo del contorno SOLO del lado que pega la luz.")]
    public float brilloBorde = 0.7f;
    [Range(0.25f, 8f)]
    [Tooltip("Grosor del contorno de relieve. 1 = fino. 3-5 = mas ancho.")]
    public float grosorContorno = 2.2f;
    [Range(0f, 0.6f)]
    [Tooltip("A partir de que tan marcado se considera borde. Mas alto = menos lineas.")]
    public float umbralContorno = 0.04f;
    [Range(0.2f, 4f)]
    [Tooltip("Que tan 'de frente' esta la luz. Mas alto = menos hueco, mas bajo = mas volumen.")]
    public float alturaLuz = 1.1f;
    [Range(0.05f, 1.5f)]
    [Tooltip("Que tan plana queda la superficie. Mas bajo = relieves mas fuertes.")]
    public float planoNormal = 0.28f;
    [Range(0.05f, 1f)]
    public float bordeSuave = 0.5f;

    [Header("Shader")]
    public Shader shaderLuz;
    public Material materialLuz;

    static readonly int IdPos = Shader.PropertyToID("_HopeLightPos");
    static readonly int IdRadio = Shader.PropertyToID("_HopeLightRadius");
    static readonly int IdOscuridad = Shader.PropertyToID("_HopeOscuridad");
    static readonly int IdRelieve = Shader.PropertyToID("_HopeRelieve");
    static readonly int IdSuave = Shader.PropertyToID("_HopeSuave");
    static readonly int IdColor = Shader.PropertyToID("_HopeColorLuz");
    static readonly int IdAmbiente = Shader.PropertyToID("_HopeColorAmbiente");
    static readonly int IdRim = Shader.PropertyToID("_HopeRim");
    static readonly int IdAltura = Shader.PropertyToID("_HopeAlturaLuz");
    static readonly int IdPlano = Shader.PropertyToID("_HopePlano");
    static readonly int IdGrosor = Shader.PropertyToID("_HopeGrosorContorno");
    static readonly int IdUmbral = Shader.PropertyToID("_HopeUmbralContorno");

    SpriteRenderer oscuridad;
    bool materialesAsignados;

    void Awake()
    {
        oscuridad = GetComponent<SpriteRenderer>();
        if (shaderLuz == null)
            shaderLuz = Shader.Find("Hope/SpriteLuzRelieve");
        if (materialLuz == null && shaderLuz != null)
            materialLuz = new Material(shaderLuz);
    }

    void Start()
    {
        AsignarMateriales();
        PublicarLuz();
    }

    void LateUpdate()
    {
        PublicarLuz();
    }

    void AsignarMateriales()
    {
        if (materialesAsignados || materialLuz == null) return;
        materialesAsignados = true;

        SpriteRenderer[] todos = FindObjectsOfType<SpriteRenderer>();
        for (int i = 0; i < todos.Length; i++)
        {
            SpriteRenderer sr = todos[i];
            if (!DebeRecibirLuz(sr)) continue;
            sr.sharedMaterial = materialLuz;
            if (sr.GetComponent<RelieveSprite>() == null)
                sr.gameObject.AddComponent<RelieveSprite>();
        }
    }

    bool DebeRecibirLuz(SpriteRenderer sr)
    {
        if (sr == null || sr == oscuridad) return false;

        Transform t = sr.transform;
        while (t != null)
        {
            string n = t.name;
            if (n == "Pp" || n == "PP ana" || n == "Oscuridad") return false;
            if (n.IndexOf("halo", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            t = t.parent;
        }

        RelieveSprite marca = sr.GetComponent<RelieveSprite>();
        if (soloObjetosMarcados)
        {
            if (incluirMano && EsMano(sr))
                return marca == null || marca.recibirLuz;
            return marca != null && marca.recibirLuz;
        }

        if (marca != null && !marca.recibirLuz)
            return false;

        if (incluirMano && EsMano(sr))
            return true;

        string capa = sr.sortingLayerName;
        if (capasQueRecibenLuz == null || capasQueRecibenLuz.Length == 0)
            return capa != "Oscuridad" && capa != "Sky " && capa != "Sky";

        for (int i = 0; i < capasQueRecibenLuz.Length; i++)
        {
            if (capa == capasQueRecibenLuz[i])
                return true;
        }
        return false;
    }

    bool EsMano(SpriteRenderer sr)
    {
        if (sr == null) return false;
        Transform t = sr.transform;
        while (t != null)
        {
            if (t.name.IndexOf("mano", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            t = t.parent;
        }
        return false;
    }

    void PublicarLuz()
    {
        Vector3 pos = transform.position;
        Shader.SetGlobalVector(IdPos, new Vector4(pos.x, pos.y, 0f, 0f));
        Shader.SetGlobalFloat(IdRadio, RadioActual());
        Shader.SetGlobalFloat(IdOscuridad, oscuridadAmbiente);
        Shader.SetGlobalFloat(IdRelieve, relieve);
        Shader.SetGlobalFloat(IdSuave, bordeSuave);
        Shader.SetGlobalColor(IdColor, colorLuz);
        Shader.SetGlobalColor(IdAmbiente, colorAmbiente);
        Shader.SetGlobalFloat(IdRim, brilloBorde);
        Shader.SetGlobalFloat(IdAltura, alturaLuz);
        Shader.SetGlobalFloat(IdPlano, planoNormal);
        Shader.SetGlobalFloat(IdGrosor, grosorContorno);
        Shader.SetGlobalFloat(IdUmbral, umbralContorno);
    }

    float RadioActual()
    {
        if (radioManual > 0.01f) return radioManual;
        if (oscuridad == null) return 4f;
        Bounds b = oscuridad.bounds;
        float lado = Mathf.Max(b.extents.x, b.extents.y);
        return Mathf.Max(0.8f, lado * radioDelAgujero);
    }
}
