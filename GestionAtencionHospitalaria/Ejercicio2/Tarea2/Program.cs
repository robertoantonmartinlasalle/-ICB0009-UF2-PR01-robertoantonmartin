using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static SemaphoreSlim semaforoMedicos = new SemaphoreSlim(4); // 4 médicos disponibles
    static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2); // 2 máquinas de diagnóstico
    static object locker = new object(); // para sincronización de consola
    static object turnoDiagnosticoLock = new object(); // para proteger acceso a la cola

    static List<Paciente> colaDiagnostico = new List<Paciente>(); // Cola de pacientes que esperan diagnóstico

    static void Main()
    {
        List<Thread> hilos = new List<Thread>();
        Random rand = new Random();

        for (int i = 1; i <= 4; i++)
        {
            int id = rand.Next(1, 101);
            int tiempoConsulta = rand.Next(5, 16);
            bool requiereDiagnostico = rand.Next(0, 2) == 0;

            Paciente p = new Paciente(id, i * 2, tiempoConsulta, i);
            p.RequiereDiagnostico = requiereDiagnostico;

            Thread hilo = new Thread(() => FlujoPaciente(p));
            hilos.Add(hilo);
            hilo.Start();

            Thread.Sleep(2000); // Simulación: llegada cada 2 segundos
        }

        foreach (var hilo in hilos)
            hilo.Join();

        Console.WriteLine("\n--- TODOS LOS PACIENTES HAN SIDO ATENDIDOS ---");
    }

    static void FlujoPaciente(Paciente p)
    {
        lock (locker)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id} (Llegada #{p.OrdenLlegada}) ha llegado. Estado: {p.ObtenerEstado()}");
        }

        semaforoMedicos.Wait();
        p.Estado = 1;
        p.FechaInicioConsulta = DateTime.Now;

        lock (locker)
        {
            double espera = (p.FechaInicioConsulta - p.FechaLlegadaReal).TotalSeconds;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()}. Espera: {espera:F2}s");
        }

        Thread.Sleep(p.TiempoConsulta * 1000); // Consulta médica

        p.FechaFinConsulta = DateTime.Now;
        semaforoMedicos.Release();

        if (p.RequiereDiagnostico)
        {
            p.Estado = 2;

            lock (locker)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()} (esperando máquina y turno)");
            }

            // Añadir a la cola sin ordenar
            lock (turnoDiagnosticoLock)
            {
                colaDiagnostico.Add(p);
            }

            bool turnoEsperado = false;
            while (!turnoEsperado)
            {
                lock (turnoDiagnosticoLock)
                {
                    // Obtener el paciente con menor OrdenLlegada en la cola actual
                    int? ordenMinimo = colaDiagnostico.Count > 0
                        ? colaDiagnostico.Min(pac => pac.OrdenLlegada)
                        : (int?)null;

                    if (ordenMinimo.HasValue && ordenMinimo == p.OrdenLlegada)
                    {
                        turnoEsperado = true;
                        colaDiagnostico.Remove(p);
                    }
                }

                if (!turnoEsperado)
                    Thread.Sleep(200); // Espera ligera
            }

            maquinasDiagnostico.Wait();
            p.FechaInicioDiagnostico = DateTime.Now;

            lock (locker)
            {
                double esperaDiag = (p.FechaInicioDiagnostico - p.FechaFinConsulta).TotalSeconds;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Diagnóstico iniciado. EsperaDiagnóstico: {esperaDiag:F2}s");
            }

            Thread.Sleep(15000); // Diagnóstico
            p.FechaFinDiagnostico = DateTime.Now;
            maquinasDiagnostico.Release();
        }

        p.Estado = 3;
        lock (locker)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()}.");
        }
    }
}
