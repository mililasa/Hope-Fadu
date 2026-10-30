# Muerte — 8bit Death Whirl

Autor: Fupi. Fuente: https://opengameart.org/content/8bit-death-whirl
Descarga: https://opengameart.org/sites/default/files/vgdeathsound.wav
Licencia publicada: CC0, https://creativecommons.org/publicdomain/zero/1.0/
Original sin editar, mono 44,1 kHz, 3,178 s. Sin capas adicionales ni procesamiento creativo.

Disparos: caida por debajo de alturaDeCaida y agarre de HandChaser mediante PlayerController2D.Morir. VolverAlCheckPoint por transicion de nivel conserva su comportamiento sin efecto de muerte. Fuente 2D bajo Pp, sin Loop, puede terminar durante respawn. Doble aviso en menos de 0,15 s se ignora; una muerte posterior reinicia el clip sin apilar voces. No cambia tiempos de respawn ni movimiento.

Configuracion Resources/HopeDeathSettings: Volumen Canal inicialmente 0,18 y Volumen Fragmento 1. Probar muerte por mano, pozo y repeticion; ajustar balance escuchando en juego. Compilacion verificada; prueba Play pendiente.
