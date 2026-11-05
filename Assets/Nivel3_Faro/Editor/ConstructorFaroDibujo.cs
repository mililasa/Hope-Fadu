using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ============================================================================
// FARO CON TUS DIBUJOS (solo en el editor)
//   Menú HOPE > Nivel 3 > Armar faro con mis dibujos
//   (o el botón violeta en el Inspector de Faro_Nivel3)
//
// Usa los PNG de Assets/Nivel3_Faro/Dibujos. Por cada cara (frente, derecha,
// atras, izquierda):
//   n3_<cara>_principal_exterior.png              la torre de afuera + lo que la rodea (suelo, avión)
//   n3_<cara>_principal_exterior_plataformas.png  lo que se pisa afuera
//   n3_<cara>_principal_interior.png              la pared de adentro
//   n3_<cara>_principal_interior_plataformas.png  lo que se pisa adentro
//   (en el PNG de la torre también cuentan las cosas apoyadas en el piso, como el
//    montón de chatarra: se vuelven escalones)
// y además:
//   n3_fondo.png                                  el cielo (queda quieto, detrás de todo)
//   n3_<cara>_principal_exterior_reja.png         (opcional) algo que va DELANTE de Hope
//   n3_exterior_luz_faro.png                      (opcional) la linterna de arriba (igual en las 4 caras)
//   n3_exterior_luz_faro_rejas.png                (opcional) la baranda de la linterna, DELANTE de Hope
// Si falta el PNG de una cara, se usa el del frente.
//
// LAS PLATAFORMAS SE LEEN SOLAS DEL DIBUJO: cada borde de arriba plano y
// horizontal de los PNG de plataformas es un lugar donde Hope puede pararse.
// Si la diseñadora mueve una plataforma, reemplazás el PNG, tocás el botón
// violeta y la repisa invisible aparece en el lugar nuevo.
// En la ventana Scene se ven como cajas de líneas (verdes adentro, celestes afuera).
// ============================================================================
public static class ConstructorFaroDibujo
{
    const string CARPETA = "Assets/Nivel3_Faro";
    const string DIBUJOS = CARPETA + "/Dibujos";

    // ------------------------------------------------------------------
    // MEDIDAS DE LOS DIBUJOS (en píxeles de los PNG de 1200 x 2520)
    //   97 px = 1 unidad: la puerta de entrada (145 px) mide 1,5 Hopes.
    //   Con "Tamaño" (en Faro_Nivel3) se divide: Tamaño 1.5 = 64,7 px por unidad (todo más grande).
    //   El faro apoya en la fila 2309, su centro está en la columna 767.5,
    //   mide 581 px de ancho abajo y 343 px arriba, y el techo está en la fila 578.
    // ------------------------------------------------------------------
    const float PPU = 97f, CENTRO_X = 767.5f, SUELO_Y = 2309f;
    static float ppu = PPU;   // píxeles por unidad de este armado: PPU / Tamaño
    const float ANCHO_ABAJO_PX = 581f, ANCHO_ARRIBA_PX = 343f, TECHO_Y = 578f;

    // Las caras: nombre en los archivos → pared del faro.
    //   frente = Sur (donde empieza Hope), derecha = Este, atras = Norte, izquierda = Oeste
    static readonly string[] CARA_ARCHIVO = { "atras", "izquierda", "frente", "derecha" };   // en el orden de las paredes: Norte, Oeste, Sur, Este
    static readonly string[] CARA_NOMBRE = { "Atras", "Izquierda", "Frente", "Derecha" };
    static readonly PiezaFaro.Cara[] CARAS = { PiezaFaro.Cara.Norte, PiezaFaro.Cara.Oeste, PiezaFaro.Cara.Sur, PiezaFaro.Cara.Este };

