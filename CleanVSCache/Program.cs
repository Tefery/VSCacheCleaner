﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace LimpiarCacheVisualStudio
{
    class Program
    {

        static bool saltarConfirmaciones = false;
        static bool ignoreOutput = true;

        static void Main(string[] args)
        {
            // Verificar si se ejecuta como administrador
            if (!EsAdministrador())
            {
                print("Se requieren permisos de administrador. Solicitando elevación...");
                ReiniciarComoAdministrador(args);
                return;
            }

            if (args.Length > 0)
            {
                saltarConfirmaciones = args.Any(x => string.Equals(x, "-y", StringComparison.InvariantCultureIgnoreCase));
                ignoreOutput = args.Any(x => string.Equals(x, "-io", StringComparison.InvariantCultureIgnoreCase));
            }

            string carpetaAppDataVS = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\VisualStudio");

            // Rutas de los directorios a limpiar
            List<string> rutas = [Path.Combine(carpetaAppDataVS, @"Roslyn")];
            
            const string componentModelCache = "ComponentModelCache";
            const string patronCarpetasVersiones = @"^(\d)+(\.)(.)+$";
            Regex regex = new Regex(patronCarpetasVersiones);

            string[] subcarpetas = Directory.GetDirectories(carpetaAppDataVS);

            print("\nCarpetas que coinciden con el patrón:");
            foreach (string subcarpeta in subcarpetas)
            {
                string nombreCarpeta = Path.GetFileName(subcarpeta);
                if (regex.IsMatch(nombreCarpeta))
                {
                    rutas.Add(Path.Combine(carpetaAppDataVS,nombreCarpeta,componentModelCache));
                }
            }

            string tempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Temp");
            DirectoryInfo di = new(tempPath);

            foreach (FileInfo file in di.EnumerateFiles())
            {
                try
                {
                    file.Delete();
                    print($"Eliminado: {file.FullName}");
                }
                catch (Exception ex)
                {
                    print($"Error al eliminar {file.FullName}: {ex.Message}");
                }
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                try
                {
                    dir.Delete(true);
                    print($"Eliminado: {dir.FullName}");
                }
                catch (Exception ex)
                {
                    print($"Error al eliminar {dir.FullName}: {ex.Message}");
                }
            }

            foreach (string ruta in rutas)
            {
                if (Directory.Exists(ruta))
                {
                    try
                    {
                        Directory.Delete(ruta, true);
                        print($"Eliminado: {ruta}");
                    }
                    catch (Exception ex)
                    {
                        print($"Error al eliminar {ruta}: {ex.Message}");
                    }
                }
                else
                {
                    print($"Directorio no encontrado: {ruta}");
                }
            }

            if (!saltarConfirmaciones)
            {
                Console.WriteLine("Limpieza completada. Presiona cualquier tecla para salir.");
                Console.ReadKey();
            }
        }

        static void print(string msg)
        {
            if (!ignoreOutput)
                Console.WriteLine(msg);
        }

        static bool EsAdministrador()
        {
            using (WindowsIdentity identidad = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identidad);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        static void ReiniciarComoAdministrador(string[] args)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = string.Join(" ", args.Select(a => $"\"{a}\""))
            };

            try
            {
                Process.Start(info);
            }
            catch (Exception)
            {
                Console.WriteLine("No se pudo obtener permisos de administrador.");
            }
        }
    }
}
