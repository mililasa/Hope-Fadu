using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(ChaseZone))]
public class ChaseZoneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (targets.Length > 1)
        {
            EditorGUILayout.HelpBox("Hace click SOLO en ChaseZone (no en Farol ni HandSpawn al mismo tiempo).", MessageType.Warning);
            return;
        }

        ChaseZone zona = (ChaseZone)target;

        EditorGUILayout.HelpBox(
            "Animaciones: no las arrastres. El farol usa el controller FarolZona y la mano ManoFantasma. El boton de abajo las pone solo.",
            MessageType.Info);

        if (GUILayout.Button("1) Enganchar faroles hijos (Farol, Farol (1)...)", GUILayout.Height(32)))
            ChaseZoneMenu.EngancharFarolesHijos(zona);

        if (GUILayout.Button("2) Poner collider de esta zona como Start Trigger"))
        {
            BoxCollider2D box = zona.GetComponent<BoxCollider2D>();
            if (box == null)
                box = Undo.AddComponent<BoxCollider2D>(zona.gameObject);
            box.isTrigger = true;
            serializedObject.Update();
            serializedObject.FindProperty("startTrigger").objectReferenceValue = box;
            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("3) Crear mano fantasma (con animacion)"))
            ChaseZoneMenu.CrearMano(zona);

        if (GUILayout.Button("4) Asignar pantalla Perdiste"))
        {
            CartelZona cartel = Object.FindObjectOfType<CartelZona>();
            GameObject go = GameObject.Find("Perdiste");
            if (cartel == null && go != null)
                cartel = go.GetComponent<CartelZona>();
            if (cartel != null)
            {
                serializedObject.Update();
                serializedObject.FindProperty("pantallaPerdiste").objectReferenceValue = cartel;
                serializedObject.ApplyModifiedProperties();
            }
        }

        EditorGUILayout.Space();
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Crear un farol extra (ya animado)"))
            ChaseZoneMenu.CrearFarol(zona, false);
        if (GUILayout.Button("Crear farol FINAL extra"))
            ChaseZoneMenu.CrearFarol(zona, true);
    }
}

public static class ChaseZoneMenu
{
    const string GuidFarolController = "9c4e8a1b2d3f4a5e8c7b6d0e1f2a3b4c";
    const string GuidManoController = "1a2b3c4d5e6f708192a3b4c5d6e7f809";
    const string GuidChispaController = "ca744460480eb0e4fa3ae4326cefdc23";
    const string PathFarolSheet = "Assets/farol/Farol-nuevoapagado-Sheet.png";
    const string PathManoSheet = "Assets/mano/manoPersigue-Sheet.png";
    const string PathChispaSheet = "Assets/farol/chispita-Sheet.png";

