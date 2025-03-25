using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    static Queue<Paciente> colaPacientes = new Queue<Paciente>();
    static List<Paciente> pacientes = new List<Paciente>();

    static object colaLock = new object();
    static object consolaLock = new object();
    static object contadorLock = new object();

    static Random random = new Random();
    static readonly object randomLock = new object();

    static int pacientesAtendidos = 0;

    static void Main(string[] args)
    {
        GenerarPacientes(10);

        Console.WriteLine("\n=== PACIENTES GENERADOS ===\n");
        foreach (var p in pacientes)
        {
            Console.WriteLine($"Paciente ID: {p.Id}, Llega en: {p.LlegadaHospital}s, Tiempo consulta: {p.TiempoConsulta}s");
        }

        Console.WriteLine("\n=== INICIANDO ATENCIÓN MÉDICA ===\n");

        for (int i = 1; i <= 3; i++)
        {
            int idMedico = i;
            Thread hiloMedico = new Thread(() => AtenderPacientes(idMedico));
            hiloMedico.Start();
        }

        for (int i = 0; i < pacientes.Count; i++)
        {
            int orden = i + 1;
            Paciente paciente = pacientes[i];
            Thread hiloLlegada = new Thread(() => SimularLlegada(paciente, orden));
            hiloLlegada.Start();
        }
    }

    static void GenerarPacientes(int cantidad)
    {
        List<int> idsUsados = new List<int>();

        for (int i = 0; i < cantidad; i++)
        {
            int id;

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

            Paciente p = new Paciente(id, i, tiempoConsulta, i + 1);
            pacientes.Add(p);
        }
    }

    static void SimularLlegada(Paciente paciente, int orden)
    {
        Thread.Sleep(paciente.LlegadaHospital * 1000);
        paciente.FechaLlegadaReal = DateTime.Now;

        lock (consolaLock)
        {
            Console.WriteLine($"[{paciente.FechaLlegadaReal:HH:mm:ss}] Paciente {paciente.Id} ha llegado al hospital (orden {orden}). Estado: Espera.");
        }

        lock (colaLock)
        {
            colaPacientes.Enqueue(paciente);
        }
    }

    static void AtenderPacientes(int idMedico)
    {
        while (true)
        {
            Paciente? paciente = null;

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
                            break;
                    }
                }
            }

            if (paciente != null)
            {
                paciente.FechaInicioConsulta = DateTime.Now;
                paciente.Estado = 1;

                TimeSpan duracionEspera = paciente.FechaInicioConsulta - paciente.FechaLlegadaReal;

                lock (consolaLock)
                {
                    Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Estado: Consulta. Duración Espera: {duracionEspera.TotalSeconds:n0} segundos.");
                }

                Thread.Sleep(paciente.TiempoConsulta * 1000);

                paciente.FechaFinConsulta = DateTime.Now;
                paciente.Estado = 2;

                TimeSpan duracionConsulta = paciente.FechaFinConsulta - paciente.FechaInicioConsulta;

                lock (consolaLock)
                {
                    Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Estado: Finalizado. Duración Consulta: {duracionConsulta.TotalSeconds:n0} segundos.");
                }

                lock (contadorLock)
                {
                    pacientesAtendidos++;
                }
            }
            else
            {
                Thread.Sleep(200);
            }
        }
    }
}
