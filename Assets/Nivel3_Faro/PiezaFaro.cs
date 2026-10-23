using UnityEngine;

// ============================================================================
// PIEZA DEL FARO
// Va en cada objeto del faro que está adentro de "Pivote": repisas, paredes,
// pisos y decorados. Le dice a FaroFez qué es y de qué lado está.
//
// REPISAS: su lugar se guarda con números (en qué pared, de qué lado, desde
// dónde hasta dónde y a qué altura). El cubo se acomoda solo a esos números.
//   - Cambiá los números en el Inspector y la repisa se mueve.
//   - O arrastrala en la ventana Scene (a lo largo de la pared o para arriba y
//     abajo, sin rotarla): los números se actualizan solos.
//   - Para pasarla a otra pared, cambiá "Pared" en el Inspector.
// "Desde" y "Hasta" se miden de izquierda a derecha como se ve esa pared de
// frente (de -4 a 4 adentro, de -5 a 5 afuera).
// ============================================================================
[ExecuteAlways]
public class PiezaFaro : MonoBehaviour
{
    public enum Tipo
    {
        Repisa,      // se pisa. Solo se ve cuando su pared está de frente.
        Pared,       // el muro (con sus dos caras). Adentro se esconde el de adelante.
        Piso,        // pisos, suelo, mar, balcón, techo: se ven según el Lado
        Decoracion   // solo se ve, igual que una repisa pero sin pisarse
    }
    public enum Lado { Adentro, Afuera, Ambos }
    public enum Cara { Norte, Oeste, Sur, Este, Ninguna }

    public Tipo tipo = Tipo.Repisa;
    public Lado lado = Lado.Adentro;
    public Cara pared = Cara.Norte;
    [Tooltip("Repisa rota: se pisa, pero nunca lleva flecha.")]
    public bool rota;

    [Header("Lugar de la repisa en su pared")]
    public float desde = -1f;
    public float hasta = 1f;
    [Tooltip("Altura de la parte de arriba de la repisa (donde apoya los pies Hope).")]
    public float altura = 3f;
    public float grosor = 0.35f;

    public bool EsAdentro { get { return lado == Lado.Adentro; } }
    public int ParedIndice { get { return pared == Cara.Ninguna ? -1 : (int)pared; } }   // 0 N, 1 O, 2 S, 3 E

    Renderer[] renders;

    public void Mostrar(bool visible)
    {
        if (renders == null) renders = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renders.Length; i++)
            if (renders[i] != null && renders[i].enabled != visible) renders[i].enabled = visible;
    }

    // Pone el cubo donde dicen los números
    public void Ubicar(FaroFez f)
    {
        if (tipo != Tipo.Repisa && tipo != Tipo.Decoracion) return;
        if (f == null || f.pivote == null || ParedIndice < 0 || lado == Lado.Ambos) return;
        int k = FaroFez.VistaDePared(EsAdentro, ParedIndice);
        float largo = Mathf.Max(0.2f, hasta - desde);
        grosor = Mathf.Max(0.05f, grosor);
        Vector3 local = FaroFez.PuntoDePanel(EsAdentro, k, (desde + hasta) * 0.5f, altura - grosor * 0.5f, f.salidaRepisa * 0.5f, f.medioAncho, f.grosorMuro);
        transform.position = f.pivote.TransformPoint(local);
        transform.rotation = f.pivote.rotation;
        transform.localScale = (k % 2 == 0) ? new Vector3(largo, grosor, f.salidaRepisa) : new Vector3(f.salidaRepisa, grosor, largo);
        transform.hasChanged = false;
    }

    // Lee los números a partir de dónde quedó el cubo (cuando lo arrastrás en la escena)
    public void LeerPosicion(FaroFez f)
    {
        if (f == null || f.pivote == null || ParedIndice < 0 || lado == Lado.Ambos) return;
        int k = FaroFez.VistaDePared(EsAdentro, ParedIndice);
        Vector3 enPanel = Quaternion.Euler(0f, 90f * k, 0f) * f.pivote.InverseTransformPoint(transform.position);
        float largo = Mathf.Abs(k % 2 == 0 ? transform.localScale.x : transform.localScale.z);
        grosor = Mathf.Abs(transform.localScale.y);
        desde = Redondear(enPanel.x - largo * 0.5f);
        hasta = Redondear(enPanel.x + largo * 0.5f);
        altura = Redondear(enPanel.y + grosor * 0.5f);
    }
    static float Redondear(float v) { return Mathf.Round(v * 100f) / 100f; }

#if UNITY_EDITOR
    // En el editor: si cambiás un número, el cubo se mueve; si movés el cubo, cambian los números.
    void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            Ubicar(GetComponentInParent<FaroFez>());
        };
    }

    void Update()
    {
        if (Application.isPlaying || (tipo != Tipo.Repisa && tipo != Tipo.Decoracion) || !transform.hasChanged) return;
        FaroFez f = GetComponentInParent<FaroFez>();
        if (f == null) { transform.hasChanged = false; return; }
        UnityEditor.Undo.RecordObject(this, "Mover repisa");
        LeerPosicion(f);
        Ubicar(f);   // la vuelve a pegar a su pared
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
