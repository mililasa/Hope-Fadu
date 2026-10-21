# Explosion pintura

Tres capas separadas de la prueba 02: detonacion belica, energia futurista y salpicadura de pintura. Cada WAV conserva sus desplazamientos internos y su cola espacial; se reproducen juntos con PlayScheduled. La suma con ganancias 0.85, 0.32 y 0.4 reconstruye la maqueta de referencia, antes del volumen general y la atenuacion por distancia.

Editar Resources/HopeExplosionSettings.asset para controlar Volumen Detonacion, Volumen Energia y Volumen Pintura de forma independiente. Volumen General controla el conjunto. La distancia y el panorama tambien son configurables.

Al entrar en Play, HopeExplosionAudio se agrega automaticamente a explosion-Sheet_0. Dispara una vez al comienzo de cada ciclo de la animacion explosion, sin inventar un golpe al acercarse Hope a mitad de ciclo. Tres AudioSource hijos se asignan al submixer Foley. No modifica la escena ni las animaciones.

Primera implementacion: falta evaluar volumen y sincronizacion perceptual en Play. Fuentes y licencia de la biblioteca Sonniss GDC 2024 en Procedencia_Explosion.json.
