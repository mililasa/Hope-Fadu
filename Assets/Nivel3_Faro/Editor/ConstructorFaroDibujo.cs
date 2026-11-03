using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ============================================================================
// FARO CON TUS DIBUJOS (solo en el editor)
//   Menú HOPE > Nivel 3 > Armar faro con mis dibujos
//   (o el botón violeta en el Inspector de Faro_Nivel3)
//
// Usa los PNG de Assets/Nivel3_Faro/Dibujos:
//   n3_frente_principal_exterior.png              torre de afuera + fondo
//   n3_frente_principal_exterior_plataformas.png  ventanas, balcones, hamacas
//   n3_frente_principal_interior.png              torre de adentro
//   n3_frente_principal_interior_plataformas.png  pisos, escalones, ventanas
// Por ahora las 4 caras del faro usan el mismo dibujo. Cuando dibujes las otras,
// arrastrá cada PNG a la lámina de su pared (Pivote > Laminas).
//
// Cada plataforma dibujada tiene encima una "repisa invisible" (la caja de
// líneas verdes o celestes en la ventana Scene): es lo que Hope pisa.
// Si movés una plataforma en el dibujo, mové también su repisa.
// ============================================================================
public static class ConstructorFaroDibujo
{
    const string CARPETA = "Assets/Nivel3_Faro";
    const string DIBUJOS = CARPETA + "/Dibujos";
    const string PNG_TORRE_AFUERA = "n3_frente_principal_exterior";
    const string PNG_PLAT_AFUERA = "n3_frente_principal_exterior_plataformas";
    const string PNG_TORRE_ADENTRO = "n3_frente_principal_interior";
    const string PNG_PLAT_ADENTRO = "n3_frente_principal_interior_plataformas";

    // ------------------------------------------------------------------
    // MEDIDAS DE TU DIBUJO (en píxeles de los PNG de 1200 x 2520)
    //   97 px = 1 unidad: la puerta de entrada (145 px) mide 1,5 Hopes.
    //   El faro apoya en la fila 2309, su centro está en la columna 767.5,
    //   mide 581 px de ancho abajo y 343 px arriba, y el techo está en la fila 578.
    //   O sea: 6 de ancho abajo, 3.5 arriba y 17.8 de alto.
    //   Para cambiar el tamaño de todo el nivel, cambiá PPU y volvé a armar el faro.
    // ------------------------------------------------------------------
    const float PPU = 97f, CENTRO_X = 767.5f, SUELO_Y = 2309f;
    const float ANCHO_ABAJO_PX = 581f, ANCHO_ARRIBA_PX = 343f, TECHO_Y = 578f;

    // ------------------------------------------------------------------
    // PLATAFORMAS DE TU DIBUJO, en píxeles del PNG (así las podés comparar con tu dibujo):
    //   x desde, x hasta  columnas de la punta izquierda y derecha
    //   y                 fila de la parte de arriba (donde apoya los pies Hope)
    // Se pasan solas a unidades.
    // ------------------------------------------------------------------
    struct Plat { public string nombre; public float x0, x1, y; public bool sinFlecha, ayuda; }
    static Plat P(string n, float x0, float x1, float y, bool sinFlecha = false) { return new Plat { nombre = n, x0 = x0, x1 = x1, y = y, sinFlecha = sinFlecha }; }
    // Repisa de AYUDA: no está en tu dibujo, se ve como un escalón violeta. Borrala cuando la dibujes.
    static Plat Ayuda(string n, float x0, float x1, float y) { return new Plat { nombre = n, x0 = x0, x1 = x1, y = y, ayuda = true }; }

