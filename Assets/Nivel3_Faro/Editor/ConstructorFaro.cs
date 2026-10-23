using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ============================================================================
// CONSTRUCTOR DEL FARO (solo en el editor)
//   Menú HOPE > Nivel 3 > Crear escena Nivel_3
//     Copia Nivel_2 (Hope, cámara, oscuridad y el farol), saca el escenario
//     del nivel 2 y arma el faro con el nivel de ejemplo.
//   Botón "Reconstruir faro de ejemplo" en el Inspector de Faro_Nivel3
//     Vuelve a armar el faro desde cero (se pierden los cambios a mano).
//
// Después de construirlo, el nivel se edita a mano en la escena: las repisas y
// las puertas son objetos comunes adentro de Faro_Nivel3 > Pivote.
// ============================================================================
public static class ConstructorFaro
{
    const string CARPETA = "Assets/Nivel3_Faro";
    const string ESCENA_ORIGEN = "Assets/Scenes/Nivel_2.unity";
    const string ESCENA_NUEVA = "Assets/Scenes/Nivel_3.unity";
    static readonly string[] COSAS_DEL_NIVEL_2 = { "ParallaxRoot", "border", "border (1)", "NIVEL2" };

    // ------------------------------------------------------------------
    // EL NIVEL DE EJEMPLO (el mismo del prototipo web)
    // Cada repisa: pared, adentro/afuera, desde, hasta, altura (+ rota).
    // "desde" y "hasta" se miden de izquierda a derecha como se ve esa pared.
    // ------------------------------------------------------------------
    struct DatoRepisa { public string pared; public bool adentro; public float a, b, y; public bool rota; }
    static DatoRepisa R(string pared, bool adentro, float a, float b, float y, bool rota = false)
    { return new DatoRepisa { pared = pared, adentro = adentro, a = a, b = b, y = y, rota = rota }; }

    static readonly DatoRepisa[] REPISAS =
    {
        // ADENTRO: espiral hacia la derecha desde la entrada
        R("S", true, 1.5f, 3f, 1.5f), R("S", true, 3f, 4f, 3f),
        R("W", true, -4f, -2.5f, 3f), R("W", true, -1f, 1f, 4.5f), R("W", true, 1.5f, 3f, 6f), R("W", true, 3f, 4f, 7.5f),
        R("N", true, -4f, -2.5f, 7.5f), R("N", true, -1f, 1f, 9f), R("N", true, 1.5f, 4f, 10.5f),
        R("E", true, -4f, -2f, 10.5f), R("E", true, -0.5f, 2f, 12f),
        // derrumbe: repisas rotas que no llevan a ningún lado
        R("E", true, 2.6f, 4f, 16.5f, true), R("S", true, -4f, -3f, 15f, true), R("N", true, -3.5f, -2.5f, 18f, true), R("W", true, -1f, 0f, 20f, true),
        // ADENTRO, parte de arriba (después de la ventana)
        R("W", true, 0f, 2.5f, 24f), R("W", true, 3f, 4f, 25.5f),
        R("N", true, -4f, -2.5f, 25.5f), R("N", true, -1.5f, 0.5f, 27f), R("N", true, 1f, 3f, 28.5f),
        // AFUERA: desde la puerta del balcón hasta la ventana
        R("E", false, -2.5f, 0.5f, 12f), R("E", false, 1.5f, 3f, 13.5f), R("E", false, 3.5f, 5f, 15f),
        R("N", false, -5f, -3.5f, 15f), R("N", false, -2f, 0f, 16.5f), R("N", false, 1f, 3f, 18f), R("N", false, 3.5f, 5f, 19.5f),
        R("W", false, -5f, -3.5f, 19.5f), R("W", false, -2f, 0f, 21f), R("W", false, 1f, 2.5f, 22.5f), R("W", false, -2.5f, 0.5f, 24f),
    };
    // Puertas: pared, u medido desde ADENTRO, altura, tipo
    static readonly (string pared, float u, float y, PuertaFaro.Tipo tipo)[] PUERTAS =
    {
        ("S", 0f, 0f, PuertaFaro.Tipo.Puerta),     // la entrada
        ("E", 1f, 12f, PuertaFaro.Tipo.Puerta),    // puerta del balcón (el derrumbe obliga a salir)
        ("W", 1f, 24f, PuertaFaro.Tipo.Ventana),   // ventana para volver a entrar
    };
    static int IndicePared(string p) { return p == "N" ? 0 : p == "W" ? 1 : p == "S" ? 2 : 3; }
    static readonly PiezaFaro.Cara[] CARAS = { PiezaFaro.Cara.Norte, PiezaFaro.Cara.Oeste, PiezaFaro.Cara.Sur, PiezaFaro.Cara.Este };
    static readonly string[] NOMBRES = { "Norte", "Oeste", "Sur", "Este" };
    static readonly Vector3[] NORMAL = { Vector3.forward, Vector3.left, Vector3.back, Vector3.right };

