# Prototipo GrowingCity

[English](../en/README.md) · [한국어](../ko/README.md) · [日本語](../ja/README.md) · [简体中文](../zh-Hans/README.md) · [繁體中文](../zh-Hant/README.md) · [Deutsch](../de/README.md) · [Français](../fr/README.md) · [Español](../es/README.md) · [Português](../pt/README.md)

Bombardee una ciudad que crece sola. Los edificios vecinos se combinan y evolucionan; las zonas destruidas se reconstruyen.

Abra Assets/Scenes/City Prototype.unity en Unity 6000.0.83f1 con URP 17.0.4. Es un prototipo Unity; su ejecución en navegador no está verificada.

Arrastre sobre la ciudad y suelte para enviar la formación en esa dirección. Los aviones entran y salen fuera de la pantalla. 1: bomba, 2: nuclear, Espacio: pausa/continuar, Esc: cancelar.

Cada avión empieza con una bomba por segundo. Mejore cadencia, capacidad, radio, potencia, formación, velocidad, blindaje y vida para el próximo vuelo. El crecimiento urbano ya no se compra.

El crecimiento acelera con el tiempo de juego: 1x al inicio, 2x a los 60 segundos y máximo 8x a los 420 segundos, indicado en el HUD. Solo edificios de nivel 3 o superior disparan misiles. Cada duodécima construcción es un depósito de combustible amarillo, municiones rojo o central cian. Destruirlos causa daño cercano y reacciones en cadena. El oro se paga una vez por toda la cadena; baja el desarrollo y quedan ruinas reconstruibles.

Cada avión que llega añade un 50% fijo de carga normal. Dos llegadas dan un uso; el excedente se guarda. La recarga pasiva tarda 10 segundos de juego y llena como máximo un uso.

El nuclear tiene 100 segundos de recarga independiente desde la aceptación de una orden válida, incluidas las órdenes en cola. Empieza disponible y no consume carga normal. Las llegadas y la velocidad 3x no lo reducen. La pausa detiene el contador; Nueva ciudad lo reinicia.

Los edificios disparan misiles según su nivel con seguimiento limitado. Los aviones pueden recibir daño y ser derribados. Los edificios evolucionados tienen más vida y pueden reducirse por daño. Destruir otorga oro y reduce el desarrollo urbano.

El primer inicio sigue el idioma del sistema. El botón alterna Sistema y nueve idiomas; la elección se guarda en este dispositivo. Los idiomas no compatibles usan inglés.

## Controles y ajustes

Los originales externos Assets/polyperfect/Low Poly Epic City/ no se modifican. Use recursos propios y adaptadores. No redistribuya por separado los recursos de la tienda.

Límites: imagen real de Unity, cambio de idioma en juego, despliegue web y equilibrio sin verificar en esta tarea. Compilar y probar la lógica no sustituye Play.

[HTML](index.html)

Los edificios explosivos tienen el cuerpo azul intenso; los cimientos amarillos, rojos y cian identifican el tipo. La destrucción combina caída lateral, torsión y elevación explosiva. Las secciones de fachada y escombros usan física; algunas salen de pantalla o pasan sobre tejados. Hasta 36 secciones comparten el presupuesto existente. Los efectos y el shader requieren verificación real en Play.

La ciudad empieza con96 casillas ocupadas, combinaciones y edificios especiales. La simulación estándar independiente produce82 edificios. Cada ciclo añade hasta4 casillas en bordes conectados y separados mediante azar ponderado, sin ordenar por distancia al centro. El límite es24×24,576 casillas (aproximadamente384×384unidades Unity con casilla16). Al llenarse cesa la expansión, pero se reconstruye lo destruido. Entre90 y300 segundos de juego aumentan los niveles altos directos; desde300 segundos, nuevas construcciones y reconstrucciones normales tienen75% de nivel3,20% de nivel2 y5% básico. Los especiales mantienen sus reglas. Nueva ciudad reinicia el progreso.
