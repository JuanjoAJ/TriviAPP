

[System.Serializable]
public class Jugador
{
    public string idUsuario;
    public string email;
    public string alias;        
    public string avatar;
    public string codigoUsuario;
    public string tipoLogin;
    public int puntuacionTotal;
    public string estadoConexion;
}



[System.Serializable]
public class Perfil
{
    public string alias;
    public string avatar;
}

public enum EstadoConexion
{
    EN_LINEA,
    INACTIVO,
    DESCONECTADO
}