    // ------------------------------------------------------------------
    // PUERTAS Y VENTANAS ABIERTAS (por las que se pasa con ↑), en píxeles del PNG:
    //   columna del centro, fila de los pies ADENTRO y fila de los pies AFUERA.
    // Tienen que tener algo para pisar de los dos lados (el piso, un escalón, el alféizar).
    // Las tapiadas no van acá: son solo dibujo.
    // ------------------------------------------------------------------
    struct Abertura { public int cara; public string nombre; public PuertaFaro.Tipo tipo; public float x, yAdentro, yAfuera; }
    static Abertura A(int cara, string n, PuertaFaro.Tipo t, float x, float yAd, float yAf)
    { return new Abertura { cara = cara, nombre = n, tipo = t, x = x, yAdentro = yAd, yAfuera = yAf }; }
    const int NORTE = 0, OESTE = 1, SUR = 2, ESTE = 3;
    static readonly Abertura[] PUERTAS =
    {
        A(SUR, "Puerta", PuertaFaro.Tipo.Puerta, 768, 2309, 2309),         // la entrada
        A(SUR, "Ventana 1", PuertaFaro.Tipo.Ventana, 648, 1638, 1589),     // escalón → alféizar
        A(SUR, "Ventana 2", PuertaFaro.Tipo.Ventana, 648, 1424, 1301),     // piso → alféizar
        A(SUR, "Ventana 3", PuertaFaro.Tipo.Ventana, 648, 784, 757),       // repisa → alféizar
        A(OESTE, "Ventana 3", PuertaFaro.Tipo.Ventana, 894, 859, 757),     // piso de arriba → alféizar
    };

