using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ============================================================================
// HERRAMIENTAS PARA DIBUJAR LOS PISOS CON EDGE COLLIDER 2D
// (los botones están en el Inspector de Faro_Nivel3)
//
//   Crear pisos con Edge Collider   arma "Pisos (Edge Collider)" con un grupo por
//                                   cara (afuera y adentro) y le pone un Edge
//                                   Collider por cada repisa que había detectado,
//                                   más el suelo. Es un punto de partida: después
//                                   los movés, borrás o agregás vos.
//   Ver cara ...                    gira el faro a esa cara, muestra solo su dibujo
//                                   y su grupo de pisos, y lo selecciona.
//   Ver todo                        vuelve el faro a como estaba.
// ============================================================================
public static class PisosFaroEditor
{
    public const string RAIZ = "Pisos (Edge Collider)";
    public const string UN_SENTIDO = "Se atraviesan desde abajo";
    public const string SOLIDOS = "Sólidos";
    static readonly string[] NOMBRE = { "Frente", "Derecha", "Atrás", "Izquierda" };

    public static void Crear(FaroFez f)
    {
        if (f == null || f.pivote == null) { Debug.LogWarning("Faro: primero armá el faro con el botón violeta."); return; }
        Undo.RecordObject(f, "Crear pisos");
        Transform vieja = f.transform.Find(RAIZ);
        if (vieja != null) Undo.DestroyObjectImmediate(vieja.gameObject);

        GameObject raiz = new GameObject(RAIZ);
        Undo.RegisterCreatedObjectUndo(raiz, "Crear pisos");
        raiz.transform.SetParent(f.transform, false);
        int capa = LayerMask.NameToLayer("Piso");
        if (capa < 0) capa = 0;
        var grupos = new Dictionary<string, (Transform unSentido, Transform solidos)>();
        foreach (bool adentro in new[] { false, true })
            for (int i = 0; i < 4; i++)
            {
                var caraEnum = (PisoCara.CaraDibujo)i;
                GameObject g = new GameObject(NOMBRE[i] + (adentro ? " adentro" : " afuera"));
                g.transform.SetParent(raiz.transform, false);
                g.transform.position = new Vector3(f.pivote.position.x, f.pivote.position.y, 0f);
                PisoCara pc = g.AddComponent<PisoCara>();
                pc.cara = caraEnum;
                pc.lado = adentro ? PiezaFaro.Lado.Adentro : PiezaFaro.Lado.Afuera;
                GameObject u = new GameObject(UN_SENTIDO); u.transform.SetParent(g.transform, false); u.layer = capa;
                PlatformEffector2D ef = u.AddComponent<PlatformEffector2D>();
                ef.useOneWay = true; ef.useOneWayGrouping = true; ef.surfaceArc = 170f; ef.useSideFriction = false; ef.useSideBounce = false;
                GameObject s = new GameObject(SOLIDOS); s.transform.SetParent(g.transform, false); s.layer = capa;
                grupos[Clave(adentro, pc.ParedIndice)] = (u.transform, s.transform);
            }

        int cuantas = 0;
        // Las repisas que había detectado (cada una en la cara de su dibujo)
        foreach (PiezaFaro p in f.pivote.GetComponentsInChildren<PiezaFaro>(true))
        {
            if (p.tipo != PiezaFaro.Tipo.Repisa || p.ParedIndice < 0 || p.lado == PiezaFaro.Lado.Ambos) continue;
            int cara = p.EsAdentro ? FaroFez.ParedAdentroDeCara(p.ParedIndice) : p.ParedIndice;   // adentro: la cara del dibujo
            if (!grupos.TryGetValue(Clave(p.EsAdentro, cara), out var gr)) continue;
            Linea(gr.unSentido, Mathf.Min(p.desde, p.hasta), Mathf.Max(p.desde, p.hasta), p.altura, true);
            cuantas++;
        }
        // El suelo de cada cara (sólido) y arriba: el piso de la linterna adentro, el techo afuera
        foreach (var par in grupos)
        {
            bool adentro = par.Key.StartsWith("a");
            float B = f.Borde(adentro, 0f);
            Linea(par.Value.solidos, -B - 1f, B + 1f, 0f, false);
            float H = f.alturaLinterna;
            if (adentro) Linea(par.Value.unSentido, -f.Borde(true, H), f.Borde(true, H), H, true);
            else if (f.pisarTechoAfuera)
            {
                float m = f.MedioAnchoEn(H) + f.grosorMuro * 0.5f + f.salidaTecho;
                Linea(par.Value.unSentido, -m, m, H, true);
            }
        }
        f.pisosConEdge = true;
        EditorUtility.SetDirty(f);
        EditorSceneManager.MarkSceneDirty(f.gameObject.scene);
        Debug.Log("Faro: listos los pisos con Edge Collider (" + cuantas + " plataformas + el suelo de cada cara). " +
                  "Usá los botones 'Ver cara' para ver cada dibujo y acomodar los Edge Collider 2D con 'Edit Collider'.");
        VerCara(f, false, PisoCara.CaraDibujo.Frente);
    }

