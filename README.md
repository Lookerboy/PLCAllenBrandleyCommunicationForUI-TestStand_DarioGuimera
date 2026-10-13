# Comunicacion Allen Bradley para UI y TestStand

Dos API .NET 8 mediante libplctag 1.5.1. `main` conserva la version original; la correccion de controles manuales esta en `fix/allen-bradley-manual-buttons`.

## Correcciones de la API UI

- Los setters terminan tras una escritura correcta; los setters por indice esperan la escritura y propagan sus errores.
- Las operaciones BOOL se serializan por simbolo, incluidos aliases por mayusculas/minusculas: lectura, modificacion del buffer y escritura no se solapan con polling u otra accion del mismo cliente.
- Una escritura BOOL se espera una vez, sin reintento automatico. Los errores incluyen operacion, simbolo y excepcion interna. Se conservan los seis intentos acotados para lecturas.
- Un setter BOOL por indice modifica ese bit dentro de la misma operacion; evita reenviar una copia antigua del array obtenida por otra llamada.
- `GetVariableArrayBool` devuelve la longitud logica registrada, no la capacidad empaquetada. Indices y longitudes se validan. BOOL escalar sigue usando un byte y arrays de mas de un elemento conservan palabras de 32 bits.
- Registros BOOL compatibles comparten tag; longitudes incompatibles fallan. El cierre espera operaciones BOOL activas, rechaza las nuevas y es idempotente.

Las firmas publicas de `API_UI`, sus codigos de tipo y su constructor se mantienen compatibles con la DLL que consumia el runtime. No se cambia el fuente de `API_TESTSTAND`: se compila y comprueba su fachada sincrona contra el simulador desde .NET. No equivale a ejecutar pasos en NI TestStand.

El runtime necesita tambien la correccion de eventos/orden de acciones de su rama `fix/allen-bradley-manual-buttons`. Compilar esta biblioteca no actualiza su copia `View/PLC_COMM_API.dll`; sincronizarla y comprobar su hash antes de construir el consumidor.

## Pruebas

Desde la carpeta comun del workspace:

```powershell
./scripts/Verify-ManualPLC.ps1 -UseLocalControlarBinaries -SyncRuntimeDll
```

La suite usa la DLL productora compilada con un transporte BOOL sustituido, las fuentes reales del controlador de acciones y el boton Controlar real en formularios ocultos. Incluye errores, latencia controlada, polling concurrente, 1001 toggles, 600 ciclos TAP, 54 combinaciones de entrada/liberacion, orden de apagado explicito y cierre. La prueba de la version original enlaza su fuente de `1491fe5` a un transporte falso y reproduce escrituras repetidas, finalizacion prematura y pulsacion tardia que deja TRUE.

Las pruebas requieren el checkout hermano `UIDinamicaControlar_DarioGuimera` compatible y la DLL original ControlarWinFormsLibrary del workspace. No son pruebas de un clon aislado ni conectan a equipos.

`--local-eip` es un modo separado, fijado a `127.0.0.1`, para un simulador EtherNet/IP preparado externamente. Requiere tags `Jog:BOOL[1]`, `Commands:DINT[2]` (almacenamiento de bits empaquetados), `Counter:DINT[1]`, `Values:DINT[2]` y `Message:STRING[1]`, ruta `1,0` y puerto TCP 44818. Se comprueban libplctag y su dependencia nativa, no solo el transporte sustituido. El simulador no ejecuta un programa Ladder ni controla un conveyor. Detalle y ajustes del simulador en la documentacion comun `docs/allen-bradley-manual.md`.

## Limites

La serializacion protege las operaciones del mismo cliente. Una escritura de un tag empaquetado sigue enviando su buffer: no ofrece atomicidad frente a escrituras de otro programa o del propio PLC. Para comandos de movimiento usar tags con propiedad clara y un watchdog/interbloqueo en el PLC: si se corta la red o falla el PC, la UI no puede garantizar que llegue la liberacion. El programa PLC y las pruebas fisicas siguen pendientes.

No se ha hecho una refactorizacion general de tipos, arquitectura de TestStand, polling numerico o protocolo. Los cambios se limitan a finalizacion de escrituras de UI, operaciones BOOL y sus consumidores manuales.