    [MenuItem("Tools/Chase Zones/Crear zona vacia")]
    public static void CrearZona()
    {
        GameObject go = new GameObject("ChaseZone");
        Undo.RegisterCreatedObjectUndo(go, "Crear ChaseZone");
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4f, 3f);
        ChaseZone zona = go.AddComponent<ChaseZone>();
        SerializedObject so = new SerializedObject(zona);
        so.FindProperty("startTrigger").objectReferenceValue = box;
        so.FindProperty("zoneName").stringValue = "Zona 1";
        so.ApplyModifiedProperties();
        Selection.activeGameObject = go;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
    }

    public static void EngancharFarolesHijos(ChaseZone zona)
    {
        if (zona == null) return;

        List<GameObject> encontrados = new List<GameObject>();
        for (int i = 0; i < zona.transform.childCount; i++)
        {
            Transform t = zona.transform.GetChild(i);
            if (t.name.IndexOf("Farol", System.StringComparison.OrdinalIgnoreCase) >= 0)
                encontrados.Add(t.gameObject);
        }

        if (encontrados.Count == 0)
        {
            EditorUtility.DisplayDialog("Chase Zone",
                "No hay hijos que se llamen Farol.\nDeja Farol, Farol (1), Farol (2) como hijos de ChaseZone y volve a apretar.",
                "Ok");
            return;
        }

        RuntimeAnimatorController ctrlFarol = CargarController(GuidFarolController);
        RuntimeAnimatorController ctrlChispa = CargarController(GuidChispaController);

        for (int i = 0; i < encontrados.Count; i++)
            PrepararObjetoFarol(encontrados[i], i == encontrados.Count - 1, ctrlFarol, ctrlChispa);

        SerializedObject so = new SerializedObject(zona);
        SerializedProperty lista = so.FindProperty("faroles");
        lista.arraySize = encontrados.Count;
        for (int i = 0; i < encontrados.Count; i++)
            lista.GetArrayElementAtIndex(i).objectReferenceValue = encontrados[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(zona);
        Debug.Log("ChaseZone: enganche " + encontrados.Count + " faroles. El ultimo quedo como FINAL.");
    }

    static void PrepararObjetoFarol(GameObject farol, bool esFinal, RuntimeAnimatorController ctrlFarol, RuntimeAnimatorController ctrlChispa)
    {
        SpriteRenderer sr = farol.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = Undo.AddComponent<SpriteRenderer>(farol);
        if (sr.sprite == null)
            sr.sprite = PrimerSprite(PathFarolSheet);

        Animator anim = farol.GetComponent<Animator>();
        if (anim == null)
            anim = Undo.AddComponent<Animator>(farol);
        if (anim.runtimeAnimatorController == null)
            anim.runtimeAnimatorController = ctrlFarol;

        BoxCollider2D col = farol.GetComponent<BoxCollider2D>();
        if (col == null)
            col = Undo.AddComponent<BoxCollider2D>(farol);
        col.isTrigger = true;

        Lantern lantern = farol.GetComponent<Lantern>();
        if (lantern == null)
            lantern = Undo.AddComponent<Lantern>(farol);
        lantern.isFinal = esFinal;

        Transform chispaT = farol.transform.Find("Chispita");
        GameObject chispa = chispaT != null ? chispaT.gameObject : null;
        if (chispa == null)
        {
            chispa = new GameObject("Chispita");
            Undo.RegisterCreatedObjectUndo(chispa, "Chispita");
            chispa.transform.SetParent(farol.transform, false);
            chispa.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        }
        SpriteRenderer srChispa = chispa.GetComponent<SpriteRenderer>();
        if (srChispa == null)
            srChispa = Undo.AddComponent<SpriteRenderer>(chispa);
        if (srChispa.sprite == null)
            srChispa.sprite = PrimerSprite(PathChispaSheet);
        Animator animChispa = chispa.GetComponent<Animator>();
        if (animChispa == null)
            animChispa = Undo.AddComponent<Animator>(chispa);
        if (animChispa.runtimeAnimatorController == null)
            animChispa.runtimeAnimatorController = ctrlChispa;
        chispa.SetActive(false);

        Transform luzT = farol.transform.Find("LuzFarol");
        GameObject luz = luzT != null ? luzT.gameObject : null;
        if (luz == null)
        {
            luz = new GameObject("LuzFarol");
            Undo.RegisterCreatedObjectUndo(luz, "LuzFarol");
            luz.transform.SetParent(farol.transform, false);
            luz.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            luz.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
        SpriteRenderer srLuz = luz.GetComponent<SpriteRenderer>();
        if (srLuz == null)
            srLuz = Undo.AddComponent<SpriteRenderer>(luz);
        Sprite osc = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Oscuridad_Linterna.png");
        if (osc != null) srLuz.sprite = osc;
        srLuz.color = Color.white;
        srLuz.sortingLayerName = "Oscuridad";
        srLuz.sortingOrder = 2;
        Lantern.AplicarOscuridadDeHope(luz);
        if (luz.GetComponent<HaloLuz>() == null)
            Undo.AddComponent<HaloLuz>(luz);
        LanternGlow glow = luz.GetComponent<LanternGlow>();
        if (glow == null)
            glow = Undo.AddComponent<LanternGlow>(luz);
        glow.escalaBase = 0.5f;
        glow.encendida = true;
        luz.SetActive(false);
    }

    public static void CrearFarol(ChaseZone zona, bool esFinal)
    {
        if (zona == null) return;

        GameObject farol = new GameObject(esFinal ? "Farol_Final" : "Farol");
        Undo.RegisterCreatedObjectUndo(farol, "Crear farol");
        farol.transform.SetParent(zona.transform, false);
        farol.transform.localPosition = new Vector3(esFinal ? 6f : 2f, 0f, 0f);
        PrepararObjetoFarol(farol, esFinal, CargarController(GuidFarolController), CargarController(GuidChispaController));
        EngancharFarolesHijos(zona);
        Selection.activeGameObject = farol;
    }

    public static void CrearMano(ChaseZone zona)
    {
        if (zona == null) return;

        GameObject mano = GameObject.Find("ManoFantasma");
        if (mano == null)
        {
            mano = new GameObject("ManoFantasma");
            Undo.RegisterCreatedObjectUndo(mano, "Crear mano fantasma");
        }

        mano.transform.position = zona.transform.position + Vector3.up * 2f;

        SpriteRenderer sr = mano.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = Undo.AddComponent<SpriteRenderer>(mano);
        if (sr.sprite == null)
            sr.sprite = PrimerSprite(PathManoSheet);
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 5;
        Color c = sr.color;
        c.a = 0.85f;
        sr.color = c;

        Animator anim = mano.GetComponent<Animator>();
        if (anim == null)
            anim = Undo.AddComponent<Animator>(mano);
        anim.runtimeAnimatorController = CargarController(GuidManoController);

        HandChaser chaser = mano.GetComponent<HandChaser>();
        if (chaser == null)
            chaser = Undo.AddComponent<HandChaser>(mano);

        Transform spawnT = zona.transform.Find("HandSpawn");
        GameObject spawn = spawnT != null ? spawnT.gameObject : new GameObject("HandSpawn");
        if (spawnT == null)
        {
            Undo.RegisterCreatedObjectUndo(spawn, "Crear HandSpawn");
            spawn.transform.SetParent(zona.transform, false);
            spawn.transform.localPosition = new Vector3(-2f, 2f, 0f);
        }

        SerializedObject so = new SerializedObject(zona);
        so.FindProperty("hand").objectReferenceValue = chaser;
        so.FindProperty("handSpawnPoint").objectReferenceValue = spawn.transform;
        so.ApplyModifiedProperties();

        mano.SetActive(false);
        Selection.activeGameObject = zona.gameObject;
    }

    static RuntimeAnimatorController CargarController(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
    }

    static Sprite PrimerSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets == null) return null;
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite s = assets[i] as Sprite;
            if (s != null) return s;
        }
        return null;
    }
}
