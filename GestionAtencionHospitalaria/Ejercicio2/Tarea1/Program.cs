using System;
using System.Threading;
using System.Collections.Generic;

class Program
{
    static SemaphoreSlim medicos = new SemaphoreSlim(4); // 4 médicos disponibles
    static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2); // 2 máquinas de diagnóstico
    static object locker = new object(); // para proteger secciones críticas

    static void Main()
    {
        List<Thread> hilos = new List<Thread>();
        Random rand = new Random();

        for (int i = 1; i <= 4; i++)
        {
            int id = rand.Next(1, 101);
            int tiempoConsulta = rand.Next(5, 16); // 5 a 15 seg
            bool requiereDiagnostico = rand.Next(0, 2) == 0;

            Paciente p = new Paciente(id, llegadaHospital: i * 2, tiempoConsulta, ordenLlegada: i);
            p.RequiereDiagnostico = requiereDiagnostico;

            Thread t = new Thread(() => FlujoPaciente(p));
            hilos.Add(t);
            t.Start();

            Thread.Sleep(2000); // llega uno cada 2 segundos
        }

        // Esperar a que terminen todos los pacientes
        foreach (var h in hilos)
            h.Join();

        Console.WriteLine("\n--- TODOS LOS PACIENTES HAN SIDO ATENDIDOS ---");
    }

    static void FlujoPaciente(Paciente p)
    {
        lock (locker)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id} (Llegada #{p.OrdenLlegada}) ha llegado. Estado: {p.ObtenerEstado()}");
        }

        // Espera consulta
        medicos.Wait();
        p.Estado = 1; // Consulta
        p.FechaInicioConsulta = DateTime.Now;

        lock (locker)
        {
            double espera = (p.FechaInicioConsulta - p.FechaLlegadaReal).TotalSeconds;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()}. Espera: {espera:F2}s");
        }

        Thread.Sleep(p.TiempoConsulta * 1000); // Simula consulta

        p.FechaFinConsulta = DateTime.Now;
        medicos.Release(); // médico queda libre

        // Diagnóstico
        if (p.RequiereDiagnostico)
        {
            p.Estado = 2; // EsperaDiagnostico

            lock (locker)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()} (esperando máquina)");
            }

            maquinasDiagnostico.Wait();
            p.FechaInicioDiagnostico = DateTime.Now;

            lock (locker)
            {
                double esperaDiag = (p.FechaInicioDiagnostico - p.FechaFinConsulta).TotalSeconds;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Diagnóstico iniciado. EsperaDiagnóstico: {esperaDiag:F2}s");
            }

            Thread.Sleep(15000); // diagnóstico dura 15s
            p.FechaFinDiagnostico = DateTime.Now;

            maquinasDiagnostico.Release();
        }

        p.Estado = 3; // Finalizado

        lock (locker)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()}.");
        }
    }
}
