using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static SemaphoreSlim semaforoMedicos = new SemaphoreSlim(4);  // 4 médicos
    static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2); // 2 máquinas de diagnóstico
    static object locker = new object();                  // Para proteger consola
    static object colaDiagnosticoLock = new object();     // Para cola de diagnóstico

    static List<Paciente> colaDiagnostico = new List<Paciente>(); // Lista para respetar orden y prioridad

    static void Main()
    {
        List<Thread> hilos = new List<Thread>();
        Random rand = new Random();

        for (int i = 1; i <= 20; i++)
        {
            int id = rand.Next(1, 101);
            int tiempoConsulta = rand.Next(5, 16);
            bool requiereDiagnostico = rand.Next(0, 2) == 0;
            int prioridad = rand.Next(1, 4); // 1: emergencia, 2: urgencia, 3: consulta general

            Paciente p = new Paciente(id, i * 2, tiempoConsulta, i);
            p.RequiereDiagnostico = requiereDiagnostico;
            p.Prioridad = prioridad;

            Thread hilo = new Thread(() => FlujoPaciente(p));
            hilo.Start();
            hilos.Add(hilo);

            Thread.Sleep(2000); // Llegan cada 2 segundos
        }

        foreach (var hilo in hilos)
            hilo.Join();

        Console.WriteLine("\n--- TODOS LOS PACIENTES HAN SIDO ATENDIDOS ---");
    }

    static void FlujoPaciente(Paciente p)
    {
        p.FechaLlegadaReal = DateTime.Now;

        lock (locker)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id} (Prioridad {p.Prioridad}, Llegada #{p.OrdenLlegada}) ha llegado. Estado: {p.ObtenerEstado()}");
        }

        semaforoMedicos.Wait();
        p.Estado = 1;
        p.FechaInicioConsulta = DateTime.Now;

        lock (locker)
        {
            double espera = (p.FechaInicioConsulta - p.FechaLlegadaReal).TotalSeconds;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()}. Espera: {espera:F2}s");
        }

        Thread.Sleep(p.TiempoConsulta * 1000); // Simular consulta
        p.FechaFinConsulta = DateTime.Now;
        semaforoMedicos.Release();

        if (p.RequiereDiagnostico)
        {
            p.Estado = 2;
            lock (locker)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()} (esperando máquina y turno)");
            }

            // Añadir a la cola de diagnóstico
            lock (colaDiagnosticoLock)
            {
                colaDiagnostico.Add(p);
            }

            // Esperar turno basado en prioridad y orden de llegada
            bool turnoEsperado = false;
            while (!turnoEsperado)
            {
                lock (colaDiagnosticoLock)
                {
                    var siguiente = colaDiagnostico
                        .OrderBy(x => x.Prioridad)
                        .ThenBy(x => x.OrdenLlegada)
                        .FirstOrDefault();

                    if (siguiente != null && siguiente.OrdenLlegada == p.OrdenLlegada)
                    {
                        colaDiagnostico.Remove(p);
                        turnoEsperado = true;
                    }
                }

                if (!turnoEsperado)
                    Thread.Sleep(200); // espera activa controlada
            }

            maquinasDiagnostico.Wait();
            p.FechaInicioDiagnostico = DateTime.Now;

            lock (locker)
            {
                double esperaDiag = (p.FechaInicioDiagnostico - p.FechaFinConsulta).TotalSeconds;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Diagnóstico iniciado. EsperaDiagnóstico: {esperaDiag:F2}s");
            }

            Thread.Sleep(15000); // Simular diagnóstico
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
