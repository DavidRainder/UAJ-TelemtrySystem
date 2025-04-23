# TelemtrySystem Source
En este repositorio se puede encontrar el proyecto original del que se exporta el paquete TelemetrySystem. 

Este repositorio ha sido creado para la entrega de la 3ª práctica de la asignatura Usabilidad y Análisis de Juegos, impartida en la Facultad de Informática de la Universidad Complutense de Madrid.

Autores:
- Eva Feliu Aréjola
- David Rivera Martínez
- Muxu Rubia Luque
- Andrea Vega Saugar
- Claudia Zarzuela Amor

## Instalación
Para instalar el paquete de TelemetrySystem en un proyecto de Unity, se debe importar el paquete que se encuentra en la raíz del repositorio: `TelemtrySystem.unitypackage`. 

En la pestaña `Assets --> Import Package --> Custom Package` de Unity debes buscar el paquete y seleccionarlo. Se abrirá una ventana con los contenidos de una carpeta `Telemetry` que contiene todo lo necesario para que el sistema de telemetría funcione.

## Ejemplo de uso

### Ver y usar el sistema de telemetría en una escena de juego
1. Asegurate de que en la escena inicial está el prefab o GameObject con el Tracker y su componente EventRegistry (Prefab `Telemetry`). 
  
  ⚠️ `Sin esto, no se registrará ningún evento`

3. Desde el inspector de Unity, acceder al componente EventRegistry y marcar los eventos que se quieren trackear en esa build

  ⚠️ `Si el evento no está activado, no se registrará aunque se envíe al tracker`

5. Ejecuta la escena. Los eventos configurados se capturarán automáticamente y se almacenarán en la cola del Tracker. A los X segundos se vuelcan a disco en JSON o XML, en Application.persistentDataPath o donde el usuario haya escogido o se envían a un servidor web. Por defecto se guardan en local en formato JSON, pero se puede cambiar en las pestañas desplegables de los campos serializados.

### Cómo añadir un nuevo evento al sistema
1. Crea una clase nueva que herede de Event o PersistentEvent

```C#
public class ItemCollectedEvent :  TelemetrySystem.Event{

public string itemName;

public ItemCollectedEvent (string name){
itemName = name;
}
public override string GetID() => “ItemCollected”;
public override string ToJSON()
{
    return base.ToJSON() + $", \"item_name\": \"{itemName}\"";
}
public override void ToXML(XmlDocument doc, XmlNode eventsNode, out XmlNode myEvent)
{
    base.ToXML(doc, eventsNode, out myEvent);

    XmlAttribute _itemName= doc.CreateAttribute("item_name");
    _itemName.Value = itemName;
    myEvent.Attributes.Append(_itemName);
}
```

Si es persistente,
```C#
public override void GetDataCallback()
{
   // Actualizar los atributos según las referencias recibidas en el constructor
}
```

2. Añadir su identificador a EventRegistry desde inspector. 
  `Para que el Tracker lo acepte, su GetID() debe coincidir con el nombre dado en el editor`

3. Instrumentalizar el juego para generar el evento
```C#
ItemCollectedEvent e = new ItemCollectedEvent(“MaginGem”);
Tracker.Instance.PushEvent(e);
```

Si es persistente,
```C#
ItemCollectedEvent p = new ItemCollectedEvent(“MaginGem”, 500);
Tracker.Instance.TrackPersistentEvent(p);
```

‼️⚠️ Un evento persistente posiblemente guarde referencias a un objeto que se borre al acabar una escena. Al borrarse, las referencias se pierden y los errores surgen. Hay que parar el evento cuando no sea necesario o posible seguir trackeándolo.
```C#
Tracker.Instance.StopTrackingPersistentEvent(“ID_de_tu_evento”);
```

4. Ejecutar la escena y revisar los ficheros de traza. Se generarán en Application.persistentDataPath o donde el usuario haya escogido con nombre de sesión incremental (X_Telemetry-dd-mm-yy; donde X es el número de sesión del día, comenzando en 0 e incrementándose en 1 por cada archivo generado).
