# TRUST.EXE — menú y selección de personajes

Unity **6000.5.5f1**, Windows, Input System. Rama: `menu-y-seleccion-de-personajes`.

## Probar

1. Abrir este directorio como proyecto en Unity y esperar la importación.
2. Abrir `Assets/00_Scenes/MainMenu.unity` y pulsar Play. En la PC de desarrollo, Play también está configurado para iniciar esta escena desde SampleScene.
3. Pulsar PLAY. P1 elige con WASD y confirma con E; P2 usa flechas y Enter (también Enter numérico).
4. R deshace P1; Shift derecho deshace P2. Escape deshace confirmaciones o vuelve. Con ratón, asignarlo primero a P1/P2, elegir una tarjeta y pulsar CONFIRMAR / DESHACER.
5. Al confirmar dos avatares distintos se reproduce su entrada y comienza la historia. Espacio/Enter revela el texto o avanza. SALTAR INTRO llega a la pantalla de equipo listo.

## Incluido

- Los 12 avatares originales; elección simultánea con cursores independientes y roles físico/virtual.
- Animación Think, secuencia de caminata, entrada lateral, ampliación de impacto y banda con nombre durante 1,5 segundos al confirmar.
- E.V.A. animada con cuatro frames, fondo con líneas de datos, título flotante, transiciones narrativas y texto progresivo.
- Respuesta sonora sintetizada localmente, sin descargas ni dependencias adicionales.
- Interfaz de referencia 1600×900 centrada y adaptada al espacio disponible, con márgenes en otras proporciones.
- Catálogo serializado con referencias directas a las texturas originales. No mueve, recorta ni cambia los pivotes de los sprites del equipo.

## Conectar el Tutorial

Esta entrega implementa el menú y la introducción. La carpeta recibida solo contenía SampleScene; no incluye todavía escenas jugables ni puzzles.

Cuando exista el Tutorial, añadirlo habilitado a Build Profiles y ajustar `tutorialScene` en `Assets/01_Script/Menu/MenuCatalog.asset` si su nombre no es `Tutorial`. Entonces aparece ENTRAR AL TUTORIAL y se carga asíncronamente. Si falta la escena, la interfaz lo comunica sin intentar cargarla.

El código de generación de jugadores debe leer `TrustEXE.Menu.LocalParty.Explorer` y `LocalParty.Operator`. Son los identificadores de avatar, independientes del rol. `IsReady` valida que hay una pareja. Se conservan entre escenas y se limpian al volver al inicio o iniciar otra sesión; no son un checkpoint persistente.

## Estructura

- `Assets/00_Scenes/MainMenu.unity`: escena inicial incluida en el build.
- `Assets/01_Script/Menu/TrustMenuController.cs`: interfaz, controles, animación, historia y transición.
- `TrustMenuCatalog.cs` y `MenuCatalog.asset`: datos de avatares y E.V.A.; estado local de la pareja.
- `Editor/TrustMenuSetup.cs`: reconstrucción explícita del catálogo y validación de flujo. El menú TRUST.EXE del editor permite abrir/preparar la escena. La interfaz se construye al ejecutar Play.

## Validación reproducible

Con MainMenu en Play Mode, escribir `validate` en `Library/TrustMenuRequest.txt`. El editor ejecuta comprobaciones de catálogo, navegación por los doce avatares, wrap, duplicados, bloqueo, deshacer, identidad de sesión, revelado/avance de historia, recorrido completo, salto y reinicio. El resultado se escribe en `Library/TrustMenuValidation.txt`; la prueba regresa al menú.

Comprobar también visualmente la entrada de ambos personajes, clic de tarjetas, teclado compartido, historia y salida a inicio. La prueba automatizada de lógica no verifica ghosting del teclado físico. Animaciones y tonos se ejecutan con tiempo no escalado.

La documentación maestra se usó como referencia narrativa y de roles. Sus prompts para construir todo el juego no forman parte de esta entrega.
