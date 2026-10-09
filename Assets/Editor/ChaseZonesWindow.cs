using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ChaseZonesWindow : EditorWindow
{
    Vector2 scroll;

    [MenuItem("Tools/Chase Zones")]
    static void Abrir()
    {
        GetWindow<ChaseZonesWindow>("Chase Zones");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Refrescar"))
            Repaint();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Crear objetos (con animaciones ya puestas)", EditorStyles.boldLabel);
        if (GUILayout.Button("Crear zona vacia"))
            ChaseZoneMenu.CrearZona();
        EditorGUILayout.HelpBox("Selecciona una ChaseZone y usa el Inspector: botones para crear faroles y mano. No hace falta arrastrar animaciones.", MessageType.None);

        ChaseZone[] zonas = FindObjectsOfType<ChaseZone>();
        EditorGUILayout.LabelField("Zonas en la escena: " + zonas.Length, EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (zonas.Length == 0)
            EditorGUILayout.HelpBox("No hay ChaseZone en la escena abierta.", MessageType.Info);

        for (int i = 0; i < zonas.Length; i++)
            DibujarZona(zonas[i]);

        EditorGUILayout.EndScrollView();
    }

    void DibujarZona(ChaseZone z)
    {
        if (z == null) return;
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(string.IsNullOrEmpty(z.ZoneName) ? z.name : z.ZoneName, EditorStyles.boldLabel);

        List<Lantern> faroles = z.Lanterns;
        int cantidad = faroles != null ? faroles.Count : 0;
        int finales = 0;
        if (faroles != null)
        {
            for (int i = 0; i < faroles.Count; i++)
            {
                if (faroles[i] != null && faroles[i].isFinal)
                    finales++;
            }
        }

        EditorGUILayout.LabelField("Faroles", cantidad.ToString());
        EditorGUILayout.LabelField("playerChaseSpeed", z.PlayerChaseSpeed.ToString("0.##"));
        EditorGUILayout.LabelField("handBaseSpeed", z.HandBaseSpeed.ToString("0.##"));
        EditorGUILayout.LabelField("handAcceleration", z.HandAcceleration.ToString("0.##"));
        EditorGUILayout.LabelField("catchDistance", z.CatchDistance.ToString("0.##"));

        if (z.Hand == null)
            EditorGUILayout.HelpBox("Falta HandChaser.", MessageType.Warning);
        if (z.StartTrigger == null)
            EditorGUILayout.HelpBox("Falta startTrigger.", MessageType.Warning);
        if (cantidad == 0)
            EditorGUILayout.HelpBox("La lista de lanterns esta vacia.", MessageType.Warning);
        if (finales == 0)
            EditorGUILayout.HelpBox("Ningun farol tiene isFinal.", MessageType.Warning);
        if (finales > 1)
            EditorGUILayout.HelpBox("Hay mas de un farol final.", MessageType.Warning);

        if (GUILayout.Button("Ir"))
        {
            Selection.activeGameObject = z.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Frame(new Bounds(z.transform.position, Vector3.one * 6f), false);
        }

        EditorGUILayout.EndVertical();
    }
}