    static readonly Plat[] AFUERA =
    {
        P("Balcon", 704, 1127, 1620),
        P("Alfeizar ventana 1", 608, 671, 1589),
        P("Alfeizar ventana 2", 608, 671, 1301),
        P("Hamaca 1", 787, 867, 1108),
        P("Hamaca 2", 912, 992, 1053),
        P("Balcon de arriba", 426, 1110, 869),
        P("Repisa chica", 688, 752, 805),
        P("Alfeizar ventana 3", 608, 671, 757),
    };
    static readonly Plat[] ADENTRO =
    {
        // Del suelo al piso 1 hay 3.4 y Hope salta 1.8 (3.6 con doble salto justo arriba):
        // este escalón provisorio hace que se pueda subir sin hacer malabares.
        Ayuda("Ayuda (escalon provisorio)", 855, 952, 2154),
        P("Piso 1", 497, 1039, 1982, true),
        P("Escalon 4", 940, 1021, 1734),
        P("Escalon 3", 826, 907, 1717),
        P("Escalon 2", 716, 797, 1676),
        P("Escalon 1", 608, 689, 1638),
        P("Piso 2", 533, 1003, 1424, true),
        P("Repisa ventana tapiada", 608, 688, 1056),
        P("Piso 3", 572, 964, 859, true),
    };
    // Puertas y ventanas abiertas, en píxeles: columna del centro, fila de los pies adentro y afuera
    static readonly (string nombre, PuertaFaro.Tipo tipo, float x, float yAdentro, float yAfuera)[] PUERTAS =
    {
        ("Puerta", PuertaFaro.Tipo.Puerta, 768, 2309, 2309),
        ("Ventana 1", PuertaFaro.Tipo.Ventana, 648, 1638, 1589),   // escalón 1 → alféizar 1
        ("Ventana 2", PuertaFaro.Tipo.Ventana, 648, 1424, 1301),   // piso 2 → alféizar 2
        ("Ventana 3", PuertaFaro.Tipo.Ventana, 648, 859, 757),     // piso 3 → alféizar 3
    };
    static float U(float xPx) { return (xPx - CENTRO_X) / PPU; }      // columna del PNG → unidades desde el centro
    static float Alto(float yPx) { return (SUELO_Y - yPx) / PPU; }    // fila del PNG → altura desde el suelo

    static readonly PiezaFaro.Cara[] CARAS = { PiezaFaro.Cara.Norte, PiezaFaro.Cara.Oeste, PiezaFaro.Cara.Sur, PiezaFaro.Cara.Este };
    static readonly string[] NOMBRES = { "Norte", "Oeste", "Sur", "Este" };

