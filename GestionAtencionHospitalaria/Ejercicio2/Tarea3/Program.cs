using System;
using System.Threading;
using System.Collections.Generic;

class Program
{
    static SemaphoreSlim semaforoMedicos = new SemaphoreSlim(4); // 4 médicos disponibles
    static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2); // 2 máquinas de diagnóstico
    static object locker = new object(); // sincronización de consola y datos compartidos
    static object turnoDiagnosticoLock = new object(); // para proteger la cola de diagnóstico

    // Nueva implementación: usamos una cola FIFO en vez de lista + Min()
    static Queue<Paciente> colaDiagnostico = new Queue<Paciente>();

    static void Main()
    {
        List<Thread> hilos = new List<Thread>();
        Random rand = new Random();

        for (int i = 1; i <= 20; i++)
        {
            int id = rand.Next(1, 101);
            int tiempoConsulta = rand.Next(5, 16); // de 5 a 15 segundos
            bool requiereDiagnostico = rand.Next(0, 2) == 0;

            Paciente p = new Paciente(id, i * 2, tiempoConsulta, i);
            p.RequiereDiagnostico = requiereDiagnostico;

            Thread hilo = new Thread(() => FlujoPaciente(p));
            hilos.Add(hilo);
            hilo.Start();

            Thread.Sleep(2000); // Simular llegada cada 2 segundos
        }

        // Esperamos que todos los hilos finalicen
        foreach (var hilo in hilos)
            hilo.Join();

        Console.WriteLine("\n--- TODOS LOS PACIENTES HAN SIDO ATENDIDOS ---");
    }

    static void FlujoPaciente(Paciente p)
    {
        p.FechaLlegadaReal = DateTime.Now;

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

        Thread.Sleep(p.TiempoConsulta * 1000); // Simular la consulta médica
        p.FechaFinConsulta = DateTime.Now;
        semaforoMedicos.Release();

        // Si necesita diagnóstico, se encola de forma sincronizada
        if (p.RequiereDiagnostico)
        {
            p.Estado = 2;

            lock (locker)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()} (esperando máquina y turno)");
            }

            // Encolar paciente de forma segura
            lock (turnoDiagnosticoLock)
            {
                colaDiagnostico.Enqueue(p);
            }

            // Esperar a que le toque su turno (debe estar al frente de la cola)
            bool turnoEsperado = false;
            while (!turnoEsperado)
            {
                lock (turnoDiagnosticoLock)
                {
                    if (colaDiagnostico.Count > 0 && ReferenceEquals(colaDiagnostico.Peek(), p))
                    {
                        colaDiagnostico.Dequeue(); // Ya es su turno
                        turnoEsperado = true;
                    }
                }

                if (!turnoEsperado)
                    Thread.Sleep(200); // Espera activa controlada
            }

            // Esperar máquina disponible
            maquinasDiagnostico.Wait();
            p.FechaInicioDiagnostico = DateTime.Now;

            lock (locker)
            {
                double esperaDiag = (p.FechaInicioDiagnostico - p.FechaFinConsulta).TotalSeconds;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Diagnóstico iniciado. EsperaDiagnóstico: {esperaDiag:F2}s");
            }

            Thread.Sleep(15000); // Simular prueba diagnóstica
            p.FechaFinDiagnostico = DateTime.Now;
            maquinasDiagnostico.Release();
        }

        // Finaliza el proceso
        p.Estado = 3;

        lock (locker)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Paciente {p.Id}. Estado: {p.ObtenerEstado()}.");
        }
    }
}
