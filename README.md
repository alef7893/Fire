# VR Fire Control

Proyecto Unity 3D/VR orientado a entrenamiento de control de incendios. El usuario interactua en realidad virtual con un extintor, inicia misiones, apaga focos de fuego y evita que la propagacion alcance vegetacion protegida.

## 1. Informacion tecnica

| Elemento | Valor |
| --- | --- |
| Motor | Unity `2022.3.62f3` |
| Plataforma objetivo | Android / Meta Quest |
| Nombre del producto | `VRFireControl` |
| Package name Android | `com.AJACompany.VRFireControl` |
| SDK minimo Android | API 32 |
| SDK objetivo Android | API 34 |
| Backend de scripting Android | IL2CPP |
| Arquitectura Android | ARM64 |
| XR principal | Meta XR SDK `201.0.0` |

## 2. Requisitos

### Software

- Unity Hub.
- Unity Editor `2022.3.62f3`.
- Modulos de Unity para Android:
  - Android Build Support.
  - Android SDK & NDK Tools.
  - OpenJDK.
- Git.
- Git LFS.
- Meta XR Simulator, recomendado para pruebas sin visor.
- Opcional para despliegue fisico: Meta Quest Developer Hub o `adb`.

### Hardware recomendado

- PC con Windows.
- Meta Quest 2 o superior para pruebas reales.
- Cable USB-C o conexion de despliegue configurada mediante herramientas Meta.

## 3. Clonar e instalar el proyecto

1. Clonar el repositorio:

   ```bash
   git clone https://github.com/alef7893/Fire.git
   cd Fire
   ```

2. Descargar archivos administrados por Git LFS:

   ```bash
   git lfs install
   git lfs pull
   ```

3. Abrir Unity Hub.

4. Seleccionar `Add project from disk`.

5. Abrir la carpeta clonada del proyecto.

6. Verificar que Unity use la version `2022.3.62f3`.

7. Abrir el proyecto y esperar a que Unity importe los assets y regenere `Library`.

## 4. Estructura principal del proyecto

```text
Assets/
├── Documentation/          Documentacion interna del proyecto.
├── ImportedAssetPacks/     Paquetes de assets externos organizados.
├── Materials/              Materiales propios del proyecto.
├── Prefabs/
│   ├── Equipment/          Extintor y elementos reutilizables.
│   ├── FireGraph/          Prefabs del grafo de propagacion de fuego.
│   ├── Player/             Rig VR reutilizable.
│   └── UI/                 Interfaces 3D VR.
├── Scenes/                 Escenas activas del juego.
└── Scripts/
    ├── FireGraph/          Logica del grafo de propagacion.
    ├── Gameplay/           Extintor, misiones y mundo.
    ├── Infrastructure/     Carga de escenas, fade y saneamiento XR.
    ├── Player/             Control y posicionamiento del jugador.
    └── UI/                 Logica de paneles de menu y mision.
```

## 5. Escenas del juego

Las escenas activas estan registradas en `File > Build Settings...` en este orden:

1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/M00_Training.unity`
3. `Assets/Scenes/M01_BasicSuppression.unity`
4. `Assets/Scenes/M02_ForestContainment.unity`

### Flujo esperado

- `MainMenu`: menu principal. Permite iniciar las misiones o salir.
- `M00_Training`: entrenamiento basico con extintor y focos de fuego independientes.
- `M01_BasicSuppression`: mision basica con propagacion y objetivo de supresion.
- `M02_ForestContainment`: mision principal de contencion forestal.

## 6. Configuracion de Unity

### Plataforma Android

1. Abrir `File > Build Settings...`.
2. Seleccionar `Android`.
3. Presionar `Switch Platform` si no esta activo.
4. Verificar que las escenas listadas en la seccion anterior esten agregadas y habilitadas.

### Player Settings

Abrir `Edit > Project Settings > Player` y revisar:

- `Company Name`: `AJACompany`.
- `Product Name`: `VRFireControl`.
- `Package Name`: `com.AJACompany.VRFireControl`.
- `Minimum API Level`: Android API 32.
- `Target API Level`: Android API 34.
- `Scripting Backend`: IL2CPP.
- `Target Architectures`: ARM64.

### XR / Meta

1. Abrir `Edit > Project Settings > XR Plug-in Management`.
2. En Android, verificar que Oculus/Meta XR este habilitado.
3. Abrir `Meta > Tools > Project Setup Tool`.
4. Aplicar las correcciones recomendadas si Unity las reporta.

## 7. Ejecutar en el editor con XR Simulator

1. Abrir una escena, por ejemplo `MainMenu`.
2. Activar Meta XR Simulator desde:

   ```text
   Meta > Meta XR Simulator > Activate
   ```

3. Verificar el estado desde:

   ```text
   Meta > Meta XR Simulator > Status
   ```

   Debe mostrar:

   - `Installed: Yes`
   - `Active: Yes`

4. Presionar `Play`.

5. Si aparece la ventana de Meta XR Simulator, usar los controles del simulador para mover la cabeza/manos y seleccionar botones.

### Problemas comunes del XR Simulator

- Si el simulador no abre, revisar que no existan versiones duplicadas o rotas del paquete Meta XR Simulator.
- Si solo se ve la ventana de Game y no el panel externo del simulador, desactivar y volver a activar desde `Meta > Meta XR Simulator`.
- Si Unity queda inestable, cerrar Unity, eliminar procesos pendientes desde el administrador de tareas y abrir nuevamente.

## 8. Generar APK

1. Cerrar el modo Play si esta activo.
2. Ir a `File > Build Settings...`.
3. Seleccionar `Android`.
4. Confirmar que la primera escena sea:

   ```text
   Assets/Scenes/MainMenu.unity
   ```

5. Presionar `Build`.
6. Seleccionar una ruta de salida, por ejemplo:

   ```text
   Builds/VRFireControl.apk
   ```

7. Esperar a que termine la compilacion IL2CPP y el empaquetado Android.

El repositorio puede contener una carpeta local `Builds/`, pero esta carpeta esta ignorada por Git. Los APK generados no deben subirse al repositorio salvo que se indique explicitamente.

## 9. Desplegar en Meta Quest

### Opcion A: desde Unity

1. Conectar el visor Meta Quest por USB.
2. Aceptar permisos de depuracion USB dentro del visor.
3. En Unity, abrir `File > Build Settings...`.
4. Seleccionar `Android`.
5. Seleccionar el dispositivo en `Run Device`.
6. Presionar `Build And Run`.

### Opcion B: usando adb

1. Generar el APK.
2. Conectar el visor por USB.
3. Verificar que el dispositivo sea detectado:

   ```bash
   adb devices
   ```

4. Instalar el APK:

   ```bash
   adb install -r Builds/VRFireControl.apk
   ```

5. Ejecutar la aplicacion desde el visor.

## 10. Verificacion posterior al despliegue

Despues de instalar el APK, validar:

- La aplicacion inicia en `MainMenu`.
- Los botones VR del menu responden con las manos/controladores.
- La transicion entre escenas muestra fade negro.
- El extintor puede agarrarse y disparar.
- El agua reduce y apaga el fuego.
- La mision 0 permite cancelar/reiniciar entrenamiento.
- Las misiones 1 y 2 cargan sin congelar el movimiento del jugador.
- Las condiciones de victoria/derrota muestran el panel correspondiente en el menu.

## 11. Mantenimiento del repositorio

### Archivos que no deben versionarse

El proyecto ignora carpetas generadas por Unity:

- `Library/`
- `Temp/`
- `Obj/`
- `Build/`
- `Builds/`
- `Logs/`
- `UserSettings/`

Tambien se ignoran archivos generados como `.csproj`, `.sln`, APK/AAB y paquetes `.unitypackage`.

### Uso de Git LFS

El proyecto usa Git LFS para assets pesados, como:

- Modelos 3D.
- Texturas.
- Audio.
- Videos.
- APK si alguna vez se decide versionarlo.

Antes de clonar o actualizar el proyecto, asegurarse de tener Git LFS instalado y ejecutar:

```bash
git lfs pull
```

## 12. Resolucion de problemas

### Android SDK not found

Instalar Android Build Support desde Unity Hub para la version `2022.3.62f3`. Verificar en:

```text
Edit > Preferences > External Tools
```

Unity debe apuntar al SDK, NDK y JDK instalados con Unity.

### Scene could not be loaded

La escena no esta en Build Settings. Abrir:

```text
File > Build Settings...
```

Agregar la escena faltante y verificar que este habilitada.

### Botones VR no responden

Revisar:

- Que exista un solo `EventSystem`.
- Que exista un solo `PointableCanvasModule`.
- Que el rig VR tenga interacciones activas.
- Que el panel use componentes compatibles con Meta Interaction SDK.

### El jugador aparece fuera del area esperada

Revisar el objeto `PlayerRoot` y los scripts de posicionamiento:

- `XRPlayerSpawnController`
- `XRTrackedAreaBoundary`

El punto de spawn debe coincidir con la zona inicial definida para cada escena.

### Pantalla negra o cielo negro

Verificar que la escena tenga skybox asignado en:

```text
Window > Rendering > Lighting > Environment
```

El proyecto utiliza skyboxes importados desde `Stylized Skyboxes FREE`.

## 13. Estado actual del proyecto

El proyecto se encuentra en una version funcional con:

- Menu principal VR.
- Tres misiones principales.
- Interfaz 3D VR estandarizada.
- Transiciones con fade.
- Extintor VR interactuable.
- Grafo de propagacion de fuego.
- Apagado de fuego con agua.
- Condiciones de victoria y derrota.

