# Ambientes de Nivel 2 — mezcla manual

Los cinco archivos originales completos suenan juntos desde el inicio y se repiten durante todo Nivel_2. Sin disparadores, recortes de eventos, cambios de volumen por posición ni panoramas automáticos.

Cada WAV incorpora un crossfade circular suave de 150 ms entre el final y el inicio. Conservan tono, velocidad, nivel original, frecuencia de muestreo y canales; se exportan como PCM de 24 bits. El período del loop es 150 ms menor por el solapamiento. No se aplica la ecualización ni el cambio de tono de la primera propuesta.

## Mezclar
Abrir `Mixer ambientes nivel 2.mixer` (doble clic) y activar `Edit in Play Mode` en la ventana Audio Mixer para mezclar durante Play. Hay cinco faders: Ambiente 1, 2, 3, 4 y 5, todos inicialmente a 0 dB. Los originales tienen distinta intensidad: misma ganancia no implica igual sonoridad percibida.

Los cinco van a `Ambientes N2 - PREMIX`, inicialmente a -12 dB para dejar margen a la suma. Ese submaster va directamente al Master del mixer general existente (`Audio nivel 1/Mixer.mixer`), conservando su ajuste actual. No se ha cambiado la mezcla del nivel 1.

Los AudioSources están a volumen 1, pitch 1, panorama central, loop activo. Ningún script sobrescribe los faders. Los cambios del mixer son los controles permanentes de esta mezcla. Seleccionar el snapshot antes de guardar y guardar el proyecto tras ajustar.

En Play, Hierarchy muestra `Audio nivel 2 - Ambientes` con los cinco hijos. El asset `HopeLevel2AmbienceSettings` ahora solo contiene los clips y sus cinco salidas. Los archivos procesados de la primera prueba siguen guardados pero no están activos. Los originales externos tampoco se modifican.
