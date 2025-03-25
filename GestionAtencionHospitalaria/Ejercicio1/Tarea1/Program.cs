using System;
using System.Threading;

class Program
{
    // Array de 4 médicos, cada uno representado por un semáforo con capacidad 1
    static SemaphoreSlim[] medicos = new SemaphoreSlim[4];

    // Random global con bloqueo para evitar conflictos entre hilos
    static Random random = new Random();
    static readonly object randomLock = new object();

    static void Main(string[] args)
    {
        // Inicializamos los semáforos, uno por médico
        for (int i = 0; i < medicos.Length; i++)
        {
            medicos[i] = new SemaphoreSlim(1); // Capacidad 1 = un paciente por médico
        }

        // Simular llegada de 4 pacientes, uno cada 2 segundos
        for (int i = 1; i <= 4; i++)
        {
            int numeroPaciente = i;

            // Simular la espera entre la llegada de pacientes
            Thread.Sleep(2000);

            // Lanzamos un hilo por paciente
            Thread hiloPaciente = new Thread(() => AtenderPaciente(numeroPaciente));
            hiloPaciente.Start();
        }
    }

    static void AtenderPaciente(int numeroPaciente)
    {
        int medicoAsignado = -1;

        // Bucle para buscar un médico libre (ocupado = espera)
        while (true)
        {
            int candidato;

            // Generar número aleatorio protegido por lock
            lock (randomLock)
            {
                candidato = random.Next(0, 4); // índice de 0 a 3
            }

            // Intentar ocupar el médico (sin bloquear si está ocupado)
            if (medicos[candidato].Wait(0))
            {
                medicoAsignado = candidato + 1; // Para mostrar del 1 al 4
                break;
            }
            else
            {
                // Esperamos antes de volver a intentar
                Thread.Sleep(100);
            }
        }

        // Mostrar mensaje de llegada con hora
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [LLEGADA] Paciente {numeroPaciente} ha llegado. Asignado al médico {medicoAsignado}.");

        // Simula 10 segundos de atención médica
        Thread.Sleep(10000);

        // Mostrar mensaje de salida con hora
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [SALIDA] Paciente {numeroPaciente} ha salido de la consulta del médico {medicoAsignado}.");

        // Liberar al médico para que atienda a otro paciente
        medicos[medicoAsignado - 1].Release();
    }
}