    // ------------------------------------------------------------------
    [MenuItem("HOPE/Nivel 3/Crear escena Nivel_3")]
    public static void CrearEscena()
    {
        if (!File.Exists(ESCENA_ORIGEN))
        {
            EditorUtility.DisplayDialog("Nivel 3", "No encuentro " + ESCENA_ORIGEN, "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ESCENA_NUEVA))
        {
            if (!EditorUtility.DisplayDialog("Nivel 3", "Ya existe Nivel_3. ¿La reemplazo por una nueva?", "Reemplazar", "Cancelar"))
                return;
            AssetDatabase.DeleteAsset(ESCENA_NUEVA);
        }
        AssetDatabase.CopyAsset(ESCENA_ORIGEN, ESCENA_NUEVA);
        var escena = EditorSceneManager.OpenScene(ESCENA_NUEVA, OpenSceneMode.Single);

        // Altura del farol respecto de los pies de Hope en el Nivel_2 (para que quede igual de bien apoyado)
        PlayerController2D pp = Object.FindObjectOfType<PlayerController2D>();
        Lantern farol = Object.FindObjectOfType<Lantern>();
        float alturaFarol = 1.2f;
        if (pp != null && farol != null)
            alturaFarol = Mathf.Clamp(farol.transform.position.y - (pp.transform.position.y - 0.5f), 0.2f, 3f);

        foreach (GameObject raiz in escena.GetRootGameObjects())
            if (COSAS_DEL_NIVEL_2.Contains(raiz.name)) Object.DestroyImmediate(raiz);

        GameObject go = new GameObject("Faro_Nivel3");
        FaroFez faro = go.AddComponent<FaroFez>();
        Construir(faro, alturaFarol);

        AgregarABuildSettings(ESCENA_NUEVA);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Selection.activeGameObject = go;
        EditorUtility.DisplayDialog("Nivel 3",
            "Listo: se creó Nivel_3 con el faro.\n\nDale Play:\n← → mover · Espacio saltar\n↑ entrar o salir por puertas y ventanas\n" +
            "Caminá contra el borde para doblar la esquina\nE prende el farol de arriba", "Dale");
    }

    // ------------------------------------------------------------------
    public static void Construir(FaroFez faro, float alturaFarol = -1f)
    {
        Materiales mats = PrepararMateriales();
        float h = faro.medioAncho, T = faro.grosorMuro, H = faro.alturaLinterna, sale = faro.salidaRepisa;
        float bordeAfuera = h + T * 0.5f + sale;

        // Pivote limpio. Va detrás de Hope (z positivo) para que ella siempre se vea adelante.
        faro.transform.position = Vector3.zero;
        Transform viejo = faro.transform.Find("Pivote");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);
        foreach (BordeGiroFaro b in faro.GetComponentsInChildren<BordeGiroFaro>(true)) Undo.DestroyObjectImmediate(b.gameObject);
        Transform hazViejo = faro.transform.Find("Haz de luz");
        if (hazViejo != null) Undo.DestroyObjectImmediate(hazViejo.gameObject);

        GameObject pivoteGO = new GameObject("Pivote");
        Undo.RegisterCreatedObjectUndo(pivoteGO, "Construir faro");
        Transform pivote = pivoteGO.transform;
        pivote.SetParent(faro.transform, false);
        pivote.localPosition = new Vector3(0f, 0f, 2f * h + 2f);
        faro.pivote = pivote;

        Transform grupoParedes = Grupo(pivote, "Paredes"), grupoPisos = Grupo(pivote, "Pisos y techo");
        Transform grupoAdentro = Grupo(pivote, "Repisas adentro"), grupoAfuera = Grupo(pivote, "Repisas afuera");
        Transform grupoPuertas = Grupo(pivote, "Puertas");

        // ---- Paredes: un cuerpo oscuro y dos caras (adentro y afuera) ----
        for (int w = 0; w < 4; w++)
        {
            Vector3 n = NORMAL[w];
            bool nortesur = w == 0 || w == 2;
            float largo = nortesur ? 2f * h + T : 2f * h - T;
            GameObject pared = new GameObject("Pared " + NOMBRES[w]);
            pared.transform.SetParent(grupoParedes, false);
            pared.transform.localPosition = n * h + Vector3.up * H * 0.5f;
            PiezaFaro pz = pared.AddComponent<PiezaFaro>();
            pz.tipo = PiezaFaro.Tipo.Pared; pz.pared = CARAS[w]; pz.lado = PiezaFaro.Lado.Ambos;

            Cubo(pared.transform, "Cuerpo", Vector3.zero, nortesur ? new Vector3(largo, H, T) : new Vector3(T, H, largo), mats.oscuro, Vector2.zero);
            Cara(pared.transform, "Cara adentro", -n * (T * 0.5f + 0.002f), Quaternion.LookRotation(n), new Vector2(2f * h - T, H), mats.paredAdentro, new Vector2(2f, 2f));
            Cara(pared.transform, "Cara afuera", n * (T * 0.5f + 0.002f), Quaternion.LookRotation(-n), new Vector2(2f * h + T, H), mats.paredAfuera, new Vector2(2f, 2f));
        }

        // ---- Pisos, suelo, mar, balcón y techo ----
        PiezaPiso(Cubo(grupoPisos, "Piso adentro", new Vector3(0, -0.5f, 0), new Vector3(2 * h, 1, 2 * h), mats.piedra, new Vector2(2, 2)), PiezaFaro.Lado.Adentro);
        PiezaPiso(Cubo(grupoPisos, "Suelo afuera", new Vector3(0, -0.52f, 0), new Vector3(2 * h + 10, 1, 2 * h + 10), mats.suelo, new Vector2(2, 2)), PiezaFaro.Lado.Afuera);
        PiezaPiso(Cubo(grupoPisos, "Mar", new Vector3(0, -1.6f, 0), new Vector3(300, 1, 300), mats.mar, Vector2.zero), PiezaFaro.Lado.Afuera);
        PiezaPiso(Cubo(grupoPisos, "Balcon", new Vector3(0, H - 0.2f, 0), new Vector3(2 * bordeAfuera, 0.4f, 2 * bordeAfuera), mats.piedra, new Vector2(2, 0)), PiezaFaro.Lado.Ambos);
        foreach (var (x, z) in new[] { (-2.6f, -2.6f), (2.6f, -2.6f), (-2.6f, 2.6f), (2.6f, 2.6f) })
            PiezaPiso(Cubo(grupoPisos, "Columna", new Vector3(x, H + 1.8f, z), new Vector3(0.25f, 3.6f, 0.25f), mats.oscuro, Vector2.zero), PiezaFaro.Lado.Ambos);
        // techo escalonado (pixelado)
        float[] anchoTecho = { 6.4f, 4.8f, 3.2f, 1.6f };
        for (int i = 0; i < anchoTecho.Length; i++)
            PiezaPiso(Cubo(grupoPisos, "Techo " + (i + 1), new Vector3(0, H + 3.8f + i * 0.6f, 0), new Vector3(anchoTecho[i], 0.6f, anchoTecho[i]), mats.techo, new Vector2(2, 0)), PiezaFaro.Lado.Ambos);

        // ---- Repisas ----
        int n1 = 0;
        foreach (DatoRepisa d in REPISAS)
        {
            int w = IndicePared(d.pared);
            int k = FaroFez.VistaDePared(d.adentro, w);
            float largo = d.b - d.a, grosor = 0.35f;
            Vector3 centro = FaroFez.PuntoDePanel(d.adentro, k, (d.a + d.b) * 0.5f, d.y - grosor * 0.5f, sale * 0.5f, h, T);
            Vector3 tam = (k % 2 == 0) ? new Vector3(largo, grosor, sale) : new Vector3(sale, grosor, largo);
            string nombre = "Repisa " + (++n1) + " " + NOMBRES[w] + (d.rota ? " (rota)" : "");
            GameObject r = Cubo(d.adentro ? grupoAdentro : grupoAfuera, nombre, centro, tam, d.rota ? mats.repisaRota : mats.repisa, new Vector2(2f, 0f));
            PiezaFaro pz = r.AddComponent<PiezaFaro>();
            pz.tipo = PiezaFaro.Tipo.Repisa;
            pz.lado = d.adentro ? PiezaFaro.Lado.Adentro : PiezaFaro.Lado.Afuera;
            pz.pared = CARAS[w];
            pz.rota = d.rota;
            // Su lugar queda guardado en números: el cubo siempre se acomoda a ellos
            pz.desde = d.a; pz.hasta = d.b; pz.altura = d.y; pz.grosor = grosor;
            pz.Ubicar(faro);
        }

        // ---- Puertas y ventanas ----
        foreach (var p in PUERTAS)
        {
            int w = IndicePared(p.pared);
            Vector3 n = NORMAL[w];
            GameObject puerta = new GameObject((p.tipo == PuertaFaro.Tipo.Ventana ? "Ventana " : "Puerta ") + NOMBRES[w]);
            puerta.transform.SetParent(grupoPuertas, false);
            puerta.transform.localPosition = FaroFez.PuntoDePanel(true, w, p.u, p.y, -T * 0.5f, h, T);   // sobre el centro del muro
            PuertaFaro pf = puerta.AddComponent<PuertaFaro>();
            pf.tipo = p.tipo;
            pf.pared = CARAS[w];
            pf.posicion = p.u;
            pf.altura = p.y;
            Material mAdentro = p.tipo == PuertaFaro.Tipo.Ventana ? mats.ventana : mats.puertaAdentro;
            Material mAfuera = p.tipo == PuertaFaro.Tipo.Ventana ? mats.ventana : mats.puertaAfuera;
            pf.caraAdentro = Cara(puerta.transform, "Adentro", -n * (T * 0.5f + 0.02f) + Vector3.up * 0.975f, Quaternion.LookRotation(n), new Vector2(1.3f, 1.95f), mAdentro, Vector2.zero).GetComponent<Renderer>();
            pf.caraAfuera = Cara(puerta.transform, "Afuera", n * (T * 0.5f + 0.02f) + Vector3.up * 0.975f, Quaternion.LookRotation(-n), new Vector2(1.3f, 1.95f), mAfuera, Vector2.zero).GetComponent<Renderer>();
        }


        // ---- Haz de luz (se prende al ganar) ----
        GameObject haz = new GameObject("Haz de luz");
        haz.transform.SetParent(faro.transform, false);
        haz.transform.localPosition = new Vector3(0f, H + 2.2f, pivote.localPosition.z);
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject q = Cara(haz.transform, "Haz", new Vector3(s * 23f, 0f, 0f), Quaternion.identity, new Vector2(46f, 7f), mats.haz, Vector2.zero);
            q.transform.localScale = new Vector3(46f * s, 7f, 1f);   // la punta angosta hacia el farol
        }
        haz.SetActive(false);
        faro.hazDeLuz = haz;
        faro.materialFlecha = mats.flecha;

        UbicarEscena(faro, alturaFarol);
        EditorUtility.SetDirty(faro);
        EditorSceneManager.MarkSceneDirty(faro.gameObject.scene);
    }