    // ------------------------------------------------------------------
    [MenuItem("HOPE/Nivel 3/Armar faro con mis dibujos")]
    public static void DesdeMenu()
    {
        FaroFez faro = Object.FindObjectOfType<FaroFez>();
        if (faro == null)
        {
            EditorUtility.DisplayDialog("Faro", "Abrí la escena Nivel_3 primero (si no existe, creala con HOPE > Nivel 3 > Crear escena Nivel_3).", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Faro", "Se borra el faro actual (repisas, puertas y paredes) y se arma con tus dibujos. ¿Seguro?", "Armar", "Cancelar"))
            return;
        Construir(faro);
    }

    public static void Construir(FaroFez faro)
    {
        Texture2D torreAfuera = CargarDibujo(PNG_TORRE_AFUERA), platAfuera = CargarDibujo(PNG_PLAT_AFUERA);
        Texture2D torreAdentro = CargarDibujo(PNG_TORRE_ADENTRO), platAdentro = CargarDibujo(PNG_PLAT_ADENTRO);
        if (torreAfuera == null || torreAdentro == null)
        {
            EditorUtility.DisplayDialog("Faro", "No encuentro tus dibujos en " + DIBUJOS + ".\nPoné ahí los 4 PNG (n3_frente_principal_...).", "OK");
            return;
        }

        Undo.RecordObject(faro, "Faro con dibujos");
        // ---- Medidas del faro, sacadas del dibujo ----
        faro.pixelesPorUnidad = PPU;
        faro.centroDibujoX = CENTRO_X;
        faro.sueloDibujoY = SUELO_Y;
        faro.medioAncho = ANCHO_ABAJO_PX * 0.5f / PPU;          // 3
        faro.medioAnchoArriba = ANCHO_ARRIBA_PX * 0.5f / PPU;   // 1.77
        faro.alturaLinterna = (SUELO_Y - TECHO_Y) / PPU;         // 17.8: el techo
        faro.grosorMuro = 0.02f;            // la torre de adentro mide lo mismo que la de afuera en tu dibujo
        // Afuera se camina hasta la punta de la plataforma que más sobresale antes de doblar
        float sale = 1f;
        foreach (Plat p in AFUERA)
            sale = Mathf.Max(sale, Mathf.Max(-U(p.x0), U(p.x1)) - faro.MedioAnchoEn(Alto(p.y)) - faro.grosorMuro * 0.5f);
        faro.salidaRepisa = Mathf.Ceil(sale * 100f) / 100f;
        faro.salidaTecho = 0f;              // en el techo, solo el techo
        faro.puertasDeFrente = true;        // ventanas de adentro y de afuera en el mismo lugar
        faro.empiezaAfuera = true;
        faro.vistaInicial = 0;

        // ---- Materiales ----
        ConstructorFaro.Materiales viejos = ConstructorFaro.PrepararMateriales();   // flecha y haz de luz
        Shader opaco = Shader.Find("Hope/FaroOpaco"), sprite = Shader.Find("Hope/FaroSprite");
        if (opaco == null || sprite == null)
        {
            EditorUtility.DisplayDialog("Faro", "Faltan los shaders Hope/FaroOpaco y Hope/FaroSprite (carpeta Nivel3_Faro/Resources).", "OK");
            return;
        }
        Material mTorreAfuera = Mat("Dibujo torre afuera", opaco, torreAfuera);
        Material mTorreAdentro = Mat("Dibujo torre adentro", opaco, torreAdentro);
        Material mPlatAfuera = platAfuera != null ? Mat("Dibujo plataformas afuera", sprite, platAfuera) : null;
        Material mPlatAdentro = platAdentro != null ? Mat("Dibujo plataformas adentro", sprite, platAdentro) : null;
        Material mFondo = Mat("Dibujo fondo", Shader.Find("Sprites/Default"), torreAfuera);

        // ---- Pivote limpio ----
        faro.transform.position = Vector3.zero;
        foreach (string viejo in new[] { "Pivote", "Haz de luz", "Fondo (dibujo)" })
        {
            Transform t = faro.transform.Find(viejo);
            if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
        }
        foreach (BordeGiroFaro b in faro.GetComponentsInChildren<BordeGiroFaro>(true)) Undo.DestroyObjectImmediate(b.gameObject);

        Transform pivote = new GameObject("Pivote").transform;
        Undo.RegisterCreatedObjectUndo(pivote.gameObject, "Faro con dibujos");
        pivote.SetParent(faro.transform, false);
        pivote.localPosition = new Vector3(0f, 0f, 2f * faro.medioAncho + 2f);   // detrás de Hope
        faro.pivote = pivote;

        // ---- Fondo: cielo, suelo y avión (no gira) ----
        Lamina(faro.transform, "Fondo (dibujo)", LaminaFaro.Capa.Fondo, PiezaFaro.Cara.Ninguna, PiezaFaro.Lado.Afuera, mFondo, 0f, faro);   // adentro no se ve

        // ---- Láminas de las 4 paredes ----
        Transform grupoLaminas = Grupo(pivote, "Laminas");
        for (int w = 0; w < 4; w++)
        {
            Transform g = Grupo(grupoLaminas, "Cara " + NOMBRES[w]);
            Lamina(g, "Torre afuera", LaminaFaro.Capa.Torre, CARAS[w], PiezaFaro.Lado.Afuera, mTorreAfuera, 0f, faro);
            if (mPlatAfuera != null) Lamina(g, "Plataformas afuera", LaminaFaro.Capa.Plataformas, CARAS[w], PiezaFaro.Lado.Afuera, mPlatAfuera, 0.04f, faro);
            Lamina(g, "Torre adentro", LaminaFaro.Capa.Torre, CARAS[w], PiezaFaro.Lado.Adentro, mTorreAdentro, 0f, faro);
            if (mPlatAdentro != null) Lamina(g, "Plataformas adentro", LaminaFaro.Capa.Plataformas, CARAS[w], PiezaFaro.Lado.Adentro, mPlatAdentro, 0.04f, faro);
        }

        // ---- Repisas: invisibles sobre tus plataformas ----
        Transform grupoAfuera = Grupo(pivote, "Repisas afuera"), grupoAdentro = Grupo(pivote, "Repisas adentro");
        for (int w = 0; w < 4; w++)
        {
            Transform ga = Grupo(grupoAfuera, "Cara " + NOMBRES[w]), gd = Grupo(grupoAdentro, "Cara " + NOMBRES[w]);
            foreach (Plat p in AFUERA) Repisa(ga, p, CARAS[w], false, faro);
            // Adentro, lo que se ve al entrar por esta cara está en la pared de enfrente
            foreach (Plat p in ADENTRO) Repisa(gd, p, CARAS[(w + 2) % 4], true, faro);
        }

        // ---- Puertas y ventanas (sin dibujo propio: ya están en tus PNG) ----
        Transform grupoPuertas = Grupo(pivote, "Puertas");
        for (int w = 0; w < 4; w++)
            foreach (var d in PUERTAS)
            {
                GameObject go = new GameObject(d.nombre + " " + NOMBRES[w]);
                go.transform.SetParent(grupoPuertas, false);
                PuertaFaro pf = go.AddComponent<PuertaFaro>();
                pf.tipo = d.tipo;
                pf.pared = CARAS[w];
                pf.posicion = U(d.x);
                pf.altura = Alto(d.yAdentro);
                pf.desnivelAfuera = Alto(d.yAfuera) - Alto(d.yAdentro);
                pf.Ubicar(faro);
            }

        // ---- Haz de luz (se prende al ganar) ----
        float H = faro.alturaLinterna;
        GameObject haz = new GameObject("Haz de luz");
        haz.transform.SetParent(faro.transform, false);
        haz.transform.localPosition = new Vector3(0f, H + 2.2f, pivote.localPosition.z);
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Haz";
            q.transform.SetParent(haz.transform, false);
            q.transform.localPosition = new Vector3(s * 23f, 0f, 0f);
            q.transform.localScale = new Vector3(46f * s, 7f, 1f);
            q.GetComponent<MeshRenderer>().sharedMaterial = viejos.haz;
        }
        haz.SetActive(false);
        faro.hazDeLuz = haz;
        faro.materialFlecha = viejos.flecha;

        ConstructorFaro.UbicarEscena(faro, -1f);   // Hope frente a la puerta, el farol en el techo, la cámara
        // La cámara sigue a Hope para arriba y abajo; de costado queda centrada en el faro
        // (si el faro fuera más ancho que la pantalla, la seguiría hasta los bordes donde se dobla)
        CameraFollow cf = Object.FindObjectOfType<CameraFollow>();
        if (cf != null)
        {
            float borde = faro.Borde(false, 0f) + 1f;
            cf.minX = -borde; cf.maxX = borde;
            cf.maxY = faro.alturaLinterna + 6f;
            EditorUtility.SetDirty(cf);
        }
        EditorUtility.SetDirty(faro);
        EditorSceneManager.MarkSceneDirty(faro.gameObject.scene);
        Selection.activeGameObject = faro.gameObject;
        Debug.Log("Faro armado con tus dibujos. Repisas invisibles: cajas verdes (adentro) y celestes (afuera) en la ventana Scene.");
    }

    // ------------------------------------------------------------------
    static Transform Grupo(Transform padre, string nombre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        return g.transform;
    }

    static void Lamina(Transform padre, string nombre, LaminaFaro.Capa capa, PiezaFaro.Cara pared, PiezaFaro.Lado lado, Material m, float separacion, FaroFez faro)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = m;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        LaminaFaro l = go.AddComponent<LaminaFaro>();
        l.capa = capa;
        l.pared = pared;
        l.lado = lado;
        l.separacion = separacion;
        l.Armar(faro);
    }

