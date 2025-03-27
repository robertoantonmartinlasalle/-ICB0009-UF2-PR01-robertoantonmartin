using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    // Semáforo que controla el número máximo de pacientes en consulta simultáneamente (4 médicos)
    static SemaphoreSlim semaforoMedicos = new SemaphoreSlim(4);

    // Semáforo para las máquinas de diagnóstico (solo 2 disponibles)
    static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2);

    // Objetos para sincronización de consola y estructura compartida
    static object locker = new object();              // Protege consola y lista global
    static object diagnosticoLock = new object();     // Protege la cola de pacientes en diagnóstico

    // Estructuras compartidas
    static List<Paciente> colaDiagnostico = new List<Paciente>();      // Cola concurrente por prioridad
    static List<Paciente> todosPacientes = new List<Paciente>();       // Todos los pacientes generados

    // Control de llegada y tiempo global
    static DateTime inicioSimulacion;
    static int contadorPacientes = 0;

    static async Task Main()
    {
        inicioSimulacion = DateTime.Now;

        // Iniciamos la generación asincrónica de pacientes
        await GenerarPacientes();

        Console.WriteLine("\n--- TODOS LOS PACIENTES HAN SIDO ATENDIDOS ---\n");
    }

    // Generador asincrónico de pacientes
    static async Task GenerarPacientes()
    {
        Random rand = new Random();
        List<Task> tareas = new List<Task>();

        int N = 1000; // Total de pacientes en esta simulación

        for (int i = 0; i < N; i++)
        {
            // Asignamos datos aleatorios
            int id = rand.Next(1, 101); // ¡OJO! Puede repetirse (no es identificador único)
            int prioridad = rand.Next(1, 4);
            int tiempoConsulta = rand.Next(5, 16);
            bool requiereDiagnostico = rand.Next(0, 2) == 0;

            // Creamos paciente con orden único de llegada
            Paciente p = new Paciente
            {
                Id = id,
                Prioridad = prioridad,
                TiempoConsulta = tiempoConsulta,
                RequiereDiagnostico = requiereDiagnostico,
                OrdenLlegada = Interlocked.Increment(ref contadorPacientes),
                FechaLlegada = DateTime.Now
            };

            // Lo registramos para trazabilidad
            lock (locker)
            {
                todosPacientes.Add(p);
            }

            // Disparamos su ciclo de vida en una tarea independiente
            tareas.Add(Task.Run(() => FlujoPaciente(p)));

            // Esperamos 2 segundos para simular llegada progresiva
            await Task.Delay(2000);
        }

        // Esperamos a que todos finalicen
        await Task.WhenAll(tareas);
    }

    // Lógica completa de vida de un paciente
    static void FlujoPaciente(Paciente p)
    {
        // 1. Registro de llegada
        lock (locker)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id} (Prioridad {p.Prioridad}, Llegada #{p.OrdenLlegada}) ha llegado. Estado: {p.ObtenerEstado()}");
        }

        // 2. Espera consulta (semaforo)
        semaforoMedicos.Wait();
        p.Estado = 1;
        p.FechaInicioConsulta = DateTime.Now;

        // Mensaje de inicio de consulta
        lock (locker)
        {
            double espera = (p.FechaInicioConsulta - p.FechaLlegada).TotalSeconds;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id} (Llegada #{p.OrdenLlegada}). Estado: {p.ObtenerEstado()}. Espera: {espera:F2}s");
        }

        // 3. Simulamos duración de la consulta
        Thread.Sleep(p.TiempoConsulta * 1000);
        p.FechaFinConsulta = DateTime.Now;
        semaforoMedicos.Release();

        // 4. Diagnóstico (si aplica)
        if (p.RequiereDiagnostico)
        {
            p.Estado = 2;

            // Mensaje de espera para diagnóstico
            lock (locker)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id} (Llegada #{p.OrdenLlegada}). Estado: {p.ObtenerEstado()} (esperando máquina y turno)");
            }

            // Añadimos a la cola de diagnóstico
            lock (diagnosticoLock)
            {
                colaDiagnostico.Add(p);
            }

            // Espera activa hasta que le toque su turno (por prioridad y orden de llegada)
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
                    Thread.Sleep(200); // Controlamos el uso de CPU
            }

            // Espera a máquina libre
            maquinasDiagnostico.Wait();
            p.FechaInicioDiagnostico = DateTime.Now;

            // Informa del inicio del diagnóstico
            lock (locker)
            {
                double esperaDiag = (p.FechaInicioDiagnostico - p.FechaFinConsulta).TotalSeconds;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id} (Llegada #{p.OrdenLlegada}). Diagnóstico iniciado. EsperaDiagnóstico: {esperaDiag:F2}s");
            }

            // Simulamos el diagnóstico
            Thread.Sleep(15000);
            p.FechaFinDiagnostico = DateTime.Now;
            maquinasDiagnostico.Release();
        }

        // 5. Mensaje final solo tras finalizar todo el proceso
        lock (locker)
        {
            p.Estado = 3;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id} (Llegada #{p.OrdenLlegada}). Estado: {p.ObtenerEstado()}.");
        }
    }
}
