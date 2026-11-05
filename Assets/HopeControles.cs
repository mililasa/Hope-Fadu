using UnityEngine;

// Teclas de Hope (pp). El Input Manager queda solo para el mando;
// el teclado se lee aca para poder reasignarlo desde el menu.
public static class HopeControles
{
    public enum Accion
    {
        Izquierda,
        Derecha,
        Arriba,
        Abajo,
        Saltar,
        Interactuar,
        Pausa
    }

    static readonly KeyCode[] inicialPrincipal =
    {
        KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S, KeyCode.Space, KeyCode.E, KeyCode.Escape
    };

    static readonly KeyCode[] inicialAlternativa =
    {
        KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow,
        KeyCode.None, KeyCode.None, KeyCode.P
    };

    static KeyCode[] principal;
    static KeyCode[] alternativa;

    public static int Cantidad => inicialPrincipal.Length;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CargarAlInicio()
    {
        Asegurar();
        Cargar();
    }

    static void Asegurar()
    {
        if (principal != null) return;
        principal = (KeyCode[])inicialPrincipal.Clone();
        alternativa = (KeyCode[])inicialAlternativa.Clone();
    }

    public static void Cargar()
    {
        Asegurar();
        for (int i = 0; i < Cantidad; i++)
        {
            string clave = Clave(i);
            if (!PlayerPrefs.HasKey(clave + "p")) continue;
            principal[i] = (KeyCode)PlayerPrefs.GetInt(clave + "p", (int)inicialPrincipal[i]);
            alternativa[i] = (KeyCode)PlayerPrefs.GetInt(clave + "a", (int)inicialAlternativa[i]);
        }
    }

    public static void Guardar()
    {
        Asegurar();
        for (int i = 0; i < Cantidad; i++)
        {
            string clave = Clave(i);
            PlayerPrefs.SetInt(clave + "p", (int)principal[i]);
            PlayerPrefs.SetInt(clave + "a", (int)alternativa[i]);
        }
        PlayerPrefs.Save();
    }

    static string Clave(int i)
    {
        return "hope.tecla." + i + ".";
    }

    public static KeyCode Tecla(Accion accion, bool alternativaDeLaAccion)
    {
        Asegurar();
        int i = (int)accion;
        return alternativaDeLaAccion ? alternativa[i] : principal[i];
    }

    public static void Asignar(Accion accion, bool alternativaDeLaAccion, KeyCode tecla)
    {
        Asegurar();
        int i = (int)accion;
        if (alternativaDeLaAccion) alternativa[i] = tecla;
        else principal[i] = tecla;
        Guardar();
    }

    public static void Restaurar(Accion accion, bool alternativaDeLaAccion)
    {
        Asegurar();
        int i = (int)accion;
        if (alternativaDeLaAccion) alternativa[i] = inicialAlternativa[i];
        else principal[i] = inicialPrincipal[i];
        Guardar();
    }

    public static void RestaurarTodo()
    {
        principal = (KeyCode[])inicialPrincipal.Clone();
        alternativa = (KeyCode[])inicialAlternativa.Clone();
        Guardar();
    }

    public static bool Mantenida(Accion accion)
    {
        if (PausaJuego.EstaPausado) return false;
        return Abajo(Tecla(accion, false)) || Abajo(Tecla(accion, true));
    }

    public static bool Presionada(Accion accion)
    {
        if (PausaJuego.EstaPausado && accion != Accion.Pausa) return false;
        return Golpe(Tecla(accion, false)) || Golpe(Tecla(accion, true));
    }

    static bool Abajo(KeyCode tecla)
    {
        return tecla != KeyCode.None && Input.GetKey(tecla);
    }

    static bool Golpe(KeyCode tecla)
    {
        return tecla != KeyCode.None && Input.GetKeyDown(tecla);
    }

    public static float Horizontal()
    {
        if (PausaJuego.EstaPausado) return 0f;
        float teclado = 0f;
        if (Mantenida(Accion.Izquierda)) teclado -= 1f;
        if (Mantenida(Accion.Derecha)) teclado += 1f;
        float mando = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(teclado) >= Mathf.Abs(mando)) return Mathf.Clamp(teclado, -1f, 1f);
        return mando;
    }

    public static float Vertical()
    {
        if (PausaJuego.EstaPausado) return 0f;
        float teclado = 0f;
        if (Mantenida(Accion.Abajo)) teclado -= 1f;
        if (Mantenida(Accion.Arriba)) teclado += 1f;
        float mando = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(teclado) >= Mathf.Abs(mando)) return Mathf.Clamp(teclado, -1f, 1f);
        return mando;
    }

    public static string Nombre(KeyCode tecla)
    {
        switch (tecla)
        {
            case KeyCode.None: return "-";
            case KeyCode.Space: return "ESPACIO";
            case KeyCode.LeftArrow: return "IZQ";
            case KeyCode.RightArrow: return "DER";
            case KeyCode.UpArrow: return "ARR";
            case KeyCode.DownArrow: return "ABA";
            case KeyCode.LeftShift: return "L-SHIFT";
            case KeyCode.RightShift: return "R-SHIFT";
            case KeyCode.LeftControl: return "L-CTRL";
            case KeyCode.RightControl: return "R-CTRL";
            case KeyCode.LeftAlt: return "L-ALT";
            case KeyCode.RightAlt: return "R-ALT";
            case KeyCode.Return: return "ENTER";
            case KeyCode.KeypadEnter: return "ENTER";
            case KeyCode.Escape: return "ESC";
            case KeyCode.Tab: return "TAB";
            case KeyCode.Backspace: return "BORRAR";
            case KeyCode.CapsLock: return "BLOQ";
        }

        string texto = tecla.ToString();
        if (texto.StartsWith("Alpha")) return texto.Substring(5);
        if (texto.StartsWith("Keypad")) return "NUM" + texto.Substring(6);
        return texto.ToUpperInvariant();
    }
}