    static void Repisa(Transform padre, Plat p, PiezaFaro.Cara pared, bool adentro, FaroFez faro)
    {
        GameObject go;
        if (p.ayuda)
        {
            // Visible: un cubo del violeta de tus escalones
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialAyuda();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.name = p.nombre;
        }
        else go = new GameObject(p.nombre);   // invisible: el dibujo ya está en la lámina
        go.transform.SetParent(padre, false);
        PiezaFaro pz = go.AddComponent<PiezaFaro>();
        pz.tipo = PiezaFaro.Tipo.Repisa;
        pz.lado = adentro ? PiezaFaro.Lado.Adentro : PiezaFaro.Lado.Afuera;
        pz.pared = pared;
        pz.sinFlecha = p.sinFlecha;
        pz.desde = Redondear(U(p.x0)); pz.hasta = Redondear(U(p.x1)); pz.altura = Redondear(Alto(p.y));
        pz.grosor = p.ayuda ? 0.2f : 0.2f;
        pz.Ubicar(faro);
    }

    static Material MaterialAyuda()
    {
        Material m = Mat("Repisa ayuda", Shader.Find("Unlit/Color"), null);
        m.color = new Color32(79, 69, 135, 255);
        EditorUtility.SetDirty(m);
        return m;
    }
    static float Redondear(float v) { return Mathf.Round(v * 100f) / 100f; }

