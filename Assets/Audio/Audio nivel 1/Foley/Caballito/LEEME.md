# Caballito de resorte

El componente HopeCaballitoAudio se conecta a caballito_0 al entrar en Play y en cambios de escena. Crea dos fuentes hijas, resorte y apoyo, conectadas al submixer Foley. Sigue la fase de la animacion, respeta su pausa y se atenua con la distancia a Hope (pleno hasta 2 unidades, silencio desde 11). El panorama cambia segun el lado donde queda el objeto.

Editar Resources/HopeCaballitoSettings.asset: volumen general, volumenApoyo (0 desactiva el clac), distancias y momentosNormal (segundos del ciclo de animacion). Tres rechinidos se alternan para evitar una repeticion identica. No se modifica la escena ni la animacion.

Fuentes, recortes y tratamientos en Procedencia_Caballito.json. Primera version para evaluar jugando.
