public class Paciente
{
    // Identificador único entre 1 y 100
    public int Id { get; set; }

    // Segundo en el que llegó al hospital
    public int LlegadaHospital { get; set; }

    // Tiempo de consulta en segundos (entre 5 y 15)
    public int TiempoConsulta { get; set; }

    // Estado del paciente: 0 = espera, 1 = en consulta, 2 = finalizado
    public int Estado { get; set; }

    // Constructor
    public Paciente(int id, int llegadaHospital, int tiempoConsulta)
    {
        Id = id;
        LlegadaHospital = llegadaHospital;
        TiempoConsulta = tiempoConsulta;
        Estado = 0; // Estado inicial: espera
    }
}
