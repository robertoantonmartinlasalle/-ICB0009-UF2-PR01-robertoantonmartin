using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    // Cola compartida donde se encolan los pacientes a medida que llegan
    static Queue<Paciente> colaPacientes = new Queue<Paciente>();

    // Lista donde se almacenan los pacientes generados
    static List<Paciente> pacientes = new List<Paciente>();

    // Objetos de sincronización para proteger recursos compartidos
    static object colaLock = new object();      // Para proteger el acceso a la cola
    static object consolaLock = new object();   // Para proteger la consola
    static object contadorLock = new object();  // Para contar los pacientes atendidos

    // Random con protección para evitar colisiones entre hilos
    static Random random = new Random();
    static readonly object randomLock = new object();

    // Contador global para saber cuántos pacientes han sido atendidos
    static int pacientesAtendidos = 0;

    static void Main(string[] args)
    {
        // Generamos 10 pacientes
        GenerarPacientes(10);

        // Mostramos los datos generados por consola
        Console.WriteLine("\n=== PACIENTES GENERADOS ===\n");
        foreach (var p in pacientes)
        {
            Console.WriteLine($"Paciente ID: {p.Id}, Llega en: {p.LlegadaHospital}s, Tiempo consulta: {p.TiempoConsulta}s");
        }

        Console.WriteLine("\n=== INICIANDO ATENCIÓN MÉDICA ===\n");

        // Creamos 3 hilos, uno por cada médico
        for (int i = 1; i <= 3; i++)
        {
            int idMedico = i;
            Thread hiloMedico = new Thread(() => AtenderPacientes(idMedico));
            hiloMedico.Start();
        }

        // Lanzamos hilos para simular la llegada de los pacientes
        foreach (var paciente in pacientes)
        {
            Thread hiloLlegada = new Thread(() => SimularLlegada(paciente));
            hiloLlegada.Start();
        }
    }

    // Método que genera pacientes con IDs únicos y tiempos aleatorios
    static void GenerarPacientes(int cantidad)
    {
        List<int> idsUsados = new List<int>();

        for (int i = 0; i < cantidad; i++)
        {
            int id;

            // Generar ID único (1–100)
            while (true)
            {
                lock (randomLock)
                {
                    id = random.Next(1, 101);
                }

                if (!idsUsados.Contains(id))
                {
                    idsUsados.Add(id);
                    break;
                }
            }

            int tiempoConsulta;
            lock (randomLock)
            {
                tiempoConsulta = random.Next(5, 16);
            }

            // Simulamos la llegada en los segundos del 0 al 9
            Paciente p = new Paciente(id, i, tiempoConsulta);
            pacientes.Add(p);
        }
    }

    // Simula la llegada de un paciente al hospital
    static void SimularLlegada(Paciente paciente)
    {
        Thread.Sleep(paciente.LlegadaHospital * 1000); // Espera hasta el momento de llegada

        lock (consolaLock)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [LLEGADA] Paciente {paciente.Id} ha llegado al hospital (segundo {paciente.LlegadaHospital})");
        }

        lock (colaLock)
        {
            colaPacientes.Enqueue(paciente); // Se encola el paciente
        }
    }

    // Cada médico atiende pacientes extraídos de la cola
    static void AtenderPacientes(int idMedico)
    {
        while (true)
        {
            Paciente? paciente = null; // <-- se permite valor null y se elimina el warning CS8600

            // Intentar obtener un paciente de la cola
            lock (colaLock)
            {
                if (colaPacientes.Count > 0)
                {
                    paciente = colaPacientes.Dequeue();
                }
                else
                {
                    lock (contadorLock)
                    {
                        if (pacientesAtendidos >= pacientes.Count)
                            break; // Si ya se atendieron todos, salir del bucle
                    }
                }
            }

            if (paciente != null)
            {
                paciente.Estado = 1; // En consulta

                lock (consolaLock)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [CONSULTA] Médico {idMedico} atiende al paciente {paciente.Id} durante {paciente.TiempoConsulta}s");
                }

                Thread.Sleep(paciente.TiempoConsulta * 1000); // Simula la duración de la consulta

                paciente.Estado = 2; // Finalizado

                lock (consolaLock)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [SALIDA] Paciente {paciente.Id} ha terminado con el médico {idMedico}");
                }

                lock (contadorLock)
                {
                    pacientesAtendidos++; // Actualizamos el contador global
                }
            }
            else
            {
                Thread.Sleep(200); // Esperamos antes de volver a mirar la cola
            }
        }
    }
}
