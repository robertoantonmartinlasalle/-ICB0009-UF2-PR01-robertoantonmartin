using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

class Program
{
    // Semáforo para médicos (máximo 4 pacientes a la vez en consulta)
    static SemaphoreSlim semaforoMedicos = new SemaphoreSlim(4);

    // Semáforo para máquinas de diagnóstico (máximo 2 a la vez)
    static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2);

    // Lock para proteger acceso a consola y lista general de pacientes
    static object locker = new object();

    // Lock exclusivo para la cola de diagnóstico (por prioridad)
    static object diagnosticoLock = new object();

    // Cola de pacientes que esperan diagnóstico
    static List<Paciente> colaDiagnostico = new List<Paciente>();

    // Lista de todos los pacientes creados (para estadísticas)
    static List<Paciente> todosPacientes = new List<Paciente>();

    // Para calcular estadísticas sobre el uso de máquinas
    static DateTime inicioSimulacion;

    static void Main()
    {
        List<Thread> hilos = new List<Thread>();
        Random rand = new Random();
        inicioSimulacion = DateTime.Now;

        // Generamos 20 pacientes que llegan cada 2 segundos
        for (int i = 1; i <= 20; i++)
        {
            int id = rand.Next(1, 101); // ID entre 1 y 100
            int prioridad = rand.Next(1, 4); // 1 = emergencia, 2 = urgencia, 3 = general
            int tiempoConsulta = rand.Next(5, 16); // duración de la consulta
            bool requiereDiagnostico = rand.Next(0, 2) == 0; // 50% requiere diagnóstico

            // Crear instancia del paciente
            Paciente p = new Paciente
            {
                Id = id,
                Prioridad = prioridad,
                TiempoConsulta = tiempoConsulta,
                RequiereDiagnostico = requiereDiagnostico,
                OrdenLlegada = i,
                FechaLlegada = DateTime.Now
            };

            // Guardamos el paciente para estadísticas futuras
            lock (locker) todosPacientes.Add(p);

            // Creamos el hilo individual del paciente
            Thread hilo = new Thread(() => FlujoPaciente(p));
            hilos.Add(hilo);
            hilo.Start();

            // Espera de llegada entre pacientes
            Thread.Sleep(2000);
        }

        // Esperamos que todos los hilos finalicen
        foreach (var hilo in hilos)
            hilo.Join();

        Console.WriteLine("\n--- TODOS LOS PACIENTES HAN SIDO ATENDIDOS ---\n");

        // Calculamos estadísticas finales
        MostrarEstadisticas();
    }

    // Lógica de vida de cada paciente
    static void FlujoPaciente(Paciente p)
    {
        lock (locker)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id} (Prioridad {p.Prioridad}, Llegada #{p.OrdenLlegada}) ha llegado. Estado: {p.ObtenerEstado()}");
        }

        // Espera a médico disponible
        semaforoMedicos.Wait();
        p.Estado = 1;
        p.FechaInicioConsulta = DateTime.Now;

        lock (locker)
        {
            double espera = (p.FechaInicioConsulta - p.FechaLlegada).TotalSeconds;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()}. Espera: {espera:F2}s");
        }

        // Simulación de tiempo de consulta
        Thread.Sleep(p.TiempoConsulta * 1000);
        p.FechaFinConsulta = DateTime.Now;

        // Libera médico
        semaforoMedicos.Release();

        // Si el paciente necesita diagnóstico, entra en cola
        if (p.RequiereDiagnostico)
        {
            p.Estado = 2;

            lock (locker)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()} (esperando máquina y turno)");
            }

            // Añadir a cola compartida para diagnóstico
            lock (diagnosticoLock)
            {
                colaDiagnostico.Add(p);
            }

            // Esperar a que sea su turno: menor prioridad y menor orden de llegada
            bool turnoEsperado = false;
            while (!turnoEsperado)
            {
                lock (diagnosticoLock)
                {
                    var siguiente = colaDiagnostico
                        .OrderBy(pac => pac.Prioridad)
                        .ThenBy(pac => pac.OrdenLlegada)
                        .FirstOrDefault();

                    if (siguiente != null && siguiente == p)
                    {
                        colaDiagnostico.Remove(p);
                        turnoEsperado = true;
                    }
                }

                if (!turnoEsperado)
                    Thread.Sleep(200);
            }

            // Esperar máquina libre
            maquinasDiagnostico.Wait();
            p.FechaInicioDiagnostico = DateTime.Now;

            lock (locker)
            {
                double esperaDiag = (p.FechaInicioDiagnostico - p.FechaFinConsulta).TotalSeconds;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Diagnóstico iniciado. EsperaDiagnóstico: {esperaDiag:F2}s");
            }

            // Simulación de prueba diagnóstica
            Thread.Sleep(15000);
            p.FechaFinDiagnostico = DateTime.Now;

            // Libera la máquina
            maquinasDiagnostico.Release();
        }

        // Finaliza la atención
        p.Estado = 3;
        lock (locker)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()}.");
        }
    }

    // Cálculo de estadísticas finales al terminar la simulación
    static void MostrarEstadisticas()
    {
        Console.WriteLine("--- FIN DEL DÍA ---\n");

        // Clasificamos pacientes por prioridad
        var emergencias = todosPacientes.Where(p => p.Prioridad == 1).ToList();
        var urgencias = todosPacientes.Where(p => p.Prioridad == 2).ToList();
        var generales = todosPacientes.Where(p => p.Prioridad == 3).ToList();

        // Totales por prioridad
        Console.WriteLine("Pacientes atendidos:");
        Console.WriteLine($"- Emergencias: {emergencias.Count}");
        Console.WriteLine($"- Urgencias: {urgencias.Count}");
        Console.WriteLine($"- Consultas generales: {generales.Count}");

        // Función para calcular la media de espera desde llegada a inicio de consulta
        double MediaEspera(List<Paciente> pacientes) =>
            pacientes.Any() ? pacientes.Average(p => (p.FechaInicioConsulta - p.FechaLlegada).TotalSeconds) : 0;

        Console.WriteLine("\nTiempo promedio de espera:");
        Console.WriteLine($"- Emergencias: {MediaEspera(emergencias):F2}s");
        Console.WriteLine($"- Urgencias: {MediaEspera(urgencias):F2}s");
        Console.WriteLine($"- Consultas generales: {MediaEspera(generales):F2}s");

        // Uso promedio de las máquinas de diagnóstico
        var conDiagnostico = todosPacientes.Where(p => p.FechaFinDiagnostico != default).ToList();
        double totalUso = conDiagnostico.Sum(p => (p.FechaFinDiagnostico - p.FechaInicioDiagnostico).TotalSeconds);
        double tiempoTotal = (DateTime.Now - inicioSimulacion).TotalSeconds;
        double usoMaquinas = (totalUso / (tiempoTotal * 2)) * 100; // 2 máquinas disponibles

        Console.WriteLine($"\nUso promedio de máquinas de diagnóstico: {usoMaquinas:F2}%");
    }
}