    static Transform Grupo(Transform padre, string nombre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        return g.transform;
    }

    static GameObject Cubo(Transform padre, string nombre, Vector3 pos, Vector3 tam, Material m, Vector2 baldosa)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(go.GetComponent<Collider>());   // la física es 2D: los colliders los arma FaroFez
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = tam;
        Preparar(go, m, baldosa);
        return go;
    }

    static GameObject Cara(Transform padre, string nombre, Vector3 pos, Quaternion rot, Vector2 tam, Material m, Vector2 baldosa)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = new Vector3(tam.x, tam.y, 1f);
        Preparar(go, m, baldosa);
        return go;
    }

    static void Preparar(GameObject go, Material m, Vector2 baldosa)
    {
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = m;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        if (baldosa != Vector2.zero)
        {
            TexturaRepetida tr = go.AddComponent<TexturaRepetida>();
            tr.baldosa = baldosa;
            tr.Aplicar();
        }
    }

    static void PiezaPiso(GameObject go, PiezaFaro.Lado lado)
    {
        PiezaFaro p = go.AddComponent<PiezaFaro>();
        p.tipo = PiezaFaro.Tipo.Piso;
        p.lado = lado;
        p.pared = PiezaFaro.Cara.Ninguna;
    }

    // Hope y el checkpoint afuera, frente a la entrada; el farol arriba; la cámara centrada en el faro.
    static void UbicarEscena(FaroFez faro, float alturaFarol)
    {
        float H = faro.alturaLinterna;
        PlayerController2D pp = Object.FindObjectOfType<PlayerController2D>();
        if (pp != null)
        {
            Undo.RecordObject(pp.transform, "Ubicar Hope");
            pp.transform.position = new Vector3(-3f, 0.6f, 0f);
            faro.hope = pp;
        }
        GameObject cp = GameObject.Find("CheckPoint");
        if (cp != null)
        {
            Undo.RecordObject(cp.transform, "Ubicar checkpoint");
            cp.transform.position = new Vector3(-3f, 0.6f, 0f);
        }

        Lantern farol = faro.farolFinal != null ? faro.farolFinal : Object.FindObjectOfType<Lantern>();
        if (farol != null)
        {
            Undo.RecordObject(farol.transform, "Ubicar farol");
            Undo.RecordObject(farol, "Ubicar farol");
            farol.name = "Farol final";
            float altura = alturaFarol > 0f ? alturaFarol : farol.transform.position.y - H;
            if (altura <= 0f || altura > 3f) altura = 1.2f;
            farol.transform.position = new Vector3(0f, H + altura, farol.transform.position.z);
            farol.segundosPrendidoLibre = 0f;
            farol.esCheckpointPermanente = false;
            farol.mostrarCartelEncender = false;
            faro.farolFinal = farol;
            EditorUtility.SetDirty(farol);
        }
        else
            Debug.LogWarning("Faro: no hay ningún farol en la escena. Copiá uno de Nivel_2 y arrastralo a 'Farol Final' en Faro_Nivel3.");

        CameraFollow cf = Object.FindObjectOfType<CameraFollow>();
        if (cf != null)
        {
            Undo.RecordObject(cf, "Ubicar cámara");
            cf.limitarALimites = true;
            cf.minX = -1f; cf.maxX = 1f;          // rango chico = la cámara queda fija en el centro del faro
            cf.minY = -1.5f;
            cf.maxY = H + 6.5f;
            cf.zoom = faro.zoomAfuera;
            EditorUtility.SetDirty(cf);
            Camera cam = cf.GetComponent<Camera>();
            if (cam != null)
            {
                Undo.RecordObject(cam, "Ubicar cámara");
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color32(26, 1, 35, 255);   // violeta noche de HOPE
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, 200f);
            }
        }
    }

    static void AgregarABuildSettings(string ruta)
    {
        List<EditorBuildSettingsScene> escenas = EditorBuildSettings.scenes.ToList();
        if (escenas.Any(s => s.path == ruta)) return;
        escenas.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = escenas.ToArray();
    }

    // ------------------------------------------------------------------
    // TEXTURAS Y MATERIALES (paleta violeta de HOPE)
    // Se crean una sola vez en Assets/Nivel3_Faro/Texturas. Si reemplazás un
    // PNG por tu propio dibujo (mismo nombre), el faro usa el tuyo.
    // ------------------------------------------------------------------
    class Materiales
    {
        public Material paredAdentro, paredAfuera, piedra, suelo, repisa, repisaRota, oscuro, mar, techo;
        public Material puertaAdentro, puertaAfuera, ventana, flecha, haz;
    }

    static Color32 C(string hex) { ColorUtility.TryParseHtmlString(hex, out Color c); return c; }

    static Materiales PrepararMateriales()
    {
        Directory.CreateDirectory(CARPETA + "/Texturas");
        Directory.CreateDirectory(CARPETA + "/Materiales");
        var m = new Materiales();
        m.paredAdentro = Opaco("Pared adentro", Textura("pared_adentro", 64, 64, Ladrillos(C("#351c4f"), C("#19052d"), C("#351c4f"))));
        m.paredAfuera = Opaco("Pared afuera", Textura("pared_afuera", 64, 64, Ladrillos(C("#4f4587"), C("#2b2b64"), C("#664580"))));
        m.piedra = Opaco("Piedra", Textura("piedra", 64, 64, Ladrillos(C("#44295e"), C("#280a37"), C("#44295e"))));
        m.suelo = Opaco("Suelo", Textura("suelo", 64, 64, Ladrillos(C("#2b0937"), C("#19052d"), C("#2b0937"))));
        m.repisa = Opaco("Repisa", Textura("repisa", 32, 16, PintarRepisa));
        m.repisaRota = Opaco("Repisa rota", Textura("repisa_rota", 32, 16, PintarRepisaRota));
        m.oscuro = MatColor("Oscuro", C("#19052d"));
        m.mar = MatColor("Mar", C("#19052d"));
        m.techo = MatColor("Techo", C("#4f4587"));
        m.puertaAdentro = Transparente("Puerta adentro", Textura("puerta_adentro", 16, 24, (x, y) => PintarPuerta(x, y, false)));
        m.puertaAfuera = Transparente("Puerta afuera", Textura("puerta_afuera", 16, 24, (x, y) => PintarPuerta(x, y, true)));
        m.ventana = Transparente("Ventana", Textura("ventana", 16, 24, PintarVentana));
        m.flecha = Transparente("Flecha esquina", Textura("flecha_esquina", 32, 32, PintarFlecha));
        m.haz = Aditivo("Haz de luz", Textura("haz_luz", 64, 16, PintarHaz, TextureWrapMode.Clamp, FilterMode.Bilinear));
        AssetDatabase.SaveAssets();
        return m;
    }

    static Material Guardar(string nombre, Material mat)
    {
        string ruta = CARPETA + "/Materiales/" + nombre + ".mat";
        Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) { Object.DestroyImmediate(mat); return existente; }
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }
    static Material Opaco(string nombre, Texture2D tex) { var m = new Material(Shader.Find("Unlit/Texture")); m.mainTexture = tex; return Guardar(nombre, m); }
    static Material MatColor(string nombre, Color32 c) { var m = new Material(Shader.Find("Unlit/Color")); m.color = c; return Guardar(nombre, m); }
    static Material Transparente(string nombre, Texture2D tex) { var m = new Material(Shader.Find("Sprites/Default")); m.mainTexture = tex; return Guardar(nombre, m); }
    static Material Aditivo(string nombre, Texture2D tex)
    {
        Shader s = Shader.Find("Legacy Shaders/Particles/Additive");
        if (s == null) s = Shader.Find("Sprites/Default");
        var m = new Material(s); m.mainTexture = tex; return Guardar(nombre, m);
    }

    delegate Color32 Pintor(int x, int y);

    // y = 0 es la fila de ARRIBA (como en el prototipo); se da vuelta al guardar
    static Texture2D Textura(string nombre, int w, int h, Pintor pintar, TextureWrapMode wrap = TextureWrapMode.Repeat, FilterMode filtro = FilterMode.Point)
    {
        string ruta = CARPETA + "/Texturas/" + nombre + ".png";
        if (!File.Exists(ruta))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, h - 1 - y, pintar(x, y));
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta);
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.textureType = TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.filterMode = filtro;
            imp.wrapMode = wrap;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static Color32 Variar(Color32 c, int v)
    {
        return new Color32((byte)Mathf.Clamp(c.r + v, 0, 255), (byte)Mathf.Clamp(c.g + v, 0, 255), (byte)Mathf.Clamp(c.b + v, 0, 255), 255);
    }

    // Ladrillos de 30x14 px con junta, 4 filas por textura (un tile = 2 x 2 unidades)
    static Pintor Ladrillos(Color32 arriba, Color32 junta, Color32 abajo)
    {
        var r = new System.Random(arriba.r * 31 + arriba.b);
        var tonos = new Dictionary<int, int>();
        return (x, y) =>
        {
            int fila = y / 16, corrido = (fila % 2) * 16;
            int xx = (x + 64 - corrido) % 64;
            if (y % 16 == 0 || y % 16 == 15 || xx % 32 == 0 || xx % 32 == 31) return junta;
            int clave = fila * 10 + xx / 32;
            if (!tonos.TryGetValue(clave, out int v)) { v = r.Next(-11, 12); tonos[clave] = v; }
            return Variar(fila >= 2 ? abajo : arriba, v);
        };
    }

    static Color32 PintarRepisa(int x, int y)
    {
        if (y == 0) return C("#e8dcff");
        if (y < 3) return C("#b48fd6");
        if (y >= 12) return C("#664580");
        if (x == 15 || x == 31) return C("#7a56a0");
        return C("#966bb8");
    }

    static Color32 PintarRepisaRota(int x, int y)
    {
        int[,] grietas = { { 6, 2, 2, 9 }, { 7, 9, 4, 2 }, { 20, 0, 2, 7 }, { 19, 6, 6, 2 }, { 27, 4, 2, 10 } };
        for (int i = 0; i < grietas.GetLength(0); i++)
            if (x >= grietas[i, 0] && x < grietas[i, 0] + grietas[i, 2] && y >= grietas[i, 1] && y < grietas[i, 1] + grietas[i, 3]) return C("#19052d");
        return y < 2 ? C("#664580") : C("#44295e");
    }

    static readonly Color32 VACIO = new Color32(0, 0, 0, 0);
    static bool Arco(int x, int y) { return (y >= 4) || (y >= 2 && x >= 3 && x <= 12) || (y >= 1 && x >= 5 && x <= 10); }
    static bool Adentro(int x, int y) { return x >= 3 && x <= 12 && (y >= 4 || (y >= 3 && x >= 5 && x <= 10)); }

    static Color32 PintarPuerta(int x, int y, bool afuera)
    {
        if (x < 1 || x > 14 || !Arco(x, y)) return VACIO;
        if (!Adentro(x, y)) return C("#966bb8");                 // marco lavanda
        if (afuera)
        {
            if (x >= 7 && x <= 8) return C("#44295e");          // tablas
            if (x >= 10 && x <= 11 && y >= 14 && y <= 15) return C("#f9dc74");   // picaporte
            return C("#664580");
        }
        if (y >= 22) return C("#5a3a2a");                      // luz cálida que se escapa abajo
        return y >= 20 ? C("#2b0937") : C("#0f0314");
    }

    static Color32 PintarVentana(int x, int y)
    {
        if (x < 1 || x > 14 || !Arco(x, y)) return VACIO;
        if (!Adentro(x, y) || y >= 22) return C("#966bb8");
        if (x == 7 || x == 8 || y == 12 || y == 13) return C("#966bb8");   // cruz del marco
        if ((x == 4 && y == 6) || (x == 11 && y == 16)) return C("#e8dcff"); // estrellitas
        return C("#2b2b64");
    }

    // Flecha curva amarilla con borde oscuro: "esta repisa sigue a la vuelta de la esquina"
    static Color32 PintarFlecha(int x, int y)
    {
        float cx = 15f, cy = 20f;
        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        bool arriba = dy <= 1f && x >= 5 && x <= 25;
        bool enPunta = EnTriangulo(x + 0.5f, y + 0.5f, new Vector2(21f, 15f), new Vector2(30f, 18f), new Vector2(23f, 26f));
        bool enPuntaBorde = EnTriangulo(x + 0.5f, y + 0.5f, new Vector2(19.5f, 13.5f), new Vector2(31.5f, 18f), new Vector2(22.5f, 27.5f));
        if (enPunta) return C("#f9dc74");
        if (arriba && Mathf.Abs(dist - 9f) <= 1.5f) return C("#f9dc74");
        if (enPuntaBorde || (arriba && Mathf.Abs(dist - 9f) <= 3f)) return C("#1a0123");
        return VACIO;
    }
    static bool EnTriangulo(float px, float py, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (px - b.x) * (a.y - b.y) - (a.x - b.x) * (py - b.y);
        float d2 = (px - c.x) * (b.y - c.y) - (b.x - c.x) * (py - c.y);
        float d3 = (px - a.x) * (c.y - a.y) - (c.x - a.x) * (py - a.y);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    // Haz de luz: angosto y fuerte en x = 0, ancho y transparente en x = 63
    static Color32 PintarHaz(int x, int y)
    {
        float t = x / 63f;
        float medioAlto = Mathf.Lerp(0.12f, 0.5f, t);
        float v = Mathf.Abs((y + 0.5f) / 16f - 0.5f);
        if (v > medioAlto) return VACIO;
        byte a = (byte)(255 * (1f - t) * (1f - v / medioAlto) * 0.55f);
        return new Color32(249, 220, 116, a);
    }
}

// Botones en el Inspector de Faro_Nivel3
[CustomEditor(typeof(FaroFez))]
public class FaroFezEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Para editar el nivel: mové, estirá o duplicá las repisas y puertas adentro de Pivote (sin rotarlas).\n" +
            "Las flechas de esquina se calculan solas al dar Play.", MessageType.Info);
        if (Application.isPlaying) return;
        GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
        if (GUILayout.Button("Reconstruir faro de ejemplo (borra tus cambios)", GUILayout.Height(26)))
        {
            if (EditorUtility.DisplayDialog("Faro", "Se borra el Pivote con todas las repisas y puertas, y se arma el nivel de ejemplo de nuevo. ¿Seguro?", "Reconstruir", "Cancelar"))
                ConstructorFaro.Construir((FaroFez)target);
        }
        GUI.backgroundColor = UnityEngine.Color.white;
    }
}
