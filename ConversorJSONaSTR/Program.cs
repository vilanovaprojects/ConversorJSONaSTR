using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {

        // TÍTULO
        Console.WriteLine("Conversor JSON to STR.  Created by Moisés Campaña");
        Console.WriteLine("");



        // 1. Solicitar la ruta de origen de los archivos JSON
        Console.WriteLine("Por favor, ingrese la ruta de origen de los archivos JSON:");
        string rutaOrigenJSON = Console.ReadLine();

        // Verificar que la ruta de origen exista
        if (string.IsNullOrWhiteSpace(rutaOrigenJSON) || !Directory.Exists(rutaOrigenJSON))
        {
            Console.WriteLine("La ruta de origen no es válida o no existe.");
            return;
        }

        // 2. Crear la ruta de destino como subcarpeta "STR" en la carpeta de origen
        string rutaDestinoSTR = Path.Combine(rutaOrigenJSON, "STR");

        // Crear la carpeta "STR" si no existe
        if (!Directory.Exists(rutaDestinoSTR))
        {
            Directory.CreateDirectory(rutaDestinoSTR);
            Console.WriteLine($"Carpeta de destino creada: {rutaDestinoSTR}");
        }

        // 3. Obtener todos los archivos JSON (.cpy) en la carpeta de origen
        string[] archivosJSON = Directory.GetFiles(rutaOrigenJSON, "*.cpy");

        if (archivosJSON.Length == 0)
        {
            Console.WriteLine("No se encontraron archivos JSON (.cpy) en la ruta de origen.");
            return;
        }

        // 4. Procesar cada archivo JSON
        foreach (var archivoCpy in archivosJSON)
        {
            try
            {
                
                //Info del CPY
                DatosTabla cpy = LeerJsonCpy(archivoCpy);
                int TamRegistro = (int)cpy.LongitudRegistro;

                // Obtener el nombre del archivo .STR
                string nombreArchivo = Path.GetFileNameWithoutExtension(archivoCpy);
                string archivoCBL = Path.Combine(rutaDestinoSTR, nombreArchivo + ".cbl");
                string archivoIDY = Path.Combine(rutaDestinoSTR, nombreArchivo + ".idy");
                string archivoSTR = Path.Combine(rutaDestinoSTR, nombreArchivo + ".str");



                // Usar StreamWriter para crear el archivo y escribir en él
                using (StreamWriter sw = new StreamWriter(archivoCBL))
                {
                    // Escribir la cabecera COBOL
                    sw.WriteLine("       IDENTIFICATION DIVISION.");
                    sw.WriteLine($"       PROGRAM-ID. {cpy.NombreTabla}.");
                    sw.WriteLine("       ENVIRONMENT DIVISION.");
                    sw.WriteLine("       DATA DIVISION.");
                    sw.WriteLine("       WORKING-STORAGE SECTION.");
                    sw.WriteLine("       01 TABLA.");

                    // Iterar sobre los campos y escribir cada uno
                    foreach (var campo in cpy.ListaCampos)
                    {
                        // Si la PosicionFinal del campo es mayor o igual a 5000, se escribe un PIC X(5000-pos.inicial) y se detiene el bucle
                        if (campo.PosicionFinal > 5000)
                        {
                            int ultimo = 5001 - (int) campo.PosicionInicial;
                            sw.WriteLine($"          05 {campo.Nombre} PIC X({ultimo}).");
                            break; // Detener el bucle, no es necesario procesar más campos
                        }

                        string lineaCampo = $"          05 {campo.Nombre} PIC X({campo.Tamano}).";
                        sw.WriteLine(lineaCampo);
                    }

                    // 2. Verificar si el último campo tiene una PosicionFinal menor que 5000
                    var ultimoCampo = cpy.ListaCampos.Last(); // Obtener el último campo de la lista

                    if (ultimoCampo.PosicionFinal < 5000)
                    {
                        // Calcular el tamaño restante
                        int tamanoResto = 5000 - (int) ultimoCampo.PosicionFinal;

                        // Añadir la línea para el campo 'RESTO' con PIC X(tamanoResto)
                        string lineaResto = $"          05 RESTO PIC X({tamanoResto}).";
                        sw.WriteLine(lineaResto);
                    }

                }
                Console.WriteLine("**************************   CBL  **************************");
                Console.WriteLine($"Ruta archivo: {archivoCBL}");


                TransformCBLaIDY(archivoCBL);
                Console.WriteLine("**************************   IDY  **************************");
                Console.WriteLine($"Ruta archivo: {archivoIDY}");


                TransformIDYaSTR(archivoIDY, archivoSTR);
                Console.WriteLine("**************************   STR  **************************");
                Console.WriteLine($"Ruta archivo: {archivoSTR}");


                //Borrado CBL IDY
                Borrado(archivoCBL);
                Borrado(archivoIDY);
                Console.WriteLine("**************************   BORRADO CBL IDY  **************************");

            }
            catch (Exception ex)
            {
                // Manejo de errores si ocurre algún problema durante la lectura o escritura del archivo
                Console.WriteLine($"Error al procesar el archivo {archivoCpy}: {ex.Message}");
            }
        }

        // Mensaje final
        Console.WriteLine("Procesamiento de archivos completado.");
        Console.WriteLine("Presione cualquier tecla para salir...");
        Console.ReadKey();
    }





    private static void Borrado(string archivo)
    {
        try
        {
            // Comprobar si el archivo existe antes de intentar borrarlo
            if (File.Exists(archivo))
            {
                // Borrar el archivo
                File.Delete(archivo);
                Console.WriteLine($"El archivo {archivo} ha sido eliminado.");
            }
            else
            {
                Console.WriteLine($"El archivo {archivo} no existe.");
            }
        }
        catch (Exception ex)
        {
            // Capturar cualquier error que ocurra al intentar borrar el archivo
            Console.WriteLine($"Error al eliminar el archivo: {ex.Message}");
        }
    }

    private static void TransformIDYaSTR(string archivoIDY, string archivoSTR)
    {
        // Retardo
        //Thread.Sleep(300);
        // Crear un nuevo proceso para ejecutar el comando COBOL
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c set PATH=%PATH%;E:\\Program Files (x86)\\Micro Focus\\Enterprise Developer\\bin " +
                        $"&& dfstrcl \"{archivoIDY}\" /d TABLA /o \"{archivoSTR}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Inicia el proceso
        Process process = new Process { StartInfo = startInfo };
        process.Start();

        process.WaitForExit();

        Console.WriteLine($"Convertido: {archivoIDY}");

    }

    private static void TransformCBLaIDY(string archivoCBL)
    {
        // Crear un nuevo proceso para ejecutar el comando COBOL
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c set PATH=%PATH%;E:\\Program Files (x86)\\Micro Focus\\Enterprise Developer\\bin && cobol \"{archivoCBL}\" anim;",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true 
        };

        // Inicia el proceso
        Process process = new Process { StartInfo = startInfo };
        process.Start();

        process.WaitForExit();

        Console.WriteLine($"Convertido: {archivoCBL}");
    }

    public class CampoTabla
    {
        public string Nombre { get; set; }
        //public string Tipo { get; set; }
        public bool Transformar { get; set; }
        public bool Copiar { get; set; }
        public long Tamano { get; set; }
        public long PosicionInicial { get; set; }
        public long PosicionFinal { get; set; }
    }
    public class DatosTabla
    {
        public string NombreTabla { get; set; }
        public long LongitudRegistro { get; set; }
        public List<CampoTabla> ListaCampos { get; set; }
    }
    public static DatosTabla LeerJsonCpy(string archivoCpy)
    {
        string Json = string.Empty;

        using (TextReader readertext = new StreamReader(archivoCpy))
        {
            Json = readertext.ReadToEnd();

        }
        DatosTabla cpy = JsonConvert.DeserializeObject<DatosTabla>(Json);

        return cpy;

    }

}