    static string Clave(bool adentro, int pared) { return (adentro ? "a" : "f") + pared; }

    static void Linea(Transform padre, float desde, float hasta, float altura, bool unSentido)
    {
        EdgeCollider2D e = padre.gameObject.AddComponent<EdgeCollider2D>();
        e.points = new[] { new Vector2(desde, altura), new Vector2(hasta, altura) };
        e.usedByEffector = unSentido;
    }

    // Gira el faro a esa cara y deja ver solo su dibujo y sus pisos
    public static void VerCara(FaroFez f, bool adentro, PisoCara.CaraDibujo caraDibujo)
    {
        if (f == null || f.pivote == null) return;
        int pared = PisoCaraPared(caraDibujo);
        Undo.RecordObject(f.pivote, "Ver cara");
        f.pivote.localRotation = Quaternion.Euler(0f, 90f * FaroFez.VistaDeCara(adentro, pared), 0f);
        foreach (LaminaFaro l in f.laminasDibujo)
        {
            if (l == null || l.render == null) continue;
            bool ver;
            if (l.capa == LaminaFaro.Capa.Fondo) ver = (!adentro || f.cieloAdentro) && (!l.agujero || l.ParedIndice == pared);
            else ver = l.EsAdentro == adentro && l.ParedIndice == pared;
            Undo.RecordObject(l.render, "Ver cara");
            l.render.enabled = ver;
        }
        PisoCara elegido = null;
        foreach (PisoCara g in f.GetComponentsInChildren<PisoCara>(true))
        {
            bool este = g.EsAdentro == adentro && g.cara == caraDibujo;
            Undo.RecordObject(g.gameObject, "Ver cara");
            g.gameObject.SetActive(este);
            if (este) elegido = g;
        }
        if (elegido != null)
        {
            Transform u = elegido.transform.Find(UN_SENTIDO);
            Selection.activeGameObject = u != null ? u.gameObject : elegido.gameObject;
        }
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.in2DMode = true;
            float B = f.Borde(false, 0f) + 1f, H = f.alturaLinterna + 4f;
            sv.Frame(new Bounds(f.pivote.position + Vector3.up * (H * 0.5f - 1f), new Vector3(B * 2f, H, 1f)), false);
        }
    }

    public static void VerTodo(FaroFez f)
    {
        if (f == null || f.pivote == null) return;
        Undo.RecordObject(f.pivote, "Ver todo");
        f.pivote.localRotation = Quaternion.identity;
        foreach (LaminaFaro l in f.laminasDibujo)
        {
            if (l == null || l.render == null) continue;
            Undo.RecordObject(l.render, "Ver todo");
            l.render.enabled = true;
        }
        foreach (PisoCara g in f.GetComponentsInChildren<PisoCara>(true))
        {
            Undo.RecordObject(g.gameObject, "Ver todo");
            g.gameObject.SetActive(true);
        }
    }

    static int PisoCaraPared(PisoCara.CaraDibujo c)
    {
        switch (c)
        {
            case PisoCara.CaraDibujo.Frente: return 2;
            case PisoCara.CaraDibujo.Derecha: return 3;
            case PisoCara.CaraDibujo.Atras: return 0;
            default: return 1;
        }
    }

    // La sección del Inspector de Faro_Nivel3
    public static void DibujarSeccion(FaroFez f)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pisos dibujados con Edge Collider 2D", EditorStyles.boldLabel);
        bool hay = f.transform.Find(RAIZ) != null;
        EditorGUILayout.HelpBox(hay
            ? "Elegí una cara: el faro gira a ese dibujo y queda seleccionado su grupo. En el Edge Collider 2D tocá 'Edit Collider' y arrastrá los puntos encima del dibujo " +
              "(celeste = se atraviesa desde abajo, naranja = sólido). Para más plataformas: Add Component > Edge Collider 2D en el mismo objeto."
            : "Crea un grupo de Edge Collider 2D por cara (afuera y adentro), empezando con las repisas que detecté. Después los acomodás vos encima del dibujo.",
            MessageType.None);
        GUI.backgroundColor = new Color(0.7f, 0.95f, 1f);
        if (GUILayout.Button(hay ? "Volver a crear los pisos desde lo detectado (borra los tuyos)" : "Crear pisos con Edge Collider (desde lo detectado)", GUILayout.Height(24)))
        {
            if (!hay || EditorUtility.DisplayDialog("Faro", "Se borran tus Edge Collider de 'Pisos (Edge Collider)' y se crean de nuevo desde lo detectado. ¿Seguro?", "Crear", "Cancelar"))
                Crear(f);
        }
        GUI.backgroundColor = Color.white;
        if (!hay) return;
        foreach (bool adentro in new[] { false, true })
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(adentro ? "Adentro" : "Afuera", GUILayout.Width(60));
            for (int i = 0; i < 4; i++)
                if (GUILayout.Button(NOMBRE[i])) VerCara(f, adentro, (PisoCara.CaraDibujo)i);
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Ver todo (salir de editar pisos)")) VerTodo(f);
    }
}
