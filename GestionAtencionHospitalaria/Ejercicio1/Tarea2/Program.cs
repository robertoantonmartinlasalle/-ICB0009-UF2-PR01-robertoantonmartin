using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    // Lista para almacenar los pacientes creados
    static List<Paciente> pacientes = new List<Paciente>();

    // Array de semáforos para los 3 médicos (capacidad 1)
    static SemaphoreSlim[] medicos = new SemaphoreSlim[3];

    // Random protegido por lock para uso seguro en entornos multihilo
    static Random random = new Random();
    static readonly object randomLock = new object();

    static void Main(string[] args)
    {
        // Inicializamos los semáforos (uno por médico)
        for (int i = 0; i < medicos.Length; i++)
        {
            medicos[i] = new SemaphoreSlim(1); // Capacidad 1 = un paciente por médico
        }

        // Generamos los pacientes
        GenerarPacientes(10);

        // Mostrar los datos iniciales
        Console.WriteLine("\n=== PACIENTES GENERADOS ===\n");
        foreach (var p in pacientes)
        {
            Console.WriteLine($"Paciente ID: {p.Id}, Llega en: {p.LlegadaHospital}s, Tiempo consulta: {p.TiempoConsulta}s");
        }

        Console.WriteLine("\n=== INICIANDO ATENCIONES ===\n");

        // Lanzar un hilo por paciente
        foreach (var paciente in pacientes)
        {
            Thread hiloPaciente = new Thread(() => AtenderPaciente(paciente));
            hiloPaciente.Start();
        }
    }

    // Método para generar pacientes únicos
    static void GenerarPacientes(int cantidad)
    {
        List<int> idsUsados = new List<int>();

        for (int i = 0; i < cantidad; i++)
        {
            int id = GenerarIdUnico(idsUsados);

            int tiempoConsulta;
            lock (randomLock)
            {
                tiempoConsulta = random.Next(5, 16); // Tiempo consulta entre 5 y 15 seg
            }

            Paciente p = new Paciente(id, i, tiempoConsulta);
            pacientes.Add(p);
        }
    }

    // Método para generar un ID único entre 1 y 100
    static int GenerarIdUnico(List<int> usados)
    {
        int id;

        while (true)
        {
            lock (randomLock)
            {
                id = random.Next(1, 101); // ID entre 1 y 100
            }

            if (!usados.Contains(id))
            {
                usados.Add(id);
                break;
            }
        }

        return id;
    }

    // Simula la atención médica de un paciente
    static void AtenderPaciente(Paciente paciente)
    {
        // Esperar a que llegue al hospital
        Thread.Sleep(paciente.LlegadaHospital * 1000);

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [LLEGADA] Paciente {paciente.Id} ha llegado al hospital (segundo {paciente.LlegadaHospital})");

        int medicoAsignado = -1;

        // Buscar médico libre
        while (true)
        {
            int candidato;

            lock (randomLock)
            {
                candidato = random.Next(0, 3); // Médico 0 a 2
            }

            if (medicos[candidato].Wait(0))
            {
                medicoAsignado = candidato + 1;
                break;
            }

            Thread.Sleep(100); // Esperar un poco si están ocupados
        }

        paciente.Estado = 1; // En consulta

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [CONSULTA] Paciente {paciente.Id} está siendo atendido por el médico {medicoAsignado} durante {paciente.TiempoConsulta} segundos");

        Thread.Sleep(paciente.TiempoConsulta * 1000); // Simular consulta

        paciente.Estado = 2; // Finalizado

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [SALIDA] Paciente {paciente.Id} ha salido de consulta del médico {medicoAsignado}");

        medicos[medicoAsignado - 1].Release(); // Liberar al médico
    }
}
