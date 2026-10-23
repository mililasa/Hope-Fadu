using UnityEngine;

// ============================================================================
// PUERTA O VENTANA DEL FARO
// Va adentro de "Pivote". Tiene dos dibujos hijos: la cara de adentro y la de
// afuera. Si Hope está parada al lado y aprieta ↑, pasa al otro lado de la
// pared (la cámara da media vuelta).
//
// Su lugar se guarda con números, igual que las repisas:
//   Pared     en qué pared está
//   Posicion  a lo largo de la pared, medida como se ve DESDE ADENTRO (-4 a 4)
//   Altura    donde apoya los pies Hope (tiene que haber piso o repisa a esa
//             altura de los dos lados de la pared)
// Podés cambiar los números o arrastrarla en la escena.
// ============================================================================
[ExecuteAlways]
public class PuertaFaro : MonoBehaviour
{
    public enum Tipo { Puerta, Ventana }

    public Tipo tipo = Tipo.Puerta;
    public PiezaFaro.Cara pared = PiezaFaro.Cara.Sur;
    public float posicion = 0f;
    public float altura = 0f;
    public Renderer caraAdentro;
    public Renderer caraAfuera;

    public int ParedIndice { get { return pared == PiezaFaro.Cara.Ninguna ? 2 : (int)pared; } }
    static readonly Vector3[] NORMAL = { Vector3.forward, Vector3.left, Vector3.back, Vector3.right };

    public void Mostrar(bool adentro, bool afuera)
    {
        if (caraAdentro != null) caraAdentro.enabled = adentro;
        if (caraAfuera != null) caraAfuera.enabled = afuera;
    }

    public void Ubicar(FaroFez f)
    {
        if (f == null || f.pivote == null) return;
        int w = ParedIndice;
        Vector3 local = FaroFez.PuntoDePanel(true, w, posicion, altura, -f.grosorMuro * 0.5f, f.medioAncho, f.grosorMuro);   // centro del muro
        transform.position = f.pivote.TransformPoint(local);
        transform.rotation = f.pivote.rotation;
        Vector3 n = NORMAL[w];
        if (caraAdentro != null)
        {
            caraAdentro.transform.localPosition = -n * (f.grosorMuro * 0.5f + 0.02f) + Vector3.up * 0.975f;
            caraAdentro.transform.localRotation = Quaternion.LookRotation(n);
        }
        if (caraAfuera != null)
        {
            caraAfuera.transform.localPosition = n * (f.grosorMuro * 0.5f + 0.02f) + Vector3.up * 0.975f;
            caraAfuera.transform.localRotation = Quaternion.LookRotation(-n);
        }
        transform.hasChanged = false;
    }

    public void LeerPosicion(FaroFez f)
    {
        if (f == null || f.pivote == null) return;
        Vector3 enPanel = Quaternion.Euler(0f, 90f * ParedIndice, 0f) * f.pivote.InverseTransformPoint(transform.position);
        posicion = Mathf.Round(enPanel.x * 100f) / 100f;
        altura = Mathf.Round(enPanel.y * 100f) / 100f;
    }

#if UNITY_EDITOR
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
        if (Application.isPlaying || !transform.hasChanged) return;
        FaroFez f = GetComponentInParent<FaroFez>();
        if (f == null) { transform.hasChanged = false; return; }
        UnityEditor.Undo.RecordObject(this, "Mover puerta");
        LeerPosicion(f);
        Ubicar(f);
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.98f, 0.86f, 0.45f, 0.9f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.975f, new Vector3(1.3f, 1.95f, 1.3f));
    }
}
