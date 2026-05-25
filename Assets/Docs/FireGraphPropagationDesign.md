# Fire Graph Propagation Design

## Objetivo

Este documento describe la arquitectura actual del grafo de propagacion de fuego.

El sistema representa un incendio que avanza por rutas visibles entre puntos del escenario. El fuego no debe aparecer de forma instantanea en objetos aislados; debe recorrer aristas para que el jugador pueda leer la direccion, la velocidad y la expansion del incendio.

La implementacion actual prioriza una propagacion clara sobre suelo plano. Terreno irregular, paredes, objetos con volumen, vegetacion critica, viento y extincion avanzada quedan como extensiones posteriores.

## Modelo General

El incendio se modela como un grafo:

- `FireGraphRoot`: raiz del grafo y punto de organizacion en la jerarquia.
- `FireNode`: nodo logico del grafo.
- `FireEdge`: arista logica y visual por donde avanza el fuego.
- `FireSimulationManager`: construye el grafo runtime y activa aristas desde nodos encendidos.

La separacion importante es:

```text
Nodo = estado logico del punto alcanzado.
Arista = propagacion visual y temporal del fuego.
```

Los nodos ya no son responsables de mostrar llamas permanentes. La arista instancia el frente de fuego y los patches que quedan sobre el suelo.

## Jerarquia De Objetos

La jerarquia recomendada para cualquier grafo de fuego es:

```text
FireGraphRoot
+-- Nodes
|   +-- FireNode_Spark
|   +-- FireNode_01
|   +-- FireNode_02
|   +-- FireNode_03
|   +-- FireNode_04
+-- Edges
    +-- FireEdge_Ground_BigSimple
    +-- FireEdge_Ground_BigSimple (1)
    +-- FireEdge_Ground_BigSimple (2)
    +-- FireEdge_Ground_BigSimple (3)
```

Reglas de organizacion:

- Todos los nodos del grafo deben ser hijos de `FireGraphRoot/Nodes`.
- Todas las aristas del grafo deben ser hijas de `FireGraphRoot/Edges`.
- `FireGraphRoot` debe tener los componentes `FireGraphRoot` y `FireSimulationManager`.
- `FireGraphRoot.nodesRoot` debe apuntar al objeto `Nodes`.
- `FireGraphRoot.edgesRoot` debe apuntar al objeto `Edges`.
- Cada arista debe tener asignados sus campos `source` y `target`.
- El inicio del incendio se define colocando uno o mas nodos con `state = Burning`.

Esta estructura evita que los nodos queden mezclados con arboles, tiendas, rocas u otros objetos decorativos de la escena.

## Prefabs Actuales

Los prefabs principales son:

```text
Assets/Prefabs/FireGraph/FireGraphRoot.prefab
Assets/Prefabs/FireGraph/FireEdge_Ground_BigSimple.prefab
Assets/Prefabs/FireNodes/FireNode_Spark.prefab
Assets/Prefabs/FireNodes/FireNode_Sensitive.prefab
```

### FireGraphRoot.prefab

Contiene:

- `FireGraphRoot`
- `FireSimulationManager`
- hijo `Nodes`
- hijo `Edges`

Valores actuales:

```text
treatConnectionsAsBidirectional: true
includeInactiveNodes: true
spreadInterval: 1.25
minimumEdgeDistance: 0.1
propagationMultiplier: 0.5
```

`startingNode` ya no existe. El manager detecta como fuentes iniciales todos los nodos que ya tengan `state = Burning` cuando inicia la escena.

### FireEdge_Ground_BigSimple.prefab

Representa una arista de propagacion sobre suelo plano.

Usa:

```text
frontFireEffectPrefab: VFX_Fire_01_Big_Simple
groundFirePatchPrefab: VFX_Fire_01_Big_Simple
nodeArrivalEffectPrefab: VFX_Fire_01_Small_Simple
```

Parametros actuales:

```text
propagationSpeed: 1.0
propagationCostMultiplier: 2.0
fireEffectLocalOffset: (0, 0, 0)
fireEffectLocalScale: (0.7, 0.7, 0.7)
firePatchLocalScale: (0.6, 0.6, 0.6)
firePatchSpacing: 0.6
firePatchLifetime: 18.0
nodeArrivalEffectLifetime: 2.0
muteFirePatchAudio: true
useDynamicPatchScale: true
firePatchInitialScaleFactor: 0.1
firePatchEdgeScaleFactor: 0.65
firePatchGrowDuration: 3.0
firePatchFadeDuration: 3.0
```

Esta version fue elegida porque comunica mejor la propagacion. La variante de fuego de suelo pequeno sirve mejor para fuego estatico, como una fogata.

### FireNode_Spark.prefab

Nodo inicial del incendio.

Valores actuales:

```text
nodeType: Spark
state: Burning
ignitionResistance: 0.0
firePower: 4.0
exposureDecayRate: 0.0
timeToDestroy: 10.0
burningEffectPrefab: None
```

Aunque su estado inicial es `Burning`, no muestra fuego permanente propio. Su funcion es activar las aristas conectadas.

### FireNode_Sensitive.prefab

Nodo combustible normal.

Valores actuales:

```text
nodeType: Structure
state: Off
ignitionResistance: 1.2
firePower: 4.0
exposureDecayRate: 0.05
timeToDestroy: 10.0
burningEffectPrefab: None
```

## Tipo De Grafo

Actualmente el grafo se interpreta como bidireccional.

Una sola arista:

```text
A -- B
```

puede propagar fuego en ambos sentidos:

```text
A -> B
B -> A
```

Esto se controla con:

```text
FireSimulationManager.treatConnectionsAsBidirectional = true
```

Mas adelante se puede desactivar para representar rutas dirigidas, viento o pendiente.

## Estados Del Nodo

Cada nodo usa `FireNodeState`:

```text
Off
Burning
Destroyed
```

### Off

El nodo no ha sido alcanzado por el fuego. Puede ser destino de una arista en propagacion.

### Burning

El nodo fue alcanzado. En este estado:

- puede activar aristas conectadas;
- avanza su tiempo interno de combustion;
- no instancia fuego visual permanente.

### Destroyed

El nodo ya fue consumido. No puede volver a encenderse.

## Estados De La Arista

Cada arista usa `FireEdgeState`:

```text
Idle
Burning
Burned
```

### Idle

La arista existe, pero todavia no esta propagando fuego.

### Burning

La arista esta propagando fuego desde un nodo origen hacia un nodo destino.

Durante este estado:

- se crea un efecto de frente de fuego;
- el frente avanza por interpolacion lineal;
- se crean patches de fuego sobre el tramo ya recorrido.

### Burned

El fuego ya recorrio la arista. Al completar la propagacion, la arista enciende el nodo destino si todavia puede encenderse.

## Flujo De Propagacion

La secuencia actual es:

```text
1. FireSimulationManager construye el grafo runtime.
2. El manager busca nodos con state = Burning.
3. Cada nodo Burning queda registrado como fuente activa.
4. Por cada nodo Burning, el manager intenta activar sus aristas conectadas.
5. FireEdge inicia la propagacion lineal desde ese nodo.
6. FireEdge mueve el frente de fuego entre source y target.
7. FireEdge crea patches de fuego a lo largo del tramo recorrido.
8. Cuando progress llega a 1.0, FireEdge enciende el nodo destino.
9. El nodo destino activa sus propias aristas conectadas.
10. Los nodos consumidos pasan a Destroyed y no pueden encenderse otra vez.
```

La propagacion visual vive principalmente en `FireEdge`, no en `FireNode`.

## Construccion Runtime Del Grafo

`FireSimulationManager.BuildGraphFromScene()` limpia el grafo runtime y vuelve a leer la escena.

Si `graphRoot` existe:

```text
nodes = graphRoot.GetNodes(includeInactiveNodes)
edges = graphRoot.GetEdges(includeInactiveNodes)
```

Si no existe:

```text
nodes = FindObjectsOfType<FireNode>(includeInactiveNodes)
edges = FindObjectsOfType<FireEdge>(includeInactiveNodes)
```

Por eso la arquitectura recomendada es usar siempre `FireGraphRoot`: reduce ruido y evita que objetos fuera del grafo entren por accidente.

Cada `FireEdge` valida:

```text
enabledForPropagation == true
source != null
target != null
source != target
```

Luego el manager agrega la conexion `source -> target`. Si el grafo es bidireccional, agrega tambien `target -> source` usando la misma arista.

## Propagacion Visual

La arista usa dos capas visuales:

```text
Front Fire Effect
Ground Fire Patches
```

El frente de fuego se mueve con:

```text
position = Lerp(activeSource.position, activeTarget.position, progress)
```

El progreso avanza con:

```text
progress += (propagationSpeed * deltaTime) / (distance * propagationCostMultiplier)
```

Esto hace que una arista mas larga tarde mas en completarse. `propagationCostMultiplier` permite encarecer una arista sin mover sus nodos.

Los patches se crean progresivamente segun la distancia recorrida:

```text
burnedDistance = totalDistance * progress
while nextPatchDistance <= burnedDistance:
    create patch
    nextPatchDistance += firePatchSpacing
```

Esto permite que el fuego no solo se mueva, sino que deje una zona encendida detras del frente.

## Patches De Fuego

Cada patch se instancia en una posicion interpolada entre los nodos:

```text
position = Lerp(start, end, normalizedDistance) + fireEffectLocalOffset
```

`firePatchSpacing` controla la separacion entre patches:

- valores menores crean fuego mas continuo;
- valores mayores reducen instancias pero pueden dejar huecos visibles.

El gizmo de la arista dibuja una esfera en cada posicion estimada de patch. Esto permite ajustar `firePatchSpacing` desde la vista Scene antes de ejecutar Play.

## Escala Dinamica De Patches

El sistema ya tiene parametros para variar la escala de cada patch:

```text
useDynamicPatchScale
firePatchInitialScaleFactor
firePatchEdgeScaleFactor
firePatchGrowDuration
firePatchFadeDuration
firePatchGrowthCurve
firePatchFadeCurve
```

La intencion de esta seccion es:

- que el patch nazca pequeno;
- crezca hasta una escala objetivo;
- se mantenga visible durante su vida util;
- disminuya antes de destruirse.

Tambien existe una escala maxima calculada por posicion dentro de la arista:

```text
centerFactor = 1 - abs(normalizedDistance - 0.5) * 2
scaleFactor = Lerp(firePatchEdgeScaleFactor, 1, centerFactor)
peakScale = firePatchLocalScale * scaleFactor
```

Con esto, los patches cercanos al centro de la arista pueden verse mas grandes que los patches cercanos a los nodos. Esta parte todavia queda pendiente de revisar y ajustar manualmente para que el crecimiento sea visible desde el inicio de la propagacion, no solo al apagarse.

## Gizmos

Los gizmos son herramientas de edicion y no afectan la simulacion.

`FireNodeGizmo` dibuja el nodo con:

```text
Gizmos.DrawSphere
```

`FireEdge` dibuja:

- una linea entre `source` y `target`;
- una esfera por cada patch estimado segun `firePatchSpacing`.

Para verlos en Unity, la ventana Scene debe tener activado el boton `Gizmos`.

## Como Crear Un Grafo Manualmente

1. Arrastrar `FireGraphRoot.prefab` a la escena.
2. Crear o arrastrar nodos dentro de `FireGraphRoot/Nodes`.
3. Usar `FireNode_Spark` para el inicio del incendio, o colocar cualquier `FireNode` con `state = Burning`.
4. Usar `FireNode_Sensitive` para nodos combustibles normales.
5. Crear aristas dentro de `FireGraphRoot/Edges` usando `FireEdge_Ground_BigSimple`.
6. En cada arista, asignar:

```text
source: nodo A
target: nodo B
```

7. Verificar en Scene que los gizmos de nodos y patches forman la ruta esperada.
8. Ejecutar Play.

Ejemplo de dos caminos:

```text
FireGraphRoot
+-- Nodes
|   +-- Spark_Campfire
|   +-- Left_01
|   +-- Left_02
|   +-- Right_01
|   +-- Right_02
|   +-- Goal
+-- Edges
    +-- Edge_Spark_Left01
    +-- Edge_Left01_Left02
    +-- Edge_Left02_Goal
    +-- Edge_Spark_Right01
    +-- Edge_Right01_Right02
    +-- Edge_Right02_Goal
```

## Escena De Prueba

La escena actual para probar propagacion es:

```text
Assets/Scenes/FirePropagationTest.unity
```

Esta escena contiene un grafo basico con rutas de propagacion. Sirve para probar el algoritmo antes de llevarlo a la escena del tutorial.

## Herramientas De Editor

### FireGraphPrefabFactory

Ruta:

```text
Assets/Editor/FireGraphPrefabFactory.cs
```

Menus principales:

```text
Tools/Fire Simulation/Create Graph Architecture Prefabs
Tools/Fire Simulation/Apply Dynamic Fire Scale Defaults
```

Regenera o actualiza:

- `FireGraphRoot.prefab`
- `FireEdge_Ground_BigSimple.prefab`
- `FireNode_Spark.prefab`
- `FireNode_Sensitive.prefab`

### FireGraphBuilder

Ruta:

```text
Assets/Editor/FireGraphBuilder.cs
```

Menu:

```text
Tools/Fire Simulation/Validate Runtime Fire Graph
```

Sirve para validar nodos y aristas explicitas en la escena.

### FireGraphSceneTools

Ruta:

```text
Assets/Editor/FireGraphSceneTools.cs
```

Contiene utilidades para organizar nodos bajo la raiz del grafo.

## Codigo Obsoleto Eliminado

La arquitectura actual ya no usa conexiones declaradas dentro del nodo. Las conexiones reales deben estar representadas por objetos `FireEdge` dentro de `FireGraphRoot/Edges`.

Tambien se eliminaron:

- `FireGraphIdentity.cs`
- `startingNode`
- `igniteStartingNodeOnStart`
- `FireObject.cs`

El script de nodo actual es `FireNode.cs`.

## Respaldo Del Proyecto

No se deben guardar copias de seguridad con scripts `.cs` dentro de `Assets`, porque Unity compila todos los scripts dentro de esa carpeta y puede generar errores por clases duplicadas.

El respaldo del avance debe hacerse con Git y GitHub. Si se necesita crear una copia temporal local, debe quedar fuera de `Assets` y eliminarse antes de volver a compilar el proyecto.

## Proximas Mejoras

Prioridad inmediata:

```text
Ajustar el cambio dinamico de escala en cada patch.
```

Objetivo esperado:

- el fuego debe crecer visualmente cuando aparece un patch;
- la variacion de tamano debe ayudar a leer la propagacion;
- el apagado debe ser una fase posterior, no el unico momento donde se note el cambio de escala.

Despues de eso, el siguiente paso tecnico es adaptar la colocacion de patches a terrenos no planos mediante raycast contra el suelo.

## Conclusion

La arquitectura actual separa la logica del nodo y la propagacion visible:

```text
FireNode = estado del punto alcanzado.
FireEdge = recorrido visible del fuego.
FireSimulationManager = activacion del grafo runtime.
FireGraphRoot = organizacion de la jerarquia.
```

Esta base permite construir el tutorial con rutas claras de propagacion y deja espacio para extender el sistema hacia vegetacion, objetos reales, paredes, terreno irregular y mecanicas de extincion.