    // Carga un PNG de la carpeta Dibujos y lo deja bien importado (sin achicar, sin borronear)
    static Texture2D CargarDibujo(string nombre)
    {
        string ruta = DIBUJOS + "/" + nombre + ".png";
        if (!File.Exists(ruta)) return null;
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp != null)
        {
            bool cambio = imp.textureType != TextureImporterType.Default || imp.npotScale != TextureImporterNPOTScale.None ||
                          imp.maxTextureSize < 4096 || imp.mipmapEnabled || imp.wrapMode != TextureWrapMode.Clamp ||
                          imp.filterMode != FilterMode.Point || imp.textureCompression != TextureImporterCompression.Uncompressed;
            if (cambio)
            {
                imp.textureType = TextureImporterType.Default;
                imp.npotScale = TextureImporterNPOTScale.None;    // no la lleva a 1024 x 2048
                imp.maxTextureSize = 4096;                        // tus PNG miden 2520 de alto
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;             // los bordes del fondo se estiran
                imp.filterMode = FilterMode.Point;                // pixeles nítidos
                imp.alphaIsTransparency = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static Material Mat(string nombre, Shader shader, Texture2D tex)
    {
        Directory.CreateDirectory(CARPETA + "/Materiales");
        string ruta = CARPETA + "/Materiales/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, ruta); }
        m.shader = shader;
        if (tex != null && m.HasProperty("_MainTex")) m.mainTexture = tex;
        if (m.HasProperty("_Color")) m.color = Color.white;
        EditorUtility.SetDirty(m);
        return m;
    }
}

// Todo PNG que pongas en Assets/Nivel3_Faro/Dibujos se importa listo para el faro:
// tamaño original (sin achicar a 2048 ni llevarlo a potencia de 2), pixeles nítidos y bordes estirados.
public class DibujosFaroImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Nivel3_Faro/Dibujos/")) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Default;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.maxTextureSize = 4096;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.filterMode = FilterMode.Point;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