    struct Plat { public float x0, x1, y; public bool sinFlecha; }

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
        if (!EditorUtility.DisplayDialog("Faro", "Se borra el faro actual (repisas, puertas y láminas) y se arma con tus dibujos. ¿Seguro?", "Armar", "Cancelar"))
            return;
        Construir(faro);
    }

    public static void Construir(FaroFez faro)
    {
        // ---- Los dibujos de cada cara (si falta alguno, el del frente) ----
        var torreAf = new Texture2D[4]; var platAf = new Texture2D[4]; var torreAd = new Texture2D[4]; var platAd = new Texture2D[4]; var reja = new Texture2D[4];
        var faltan = new List<string>();
        for (int w = 0; w < 4; w++)
        {
            torreAf[w] = CargarCara(CARA_ARCHIVO[w], "exterior", faltan);
            platAf[w] = CargarCara(CARA_ARCHIVO[w], "exterior_plataformas", faltan);
            torreAd[w] = CargarCara(CARA_ARCHIVO[w], "interior", faltan);
            platAd[w] = CargarCara(CARA_ARCHIVO[w], "interior_plataformas", faltan);
            reja[w] = CargarCara(CARA_ARCHIVO[w], "exterior_reja", null);
        }
        for (int w = 0; w < 4; w++)
        {
            if (torreAf[w] == null) torreAf[w] = torreAf[SUR];
            if (torreAd[w] == null) torreAd[w] = torreAd[SUR];
            if (platAf[w] == null && torreAf[w] == torreAf[SUR]) platAf[w] = platAf[SUR];
            if (platAd[w] == null && torreAd[w] == torreAd[SUR]) platAd[w] = platAd[SUR];
        }
        Texture2D cielo = CargarDibujo("n3_fondo");
        Texture2D linterna = CargarDibujo("n3_exterior_luz_faro"), rejaLinterna = CargarDibujo("n3_exterior_luz_faro_rejas");
        if (torreAf[SUR] == null || torreAd[SUR] == null)
        {
            EditorUtility.DisplayDialog("Faro", "No encuentro los dibujos del frente en " + DIBUJOS +
                ".\nTienen que estar al menos n3_frente_principal_exterior.png y n3_frente_principal_interior.png.", "OK");
            return;
        }

        Undo.RecordObject(faro, "Faro con dibujos");
        Undo.RecordObject(faro.transform, "Faro con dibujos");
        // ---- Tamaño: si escalaste Faro_Nivel3 (o el Pivote), esa escala pasa a "Tamaño" ----
        float escala = faro.transform.localScale.x;
        Transform pivoteViejo = faro.transform.Find("Pivote");
        if (pivoteViejo != null) escala *= pivoteViejo.localScale.x;
        if (Mathf.Abs(escala - 1f) > 0.01f)
        {
            faro.tamano = Mathf.Max(0.1f, faro.tamano) * escala;
            Debug.Log("Faro: estaba escalado x" + escala.ToString("0.##") + ". Ahora la escala es 1 y 'Tamaño' = " + faro.tamano.ToString("0.##") +
                      " (así los dibujos y las repisas invisibles crecen juntos).");
        }
        faro.transform.localScale = Vector3.one;
        faro.tamano = Mathf.Max(0.1f, faro.tamano);
        ppu = PPU / faro.tamano;

        // ---- Medidas del faro, sacadas del dibujo ----
        faro.pixelesPorUnidad = ppu;
        faro.centroDibujoX = CENTRO_X;
        faro.sueloDibujoY = SUELO_Y;
        faro.medioAncho = ANCHO_ABAJO_PX * 0.5f / ppu;          // 3 (× Tamaño)
        faro.medioAnchoArriba = ANCHO_ARRIBA_PX * 0.5f / ppu;   // 1.77 (× Tamaño)
        faro.alturaLinterna = (SUELO_Y - TECHO_Y) / ppu;         // 17.8 (× Tamaño): el techo
        faro.grosorMuro = 0.02f;            // la torre de adentro mide lo mismo que la de afuera
        faro.salidaTecho = 0f;              // en el techo, solo el techo
        faro.puertasDeFrente = true;        // ventanas de adentro y de afuera en el mismo lugar
        faro.empiezaAfuera = true;
        faro.vistaInicial = 0;
        faro.profundidadDeCostado = true;      // adentro el rincón se angosta: se nota que es una esquina de adentro
        faro.profundidadAfuera = false;
        faro.profundidadSoloDeCostado = false; // el rincón se achica hacia los pies de Hope (V), sin abrirse
        faro.perspectivaRincon = 1f;
        faro.versionArmado = 4;

        // ---- Plataformas: se leen de los dibujos ----
        var plAf = new List<Plat>[4]; var plAd = new List<Plat>[4];
        for (int w = 0; w < 4; w++)
        {
            plAf[w] = LeerPlataformas(platAf[w], torreAf[w], true);
            plAd[w] = LeerPlataformas(platAd[w], torreAd[w], false);
        }
        // La linterna de arriba: su balcón se pisa de las 4 caras, adentro y afuera
        float balcon = -1f;
        if (linterna != null)
        {
            foreach (Plat p0 in Detectar(linterna, null, 0, int.MaxValue))
            {
                if (p0.y < TECHO_Y - 150 || p0.y > TECHO_Y + 20 || p0.x1 - p0.x0 < 120) continue;   // solo el piso del balcón
                Plat p = p0; p.sinFlecha = true;
                // un poco más ancho que el dibujo: así Hope llega al borde donde se dobla sin caerse antes
                p.x0 = Mathf.Min(p.x0, CENTRO_X - 300f); p.x1 = Mathf.Max(p.x1, CENTRO_X + 300f);
                balcon = Mathf.Max(balcon, Alto(p.y));
                for (int w = 0; w < 4; w++) { plAf[w].Add(p); plAd[w].Add(p); }
            }
        }
        faro.pisarTechoAfuera = linterna == null;   // con linterna, afuera se pisa su balcón (no el techo de abajo)
        // Afuera, hasta dónde se camina antes de doblar: la punta de los balcones que siguen a la vuelta
        // de la esquina (una plataforma que sale del borde derecho de una cara y otra a la misma altura
        // que sale del borde izquierdo de la cara de al lado). Si no hay ninguna, la que más sobresale.
        float sale = 0f, saleMax = 1f;
        for (int w = 0; w < 4; w++)
            foreach (Plat p in plAf[w])
            {
                float m = faro.MedioAnchoEn(Alto(p.y)) + faro.grosorMuro * 0.5f;
                float der = U(p.x1) - m, izq = -U(p.x0) - m;
                saleMax = Mathf.Max(saleMax, der, izq);
                if (p.sinFlecha || der <= 0.2f) continue;
                foreach (Plat q in plAf[(w + 1) % 4])
                {
                    float izqQ = -U(q.x0) - m;
                    if (!q.sinFlecha && Mathf.Abs(q.y - p.y) <= 8 && izqQ > 0.2f) sale = Mathf.Max(sale, der, izqQ);
                }
            }
        faro.salidaRepisa = Mathf.Ceil((sale > 0f ? sale : saleMax) * 100f) / 100f;

        // ---- Materiales ----
        ConstructorFaro.Materiales viejos = ConstructorFaro.PrepararMateriales();   // flecha y haz de luz
        Shader opaco = Shader.Find("Hope/FaroOpaco"), sprite = Shader.Find("Hope/FaroSprite"), plano = Shader.Find("Sprites/Default");
        if (opaco == null || sprite == null)
        {
            EditorUtility.DisplayDialog("Faro", "Faltan los shaders Hope/FaroOpaco y Hope/FaroSprite (carpeta Nivel3_Faro/Resources).", "OK");
            return;
        }

        // ---- Pivote limpio ----
        faro.transform.position = Vector3.zero;
        faro.laminasDibujo.Clear();
        foreach (string viejo in new[] { "Pivote", "Haz de luz", "Fondo (dibujo)", "Fondo" })
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

        // ---- Fondo: el cielo (quieto) y lo que rodea a cada cara (suelo, avión) ----
        Transform grupoFondo = Grupo(faro.transform, "Fondo");
        if (cielo != null)
        {
            LaminaFaro l = Lamina(grupoFondo, "Cielo", LaminaFaro.Capa.Fondo, PiezaFaro.Cara.Ninguna, PiezaFaro.Lado.Ambos, Mat("Dibujo cielo", plano, cielo), 0f, faro, false);
            l.agujero = false; l.Armar(faro);
        }
        for (int w = 0; w < 4; w++)
            Lamina(grupoFondo, "Alrededor " + CARA_NOMBRE[w], LaminaFaro.Capa.Fondo, CARAS[w], PiezaFaro.Lado.Afuera,
                   Mat("Dibujo alrededor " + CARA_NOMBRE[w], plano, torreAf[w]), 0f, faro);

        // ---- Láminas de las 4 caras ----
        Transform grupoLaminas = Grupo(pivote, "Laminas");
        for (int w = 0; w < 4; w++)
        {
            Transform g = Grupo(grupoLaminas, "Cara " + CARA_NOMBRE[w]);
            string n = CARA_NOMBRE[w];
            Lamina(g, "Torre afuera", LaminaFaro.Capa.Torre, CARAS[w], PiezaFaro.Lado.Afuera, Mat("Dibujo " + n + " torre afuera", opaco, torreAf[w]), 0f, faro);
            if (platAf[w] != null) Lamina(g, "Plataformas afuera", LaminaFaro.Capa.Plataformas, CARAS[w], PiezaFaro.Lado.Afuera, Mat("Dibujo " + n + " plataformas afuera", sprite, platAf[w]), 0.04f, faro);
            if (reja[w] != null)
            {
                LaminaFaro l = Lamina(g, "Reja (delante de Hope)", LaminaFaro.Capa.Plataformas, CARAS[w], PiezaFaro.Lado.Afuera, Mat("Dibujo " + n + " reja", sprite, reja[w]), 0.08f, faro, false);
                l.delante = true; l.Armar(faro);
            }
            Lamina(g, "Torre adentro", LaminaFaro.Capa.Torre, CARAS[w], PiezaFaro.Lado.Adentro, Mat("Dibujo " + n + " torre adentro", opaco, torreAd[w]), 0f, faro);
            if (platAd[w] != null) Lamina(g, "Plataformas adentro", LaminaFaro.Capa.Plataformas, CARAS[w], PiezaFaro.Lado.Adentro, Mat("Dibujo " + n + " plataformas adentro", sprite, platAd[w]), 0.04f, faro);
            // La linterna de arriba se ve igual desde adentro y desde afuera (es de vidrio)
            foreach (PiezaFaro.Lado lado in new[] { PiezaFaro.Lado.Afuera, PiezaFaro.Lado.Adentro })
            {
                string cual = lado == PiezaFaro.Lado.Afuera ? "afuera" : "adentro";
                if (linterna != null) Lamina(g, "Linterna " + cual, LaminaFaro.Capa.Plataformas, CARAS[w], lado, Mat("Dibujo linterna", sprite, linterna), 0.05f, faro);
                if (rejaLinterna != null)
                {
                    LaminaFaro l = Lamina(g, "Baranda linterna " + cual + " (delante de Hope)", LaminaFaro.Capa.Plataformas, CARAS[w], lado, Mat("Dibujo linterna baranda", sprite, rejaLinterna), 0.09f, faro, false);
                    l.delante = true; l.Armar(faro);
                }
            }
        }

        // ---- Repisas invisibles sobre las plataformas dibujadas ----
        Transform grupoAfuera = Grupo(pivote, "Repisas afuera"), grupoAdentro = Grupo(pivote, "Repisas adentro");
        var resumen = new System.Text.StringBuilder();
        for (int w = 0; w < 4; w++)
        {
            Transform ga = Grupo(grupoAfuera, "Cara " + CARA_NOMBRE[w]), gd = Grupo(grupoAdentro, "Cara " + CARA_NOMBRE[w]);
            int i = 0;
            foreach (Plat p in plAf[w]) Repisa(ga, "Plataforma " + (++i), p, CARAS[w], false, faro);
            i = 0;
            // Adentro, el interior de esta cara está pegado a la pared que queda al fondo cuando se lo ve
            foreach (Plat p in plAd[w]) Repisa(gd, "Plataforma " + (++i), p, CARAS[FaroFez.ParedAdentroDeCara(w)], true, faro);
            resumen.Append(CARA_NOMBRE[w] + ": " + plAf[w].Count + " afuera, " + plAd[w].Count + " adentro. ");
        }

        // ---- Puertas y ventanas (sin dibujo propio: ya están en los PNG) ----
        Transform grupoPuertas = Grupo(pivote, "Puertas");
        foreach (Abertura d in PUERTAS)
        {
            GameObject go = new GameObject(d.nombre + " " + CARA_NOMBRE[d.cara]);
            go.transform.SetParent(grupoPuertas, false);
            PuertaFaro pf = go.AddComponent<PuertaFaro>();
            pf.tipo = d.tipo;
            pf.pared = CARAS[d.cara];
            pf.posicion = U(d.x);
            pf.altura = Alto(d.yAdentro);
            pf.desnivelAfuera = Alto(d.yAfuera) - Alto(d.yAdentro);
            pf.Ubicar(faro);
        }

        // ---- Haz de luz (se prende al ganar) ----
        float H = faro.alturaLinterna;
        GameObject haz = new GameObject("Haz de luz");
        haz.transform.SetParent(faro.transform, false);
        haz.transform.localPosition = new Vector3(0f, balcon > 0f ? Alto(420f) : H + 2.2f, pivote.localPosition.z);   // a la altura de la lente dibujada
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

        // Hope frente a la puerta, el farol en el techo (o en el balcón de la linterna), la cámara
        ConstructorFaro.UbicarEscena(faro, balcon > 0f ? balcon - H + 1.2f : -1f, -3f * faro.tamano);
        foreach (LaminaFaro l in faro.laminasDibujo) l.Armar(faro);   // la reja necesita a Hope
        CameraFollow cf = Object.FindObjectOfType<CameraFollow>();
        if (cf != null)
        {
            float borde = faro.Borde(false, 0f) + 1f;
            cf.minX = -borde; cf.maxX = borde;
            cf.maxY = Mathf.Max(faro.alturaLinterna + 6f, linterna != null ? Alto(60f) + 1f : 0f);
            EditorUtility.SetDirty(cf);
        }
        EditorUtility.SetDirty(faro);
        EditorSceneManager.MarkSceneDirty(faro.gameObject.scene);
        Selection.activeGameObject = faro.gameObject;
        Debug.Log("Faro armado con tus dibujos (Tamaño " + faro.tamano.ToString("0.##") + ", " + ppu.ToString("0.#") + " px por unidad). Plataformas leídas: " + resumen +
                  (linterna != null ? "Linterna de arriba: sí. " : "Linterna de arriba: no está n3_exterior_luz_faro.png. ") +
                  (faltan.Count > 0 ? "\nFaltan (se usa el frente): " + string.Join(", ", faltan) : ""));
    }

    // ==================================================================
    // LEER LAS PLATAFORMAS DE UN DIBUJO
    // Busca bordes de arriba planos: píxeles opacos que no tienen nada encima y
    // que tienen al menos 5 píxeles de "cuerpo" abajo (así las sogas finas no cuentan).
    //   - De 28 px de ancho para arriba es una plataforma (o un escalón).
    //   - Las tablas inclinadas (hamacas) se toman enteras por su punto más alto.
    //   - El vidrio de las ventanas (lavanda claro) no cuenta como sólido: así el
    //     alféizar de abajo de la ventana sí es una plataforma.
    //   - Lo angosto que está adentro de algo con forma de ventana (las tablas de
    //     las ventanas tapiadas) no es una plataforma.
    // Del PNG de la torre solo se leen las plataformas dibujadas en ella (por
    // colores de plataforma), por si alguna quedó en ese PNG.
    // ==================================================================
    static readonly Color32 VIDRIO = new Color32(198, 175, 219, 255);
    static readonly Color32[] COLORES_AFUERA = { new Color32(150, 107, 184, 255), new Color32(102, 69, 128, 255) };
    static readonly Color32[] COLORES_ADENTRO = { new Color32(79, 69, 135, 255), new Color32(14, 11, 56, 255) };

    static List<Plat> LeerPlataformas(Texture2D plataformas, Texture2D torre, bool afuera)
    {
        var lista = new List<Plat>();
        if (plataformas != null) lista.AddRange(Detectar(plataformas, null, 0, int.MaxValue));
        if (torre != null)
        {
            var extra = afuera ? Detectar(torre, COLORES_AFUERA, 0, 2200) : Detectar(torre, COLORES_ADENTRO, 1, 2290);
            extra.AddRange(Montones(torre, afuera ? COLORES_AFUERA : COLORES_ADENTRO));   // chatarra apoyada en el piso → escalones
            foreach (Plat e in extra)
            {
                if (e.x1 - e.x0 < 45) continue;   // en la torre, lo angosto suele ser parte de una ventana
                bool repetida = false;
                foreach (Plat q in lista)
                    if (Mathf.Abs(e.y - q.y) <= 8 && !(e.x0 > q.x1 || e.x1 < q.x0)) { repetida = true; break; }
                if (!repetida) lista.Add(e);
            }
        }
        // De abajo hacia arriba; las que cruzan la cara entera (pisos, balcones que rodean) sin flecha
        lista.Sort((a, b) => b.y.CompareTo(a.y));
        for (int i = 0; i < lista.Count; i++)
        {
            Plat p = lista[i];
            float medio = MedioAnchoPx(p.y);
            p.sinFlecha = p.x0 <= CENTRO_X - medio + 6 && p.x1 >= CENTRO_X + medio - 6;
            lista[i] = p;
        }
        return lista;
    }

    static float MedioAnchoPx(float fila)
    {
        float t = Mathf.Clamp01((SUELO_Y - fila) / (SUELO_Y - TECHO_Y));
        return Mathf.Lerp(ANCHO_ABAJO_PX, ANCHO_ARRIBA_PX, t) * 0.5f;
    }

    static bool Parecido(Color32 a, Color32 b) { return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 12; }

    // dentroDelFaro: 0 = todo el dibujo, 1 = solo adentro de la forma del faro
    static List<Plat> Detectar(Texture2D tex, Color32[] colores, int dentroDelFaro, int filaMaxima)
    {
        // Se lee el PNG del disco (así no importa cómo esté importado)
        var leida = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        leida.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(tex)));
        int W = leida.width, H = leida.height;
        Color32[] px = leida.GetPixels32();
        Object.DestroyImmediate(leida);

        // Máscara de lo sólido, con la fila 0 ARRIBA (como en el PNG)
        var m = new bool[W * H];
        for (int y = 0; y < H && y < filaMaxima; y++)
        {
            float medio = MedioAnchoPx(y);
            for (int x = 0; x < W; x++)
            {
                Color32 c = px[(H - 1 - y) * W + x];
                if (c.a <= 128 || Parecido(c, VIDRIO)) continue;
                if (colores != null)
                {
                    bool ok = false;
                    foreach (Color32 k in colores) if (Parecido(c, k)) { ok = true; break; }
                    if (!ok) continue;
                }
                if (dentroDelFaro == 1 && Mathf.Abs(x - CENTRO_X) > medio - 3) continue;
                m[y * W + x] = true;
            }
        }

        var tramos = new List<Vector3Int>();       // (x0, x1, fila)
        var ventanas = new List<RectInt>();        // cosas con forma de ventana
        // 1) Bordes de arriba con cuerpo abajo
        for (int y = 0; y < H; y++)
        {
            int inicio = -1;
            for (int x = 0; x <= W; x++)
            {
                bool borde = false;
                if (x < W && m[y * W + x] && (y == 0 || !m[(y - 1) * W + x]))
                {
                    int cuerpo = 0;
                    for (int k = 0; k < 5 && y + k < H; k++) if (m[(y + k) * W + x]) cuerpo++;
                    borde = cuerpo >= 4;
                }
                if (borde) { if (inicio < 0) inicio = x; }
                else if (inicio >= 0) { if (x - inicio >= 28) tramos.Add(new Vector3Int(inicio, x, y)); inicio = -1; }
            }
        }
        // 2) Piezas sueltas: tablas inclinadas (bajas y anchas) y formas de ventana
        var visto = new bool[W * H];
        var cola = new Queue<int>();
        for (int i0 = 0; i0 < W * H; i0++)
        {
            if (!m[i0] || visto[i0]) continue;
            int x0 = W, x1 = 0, y0 = H, y1 = 0, n = 0;
            visto[i0] = true; cola.Enqueue(i0);
            while (cola.Count > 0)
            {
                int i = cola.Dequeue(); int x = i % W, y = i / W; n++;
                if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = x + dx, yy = y + dy;
                        if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue;
                        int j = yy * W + xx;
                        if (m[j] && !visto[j]) { visto[j] = true; cola.Enqueue(j); }
                    }
            }
            int ancho = x1 - x0 + 1, alto = y1 - y0 + 1;
            if (alto <= 32 && ancho >= 40 && n > 200) tramos.Add(new Vector3Int(x0, x1 + 1, y0));
            if (alto > 60 && ancho < 120) ventanas.Add(new RectInt(x0, y0, ancho, alto));
        }
        // 3) Juntar los tramos de la misma tabla y sacar las tablas de ventanas tapiadas
        tramos.Sort((a, b) => a.z != b.z ? a.z.CompareTo(b.z) : a.x.CompareTo(b.x));
        var juntos = new List<Vector3Int>();
        foreach (Vector3Int t in tramos)
        {
            bool unido = false;
            for (int k = 0; k < juntos.Count; k++)
            {
                Vector3Int j = juntos[k];
                if (Mathf.Abs(j.z - t.z) <= 8 && !(t.x > j.y + 8 || t.y < j.x - 8))
                {
                    juntos[k] = new Vector3Int(Mathf.Min(j.x, t.x), Mathf.Max(j.y, t.y), Mathf.Min(j.z, t.z));
                    unido = true; break;
                }
            }
            if (!unido) juntos.Add(t);
        }
        var lista = new List<Plat>();
        foreach (Vector3Int j in juntos)
        {
            if (j.y - j.x < 45)
            {
                bool deVentana = false;
                foreach (RectInt v in ventanas)
                    if (j.x >= v.xMin - 2 && j.y <= v.xMax + 2 && j.z >= v.yMin && j.z <= v.yMax) { deVentana = true; break; }
                if (deVentana) continue;
            }
            lista.Add(new Plat { x0 = j.x, x1 = j.y, y = j.z });
        }
        return lista;
    }

    // ==================================================================
    // MONTONES APOYADOS EN EL PISO (como la chatarra de la derecha, adentro)
    // No tienen bordes planos, así que se leen de otra forma: por cada columna de
    // píxeles, dónde empieza el montón (desde arriba). Donde esa altura se mantiene
    // pareja por lo menos 30 px de ancho, hay un escalón. Así el montón se sube
    // como una escalera: el baúl, las cajas, la parte más alta.
    // Solo cuenta lo que está dentro de la forma del faro, en los 420 px de abajo,
    // con los colores de plataforma y tocando (casi) el piso.
    // ==================================================================
    static List<Plat> Montones(Texture2D tex, Color32[] colores)
    {
        var lista = new List<Plat>();
        if (tex == null) return lista;
        var leida = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        leida.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(tex)));
        int W = leida.width, H = leida.height;
        Color32[] px = leida.GetPixels32();
        Object.DestroyImmediate(leida);

        int fila0 = Mathf.Max(0, (int)SUELO_Y - 420), fila1 = Mathf.Min(H, (int)SUELO_Y - 3);   // banda de abajo (fila 0 = arriba)
        int alto = fila1 - fila0;
        if (alto <= 40) return lista;
        float medio = MedioAnchoPx(SUELO_Y);
        var arriba = new int[W];
        var cuenta = new int[alto + 1];
        for (int x = 0; x < W; x++)
        {
            arriba[x] = -1;
            if (Mathf.Abs(x - CENTRO_X) > medio - 3) continue;
            // cuenta[i] = cuántos píxeles sólidos hay desde la fila fila0+i hasta abajo
            cuenta[alto] = 0;
            for (int i = alto - 1; i >= 0; i--)
            {
                Color32 c = px[(H - 1 - (fila0 + i)) * W + x];
                bool solido = false;
                if (c.a > 128) foreach (Color32 k in colores) if (Parecido(c, k)) { solido = true; break; }
                cuenta[i] = cuenta[i + 1] + (solido ? 1 : 0);
            }
            if (cuenta[alto - 40] == 0) continue;                // no toca el piso
            for (int i = 0; i < alto - 1; i++)
            {
                if (cuenta[i] == cuenta[i + 1]) continue;        // esta fila no es sólida
                // el primer píxel desde arriba que tiene montón debajo (no un palito suelto)
                if (cuenta[i] >= 0.25f * (alto - i)) { arriba[x] = fila0 + i; break; }
            }
        }
        // Tramos donde la altura es pareja (de columna a columna cambia poco)
        int a = 0;
        while (a < W)
        {
            if (arriba[a] < 0) { a++; continue; }
            int b = a, min = arriba[a], max = arriba[a];
            while (b + 1 < W && arriba[b + 1] >= 0 && Mathf.Abs(arriba[b + 1] - arriba[b]) <= 8 &&
                   Mathf.Max(max, arriba[b + 1]) - Mathf.Min(min, arriba[b + 1]) <= 24)
            {
                b++; min = Mathf.Min(min, arriba[b]); max = Mathf.Max(max, arriba[b]);
            }
            if (b - a + 1 >= 30)
            {
                var alturas = new List<int>();
                for (int x = a; x <= b; x++) alturas.Add(arriba[x]);
                alturas.Sort();
                lista.Add(new Plat { x0 = a, x1 = b + 1, y = alturas[alturas.Count / 2] });
            }
            a = b + 1;
        }
        return lista;
    }

    // ------------------------------------------------------------------
    static float U(float xPx) { return (xPx - CENTRO_X) / ppu; }      // columna del PNG → unidades desde el centro
    static float Alto(float yPx) { return (SUELO_Y - yPx) / ppu; }    // fila del PNG → altura desde el suelo
    static float Redondear(float v) { return Mathf.Round(v * 100f) / 100f; }

    static Transform Grupo(Transform padre, string nombre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        return g.transform;
    }

    static LaminaFaro Lamina(Transform padre, string nombre, LaminaFaro.Capa capa, PiezaFaro.Cara pared, PiezaFaro.Lado lado, Material m, float separacion, FaroFez faro, bool armar = true)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = m;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        // La lámina no es un script en el objeto: queda anotada en la lista de Faro_Nivel3
        var l = new LaminaFaro { nombre = (padre != null ? padre.name + " / " : "") + nombre, render = mr };
        faro.laminasDibujo.Add(l);
        l.capa = capa;
        l.pared = pared;
        l.lado = lado;
        l.separacion = separacion;
        if (armar) l.Armar(faro);
        return l;
    }

    static void Repisa(Transform padre, string nombre, Plat p, PiezaFaro.Cara pared, bool adentro, FaroFez faro)
    {
        GameObject go = new GameObject(nombre);   // invisible: el dibujo ya está en la lámina
        go.transform.SetParent(padre, false);
        PiezaFaro pz = go.AddComponent<PiezaFaro>();
        pz.tipo = PiezaFaro.Tipo.Repisa;
        pz.lado = adentro ? PiezaFaro.Lado.Adentro : PiezaFaro.Lado.Afuera;
        pz.pared = pared;
        pz.sinFlecha = p.sinFlecha;
        pz.desde = Redondear(U(p.x0)); pz.hasta = Redondear(U(p.x1)); pz.altura = Redondear(Alto(p.y));
        pz.grosor = 0.2f;
        pz.Ubicar(faro);
    }

    // Carga el PNG de una cara. Acepta "atras", "atrás" y el nombre que deja Google Drive al descargar.
    static Texture2D CargarCara(string cara, string capa, List<string> faltan)
    {
        var nombres = new List<string> { cara };
        if (cara == "atras") { nombres.Add("atrás"); nombres.Add("atr#U00e1s"); nombres.Add("atrás"); }
        foreach (string c in nombres)
        {
            Texture2D t = CargarDibujo("n3_" + c + "_principal_" + capa);
            if (t != null) return t;
        }
        if (faltan != null) faltan.Add("n3_" + cara + "_principal_" + capa + ".png");
        return null;
    }

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
                imp.maxTextureSize = 4096;                        // los PNG miden 2520 de alto
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
