using UnityEngine;

// Fusiona Oscuridad y Oscuridad (1) para que los halos se unan
// y ninguno tape el hueco de luz del otro.
[DefaultExecutionOrder(50)]
public class OscuridadFusion : MonoBehaviour
{
    public SpriteRenderer otroHalo;

    SpriteRenderer yo;
    SpriteRenderer otro;
    Material matYo;
    Material matOtro;
    Material matSpriteDefault;
    Shader shaderFusion;
    bool otroEraActivo = true;

    public void Configurar(SpriteRenderer haloFarol)
    {
        otroHalo = haloFarol;
        otro = haloFarol;
    }

    void Awake()
    {
        yo = GetComponent<SpriteRenderer>();
        shaderFusion = Shader.Find("Hope/OscuridadFusion");
        if (yo != null) matSpriteDefault = yo.sharedMaterial;
        BuscarOtro();
    }

    void BuscarOtro()
    {
        if (otro != null) return;
        if (otroHalo != null)
        {
            otro = otroHalo;
            return;
        }

        GameObject farol = GameObject.Find("farol");
        if (farol != null)
        {
            Transform t = farol.transform.Find("Oscuridad (1)");
            if (t == null) t = farol.transform.Find("Oscuridad(1)");
            if (t != null) otro = t.GetComponent<SpriteRenderer>();
        }
    }

    void OnDestroy()
    {
        if (matYo != null) Destroy(matYo);
        if (matOtro != null) Destroy(matOtro);
    }

    void LateUpdate()
    {
        if (yo == null || shaderFusion == null) return;
        if (otro == null) BuscarOtro();

        bool fusionar = otro != null && otro.gameObject.activeInHierarchy;
        if (!fusionar)
        {
            if (otroEraActivo)
            {
                yo.sharedMaterial = matSpriteDefault;
                otroEraActivo = false;
            }
            return;
        }

        if (matYo == null)
        {
            matYo = new Material(shaderFusion);
            matYo.name = "OscuridadFusion_Pp";
        }
        if (matOtro == null)
        {
            matOtro = new Material(shaderFusion);
            matOtro.name = "OscuridadFusion_Farol";
        }

        if (!otroEraActivo)
        {
            yo.sharedMaterial = matYo;
            otro.sharedMaterial = matOtro;
            otroEraActivo = true;
        }

        Aplicar(matYo, yo, otro, false);
        Aplicar(matOtro, otro, yo, true);
    }

    void Aplicar(Material mat, SpriteRenderer self, SpriteRenderer other, bool secundario)
    {
        self.sharedMaterial = mat;
        mat.SetFloat("_IsSecondary", secundario ? 1f : 0f);
        mat.SetFloat("_OtherActive", other != null && other.gameObject.activeInHierarchy ? 1f : 0f);

        if (other == null || other.sprite == null || other.sprite.texture == null)
            return;

        Transform t = other.transform;
        Vector3 escala = t.lossyScale;
        if (Mathf.Abs(escala.z) < 0.001f) escala.z = 1f;
        Matrix4x4 worldToLocal = Matrix4x4.TRS(t.position, t.rotation, escala).inverse;

        Bounds b = other.sprite.bounds;
        Sprite sp = other.sprite;
        Texture tex = sp.texture;
        Rect r = sp.textureRect;

        mat.SetTexture("_OtherTex", tex);
        mat.SetMatrix("_OtherWorldToLocal", worldToLocal);
        mat.SetVector("_OtherBoundsMin", b.min);
        mat.SetVector("_OtherBoundsSize", b.size);
        mat.SetVector("_OtherAtlasST", new Vector4(
            r.width / tex.width,
            r.height / tex.height,
            r.x / tex.width,
            r.y / tex.height));
    }
}
