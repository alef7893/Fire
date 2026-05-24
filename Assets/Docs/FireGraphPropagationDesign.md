# Fire Graph Propagation Design

## Objetivo

Este documento describe la arquitectura actual del grafo de propagacion de fuego.

El objetivo del sistema es representar un incendio que avanza por rutas visibles entre puntos del escenario. El fuego no debe aparecer de forma instantanea en nodos aislados; debe recorrer aristas para que el jugador pueda leer la direccion y la expansion del incendio.

La implementacion actual prioriza un suelo plano y una propagacion basica pero clara. La adaptacion a terrenos irregulares, vegetacion critica, viento y extincion avanzada queda como trabajo posterior.

## Modelo General

El incendio se modela como un grafo:

- `FireGraphRoot`: raiz del grafo y punto de organizacion en la jerarquia.
- `FireObject`: nodo logico del grafo.
- `FireEdge`: arista logica y visual por donde avanza el fuego.
- `FireSimulationManager`: construye el grafo runtime y activa las aristas cuando un nodo es alcanzado.

La separacion importante es esta:

```text
Nodo = estado logico del combustible.
Arista = propagacion visual y temporal del fuego.
```

Los nodos ya no son responsables de mostrar fuego permanente. La arista es quien instancia y mueve los efectos de fuego.

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

- Todos los nodos deben ser hijos de `FireGraphRoot/Nodes`.
- Todas las aristas deben ser hijas de `FireGraphRoot/Edges`.
- `FireGraphRoot` debe tener los componentes `FireGraphRoot` y `FireSimulationManager`.
- Cada arista debe tener asignados sus campos `source` y `target`.
- El `FireSimulationManager.startingNode` debe apuntar al nodo inicial, normalmente `FireNode_Spark`.

Esta estructura evita que los nodos queden dispersos entre objetos decorativos de la escena.

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

Valores recomendados actuales:

```text
treatConnectionsAsBidirectional: true
includeInactiveNodes: true
spreadInterval: 1.25
minimumEdgeDistance: 0.1
propagationMultiplier: 0.5
```

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
fireEffectLocalOffset: (0, 0.25, 0)
fireEffectLocalScale: (0.7, 0.7, 0.7)
firePatchLocalScale: (0.6, 0.6, 0.6)
firePatchSpacing: 0.85
firePatchLifetime: 18.0
nodeArrivalEffectLifetime: 2.0
muteFirePatchAudio: true
```

Esta version fue elegida porque funciona mejor para comunicar propagacion. La variante `VFX_Fire_Floor_01` se considera mas util para fuego estatico, como una fogata.

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

Mas adelante se puede desactivar para representar viento, pendiente o rutas dirigidas.

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

El fuego ya recorrio la arista. Al completar la propagacion, la arista enciende el nodo destino si este todavia puede encenderse.

## Flujo De Propagacion

La secuencia actual es:

```text
1. FireSimulationManager construye el grafo runtime.
2. El nodo inicial se marca como Burning.
3. El manager detecta nodos Burning.
4. Por cada nodo Burning, intenta activar sus aristas conectadas.
5. FireEdge inicia la propagacion lineal desde ese nodo.
6. FireEdge mueve el frente de fuego entre source y target.
7. FireEdge crea patches de fuego a lo largo del tramo recorrido.
8. Cuando progress llega a 1.0, FireEdge enciende el nodo destino.
9. El nodo destino activa sus propias aristas conectadas.
```

La propagacion visual vive principalmente en `FireEdge`, no en `FireObject`.

## Propagacion Visual

La arista usa dos capas visuales:

```text
Front Fire Effect
Ground Fire Patches
```

El frente de fuego se mueve:

```text
position = Lerp(activeSource.position, activeTarget.position, progress)
```

Los patches se crean progresivamente segun la distancia recorrida:

```text
burnedDistance = totalDistance * progress
while nextPatchDistance <= burnedDistance:
    create patch
    nextPatchDistance += firePatchSpacing
```

Esto permite que el fuego no solo se mueva, sino que vaya dejando una zona encendida detras del frente.

## Como Crear Un Grafo Manualmente

1. Arrastrar `FireGraphRoot.prefab` a la escena.
2. Crear o arrastrar nodos dentro de `FireGraphRoot/Nodes`.
3. Usar `FireNode_Spark` para el inicio del incendio.
4. Usar `FireNode_Sensitive` para nodos combustibles normales.
5. Crear aristas dentro de `FireGraphRoot/Edges` usando `FireEdge_Ground_BigSimple`.
6. En cada arista, asignar:

```text
source: nodo A
target: nodo B
```

7. En `FireSimulationManager`, asignar:

```text
startingNode: FireNode_Spark
igniteStartingNodeOnStart: true
```

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

Esta escena contiene un grafo basico con dos rutas de propagacion. Sirve para probar el algoritmo antes de llevarlo a la escena del tutorial.

## Herramientas De Editor

### FireGraphPrefabFactory

Ruta:

```text
Assets/Editor/FireGraphPrefabFactory.cs
```

Menu:

```text
Tools/Fire Simulation/Create Graph Architecture Prefabs
```

Regenera:

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

Sirve para validar nodos, aristas explicitas y posibles IDs duplicados.

## Codigo Obsoleto Eliminado

La arquitectura actual ya no usa conexiones declaradas dentro del nodo. Las conexiones reales deben estar representadas por objetos `FireEdge` dentro de `FireGraphRoot/Edges`.

## Respaldo Del Proyecto

No se debe guardar copias de seguridad con scripts `.cs` dentro de `Assets`, porque Unity compila todos los scripts dentro de esa carpeta y puede generar errores por clases duplicadas.

El respaldo del avance debe hacerse con Git y GitHub. Si se necesita crear una copia temporal local, debe quedar fuera de `Assets` y eliminarse antes de volver a compilar el proyecto.

Incluye scripts, prefabs del grafo y la escena `FirePropagationTest.unity`.

## Proximas Mejoras

La siguiente mejora recomendada es ajustar dinamicamente cada patch de fuego.

Parametros posibles:

```text
minPatchScale
maxPatchScale
randomRotation
lateralJitter
patchLifetimeRange
scaleByProgress
```

Despues de eso, el siguiente paso tecnico seria adaptar la colocacion de patches a terrenos no planos mediante raycast contra el suelo.

## Conclusion

La arquitectura actual separa correctamente la logica del nodo y la propagacion visible:

```text
FireObject = estado del punto alcanzado.
FireEdge = recorrido visible del fuego.
FireSimulationManager = activacion del grafo runtime.
FireGraphRoot = organizacion de la jerarquia.
```

Esta base permite construir el tutorial con rutas claras de propagacion y deja espacio para extender el sistema hacia vegetacion, objetos reales, paredes, terreno irregular y mecanicas de extincion.
