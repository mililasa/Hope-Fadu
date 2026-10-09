using UnityEngine;
using System.Collections.Generic;

// En Oscuridad (Hope y faroles). Publica el agujero de opacidad para que los
// halos se junten en vez de taparse con el negro del otro sprite.
public class HaloLuz : MonoBehaviour
{
    [Tooltip("Parte del sprite de Oscuridad que es luz. En Oscuridad_Linterna el halo dibujado termina en ~0.22.")]
    [Range(0.05f, 1.2f)]
    public float radioDelAgujero = 0.22f;
    [Tooltip("Faroles: su oscuridad solo cubre alrededor del halo, para no tapar la de Hope.")]
    public bool limitarOscuridadAlHalo;

    static readonly List<HaloLuz> activos = new List<HaloLuz>();
    static readonly Vector4[] buffer = new Vector4[8];
    static readonly int IdHalos = Shader.PropertyToID("_HopeHalos");
    static readonly int IdCount = Shader.PropertyToID("_HopeHaloCount");
    static Material materialOscuridad;

    SpriteRenderer sr;
    LanternGlow glow;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        AplicarMaterial(sr);
    }

    void OnEnable()
    {
        if (!activos.Contains(this))
            activos.Add(this);
        AsegurarHope();
        Publicar();
    }

    void OnDisable()
    {
        activos.Remove(this);
        Publicar();
    }

    void LateUpdate()
    {
        if (limitarOscuridadAlHalo && sr != null)
        {
            if (glow == null) glow = GetComponent<LanternGlow>();
            float a = glow != null ? glow.opacidadOscuridad : 0f;
            sr.color = new Color(1f, 1f, 1f, a);
        }
        Publicar();
    }

    public float Radio
    {
        get
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr == null) return 1.2f;
            if (glow == null) glow = GetComponent<LanternGlow>();
            float parte = glow != null ? glow.radioDelHalo : radioDelAgujero;
            float lado = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y);
            return Mathf.Max(0.2f, lado * parte);
        }
    }

    float AlcanceOscuridad
    {
        get
        {
            if (glow == null) glow = GetComponent<LanternGlow>();
            return glow != null ? glow.alcanceOscuridad : 1.8f;
        }
    }

    static Material MaterialOscuridad()
    {
        if (materialOscuridad == null)
        {
            Shader s = Shader.Find("Hope/OscuridadMultiHalo");
            if (s != null)
                materialOscuridad = new Material(s);
        }
        return materialOscuridad;
    }

    public static void AplicarMaterial(SpriteRenderer sr)
    {
        if (sr == null) return;
        Material mat = MaterialOscuridad();
        if (mat != null)
            sr.sharedMaterial = mat;
        sr.color = Color.white;
        sr.sortingLayerName = "Oscuridad";
        if (sr.sortingOrder < 2)
            sr.sortingOrder = 2;
    }

    static void Publicar()
    {
        int n = 0;
        for (int i = 0; i < activos.Count && n < 8; i++)
        {
            HaloLuz h = activos[i];
            if (h == null) continue;
            Vector3 p = h.transform.position;
            // w = 1: oscuridad normal. w > 1: la oscuridad propia se apaga a (w - 1) radios.
            buffer[n] = new Vector4(p.x, p.y, h.Radio, h.limitarOscuridadAlHalo ? 1f + h.AlcanceOscuridad : 1f);
            n++;
        }
        for (int i = n; i < 8; i++)
            buffer[i] = Vector4.zero;
        Shader.SetGlobalVectorArray(IdHalos, buffer);
        Shader.SetGlobalFloat(IdCount, n);
    }

    public static void AsegurarHope()
    {
        GameObject pp = GameObject.Find("Pp");
        if (pp == null) return;
        Transform t = pp.transform.Find("Oscuridad");
        if (t == null) return;
        if (t.GetComponent<HaloLuz>() == null)
            t.gameObject.AddComponent<HaloLuz>();
        AplicarMaterial(t.GetComponent<SpriteRenderer>());
    }
}
