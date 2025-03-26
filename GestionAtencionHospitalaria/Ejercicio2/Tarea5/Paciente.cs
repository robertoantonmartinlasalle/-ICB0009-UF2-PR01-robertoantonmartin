using System;

public class Paciente
{
    public int Id { get; set; }
    public int Prioridad { get; set; } // 1 = Emergencia, 2 = Urgencia, 3 = General
    public int TiempoConsulta { get; set; }
    public int OrdenLlegada { get; set; }
    public bool RequiereDiagnostico { get; set; }
    public int Estado { get; set; } // 0 = EsperaConsulta, 1 = Consulta, 2 = EsperaDiagnóstico, 3 = Finalizado

    public DateTime FechaLlegada { get; set; }
    public DateTime FechaInicioConsulta { get; set; }
    public DateTime FechaFinConsulta { get; set; }
    public DateTime FechaInicioDiagnostico { get; set; }
    public DateTime FechaFinDiagnostico { get; set; }

    public string ObtenerEstado()
    {
        return Estado switch
        {
            0 => "EsperaConsulta",
            1 => "Consulta",
            2 => "EsperaDiagnostico",
            3 => "Finalizado",
            _ => "Desconocido"
        };
    }
}
