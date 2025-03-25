# Tarea 2 – Simulación de atención médica con llegada aleatoria

## Descripción

Este programa simula la llegada de **10 pacientes** a un hospital donde hay **3 médicos disponibles**.  
Cada paciente tiene un **tiempo de llegada** simulado entre los segundos 0 y 9, y un **tiempo de consulta aleatorio** entre 5 y 15 segundos.  
Cada médico atiende a los pacientes en orden de llegada, y cada uno está gestionado por un **hilo independiente**.

Los pacientes llegan progresivamente y se **encolan** a medida que llegan. Los médicos, funcionando en paralelo, van **atendiendo pacientes disponibles en la cola**, uno por uno, hasta que se ha completado toda la atención.

El programa está diseñado para respetar la concurrencia, asegurando que:
- Cada paciente es atendido individualmente por un único médico.
- La llegada y la atención se simulan mediante `Thread.Sleep()`.
- Se utilizan hilos para los médicos y una cola protegida para los pacientes.
- La integridad de los datos se garantiza mediante `lock`.

---

## Tecnologías utilizadas

- Lenguaje: C#
- Plataforma: .NET Console App
- Concurrencia: `Thread`, `Queue`, `lock`, `Random`

---

## Respuestas a las preguntas de la práctica

### ¿Cuántos hilos se están ejecutando en este programa?

Se ejecutan **13 hilos** en total:

- **10 hilos**, uno por cada paciente, que simulan su llegada al hospital en tiempos distintos.
- **3 hilos**, uno por cada médico, que atienden pacientes desde una cola compartida.

Cada hilo médico permanece activo hasta que se ha atendido a todos los pacientes.  
El hilo principal (`Main`) lanza todos los hilos y no participa directamente en la atención.

---

### ¿Cuál de los pacientes entra primero en consulta?

En este programa, los pacientes se encolan en orden de llegada.  
El primer paciente que **llega y encuentra a un médico disponible** será el primero en entrar en consulta.

Normalmente será el paciente que tiene `LlegadaHospital = 0`,  
pero el orden real puede verse afectado por la disponibilidad del médico en ese instante.

---

### ¿Cuál de los pacientes sale primero de consulta?

Depende de:
- El tiempo de llegada (`LlegadaHospital`)
- El tiempo de consulta (`TiempoConsulta`)
- Qué médico esté disponible primero

Un paciente que llega más tarde pero tiene un tiempo de consulta muy corto y encuentra un médico libre de inmediato **puede salir antes** que otro que llegó antes pero tuvo que esperar o tenía una consulta más larga.

Este comportamiento refleja una simulación concurrente más fiel a la realidad.

---

## Captura de pantalla

A continuación se muestra la ejecución del programa:

![Ejecución en consola](../../Capturas/Tarea2.png)
