public class Paciente
{
    public int Id { get; set; }
    public int LlegadaHospital { get; set; }
    public int TiempoConsulta { get; set; }
    public int Estado { get; set; }
    public int OrdenLlegada { get; set; }

    // Nuevas propiedades para Tarea 3
    public DateTime FechaLlegadaReal { get; set; }
    public DateTime FechaInicioConsulta { get; set; }
    public DateTime FechaFinConsulta { get; set; }

    public Paciente(int id, int llegadaHospital, int tiempoConsulta, int ordenLlegada)
    {
        Id = id;
        LlegadaHospital = llegadaHospital;
        TiempoConsulta = tiempoConsulta;
        Estado = 0;
        OrdenLlegada = ordenLlegada;
    }

    // Método auxiliar para mostrar el estado como texto
    public string ObtenerEstado()
    {
        return Estado switch
        {
            0 => "Espera",
            1 => "Consulta",
            2 => "Finalizado",
            _ => "Desconocido"
        };
    }
}
