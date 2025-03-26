public class Paciente
{
    public int Id { get; set; }
    public int LlegadaHospital { get; set; }
    public int TiempoConsulta { get; set; }
    public int Estado { get; set; }
    public int OrdenLlegada { get; set; }
    public bool RequiereDiagnostico { get; set; }
    public int Prioridad { get; set; }

    public DateTime FechaLlegadaReal { get; set; }
    public DateTime FechaInicioConsulta { get; set; }
    public DateTime FechaFinConsulta { get; set; }
    public DateTime FechaInicioDiagnostico { get; set; }
    public DateTime FechaFinDiagnostico { get; set; }

    public Paciente(int id, int llegadaHospital, int tiempoConsulta, int ordenLlegada)
    {
        Id = id;
        LlegadaHospital = llegadaHospital;
        TiempoConsulta = tiempoConsulta;
        OrdenLlegada = ordenLlegada;
        Estado = 0;
    }

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
