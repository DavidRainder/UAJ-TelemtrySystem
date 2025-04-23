using System;

namespace TelemetrySystem
{
    /// <summary>
    /// Interfaz que se encargar� de serializar los eventos
    /// 
    /// Como podemos serializar nuestros datos en varios formatos
    /// y hacemos una escritura "a mano" de los archivos,
    /// necesitamos implementer los m�todos
    /// - StartingContent(): para definir el contenido inicial de un archivo
    /// - FinalContent(): para definir el contenido inicial de un archivo
    /// 
    /// Esto se debe a que muchos formatos necesitan de un caracter o caracteres
    /// concretos al inicio y/o final de su escritura.
    /// 
    /// Como el Tracker no debe saber qu� contenido como est� escribiendo, se utilizan
    /// estos m�todos gen�ricos que pueden ser o no utilizados.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Contenido inicial del formato utilizado.
        /// No tiene que ver con eventos a tratar, si no con
        /// el propio formato que utiliazar� el serializador
        /// </summary>
        /// <returns> Contenido a escribir </returns>
        public string StartingContent();

        /// <summary>
        /// Contenido del evento en el formato utilizado.
        /// No tiene que ver con eventos a tratar, si no con
        /// el propio formato que utiliazar� el serializador
        /// </summary>
        /// <returns> Contenido a escribir del evento </returns>
        public string Serialize(TrackerEvent e);

        /// <summary>
        /// Contenido final del formato utilizado.
        /// No tiene que ver con eventos a tratar, si no con
        /// el propio formato que utiliazar� el serializador
        /// </summary>
        /// <returns> Contenido a escribir </returns>
        public string FinalContent();

        /// <summary>
        /// Extensi�n del archivo que utiliza el formato del serializador
        /// </summary>
        /// <returns> Extensi�n del archivo que utiliza el formato del serializador </returns>
        public string FileExtension();
    }
}
