namespace JuntalyColegios.Application.Excepciones;

/// <summary>El usuario autenticado no puede operar sobre el recurso (p.ej. no es supervisor de ningún colegio activo).</summary>
public class AccesoDenegadoException(string mensaje) : Exception(mensaje);
