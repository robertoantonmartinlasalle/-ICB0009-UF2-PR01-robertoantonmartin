public class Paciente
{
    // Identificador único del paciente (entre 1 y 100)
    public int Id { get; set; }

    // Segundo en el que llega al hospital (de 0 a 9)
    public int LlegadaHospital { get; set; }

    // Tiempo de consulta con el médico (entre 5 y 15 segundos)
    public int TiempoConsulta { get; set; }

    // Estado actual del paciente:
    // 0 = Espera, 1 = Consulta, 2 = Finalizado
    public int Estado { get; set; }

    // Orden de llegada (de 1 a 10)
    public int OrdenLlegada { get; set; }

    // Constructor
    public Paciente(int id, int llegadaHospital, int tiempoConsulta, int ordenLlegada)
    {
        Id = id;
        LlegadaHospital = llegadaHospital;
        TiempoConsulta = tiempoConsulta;
        Estado = 0; // Estado inicial: Espera
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
