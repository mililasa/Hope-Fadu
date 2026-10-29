# Faroles: canales 14, 15 y 16

Activacion desde LuzFondoInteractiva.Activar, al aceptar la E cerca de un farol apagado. Usa las referencias primeraLuz/segundaLuz actuales. TerceraLuz no tiene interaccion en este controlador; no se modifica jugabilidad para agregarla.

Chispa inmediata; encendido y comienzo de llama 0,1 s despues, como en la integracion anterior. Llama en loop con entrada de 0,15 s, volumen completo hasta 2,5 unidades y silencio desde 8; paneo segun posicion relativa de Hope. No se retriggera por repetir E sobre un farol encendido. Reiniciar cancela la secuencia pendiente y apaga todas las voces; permite encender otra vez. Volver a acercarse conserva la llama sin repetir la chispa.

Ganancias del Reaper V2: chispa 0,316228; encendido 0,223872; fuego 0,035481 por 0,09772372 (primer punto de envolvente). La automatizacion de volumen/pan del video se reemplaza por distancia al farol; no se aplica el master, coherente con el audio ya integrado. No se agrega motivo musical ni ducking de musica.

Resources/HopeLanternSettings permite ajustar clips, niveles, retraso y alcance. En Play aparecen tres fuentes bajo PrimeraLuz/SegundaLuz al encender. Probar E lejos (silencio), encender, repetir E, alejarse, volver, segundo farol y muerte/reinicio incluso antes de completar el encendido.

Credito: Fire Loop, qubodup, https://opengameart.org/content/fire-loop, Creative Commons Attribution 3.0, https://creativecommons.org/licenses/by/3.0/. Fuente adaptada para llama pequena, convertida a WAV PCM24/48 kHz; esta version recorta extremos y empalma 0,25 s para repeticion continua. Conservar este credito al distribuir.
