using UnityEngine;

// The coordinates come from the marked copy, in source-image pixels, top to bottom.
[DisallowMultipleComponent]
public sealed class HopeWoodFootsteps : MonoBehaviour
{
    public HopeWoodFootstepSettings ajustes;
    SpriteRenderer frente;
    Collider2D cuerpo;
    AudioSource fuente;
    int ultimo = -1;

    public static bool TryPlay(PlayerController2D jugador)
    {
        if (jugador == null || jugador.gameObject.scene.name != "SampleScene") return false;
        var madera = jugador.GetComponent<HopeWoodFootsteps>();
        if (madera == null) madera = jugador.gameObject.AddComponent<HopeWoodFootsteps>();
        return madera.Paso();
    }

    void Awake()
    {
        ajustes = Resources.Load<HopeWoodFootstepSettings>("HopeWoodFootstepSettings");
        cuerpo = GetComponent<Collider2D>();
        if (ajustes == null) return;
        foreach (var r in FindObjectsOfType<SpriteRenderer>())
            if (r.gameObject.scene == gameObject.scene && r.sprite == ajustes.frente)
            { frente = r; break; }
    }

    bool Paso()
    {
        if (ajustes == null || frente == null || cuerpo == null || ajustes.pasos.Length == 0) return false;
        Vector3 pie = new Vector3(cuerpo.bounds.center.x, cuerpo.bounds.min.y, transform.position.z);
        Vector3 local = frente.transform.InverseTransformPoint(pie);
        if (frente.flipX) local.x = -local.x;
        if (frente.flipY) local.y = -local.y;
        var s = frente.sprite;
        float x = local.x * s.pixelsPerUnit + s.pivot.x;
        float y = s.rect.height - (local.y * s.pixelsPerUnit + s.pivot.y);
        bool puente = false;
        foreach (Vector4 z in ajustes.zonas)
            if (x >= z.x && x <= z.y && y >= z.z - ajustes.toleranciaVerticalPixeles && y <= z.w + ajustes.toleranciaVerticalPixeles)
            { puente = true; break; }
        if (!puente) return false;
        if (fuente == null)
        {
            var go = new GameObject("F - Pasos puentes de madera");
            go.transform.SetParent(transform, false);
            fuente = go.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.loop = false;
            fuente.spatialBlend = 0;
            HopeAudioRouting.Asignar(fuente, HopeAudioBus.Foley);
        }
        int i = Random.Range(0, ajustes.pasos.Length);
        if (ajustes.pasos.Length > 1 && i == ultimo) i = (i + Random.Range(1, ajustes.pasos.Length)) % ajustes.pasos.Length;
        ultimo = i;
        fuente.volume = ajustes.volumenCanal;
        fuente.PlayOneShot(ajustes.pasos[i], Random.Range(ajustes.volumenFragmentoMin, ajustes.volumenFragmentoMax));
        return true;
    }
}
