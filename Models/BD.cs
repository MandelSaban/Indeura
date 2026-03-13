using Microsoft.Data.SqlClient;
using Dapper;

public static class BD
{
    private static string _connectionString = @"Server=localhost;DataBase=YUNOVIVA YUOOOOO SEADEA kraco;Integrated Security=True;TrustServerCertificate=True;";

    public static Usuarios IniciarSesion(string usuario, string password)
    {
        Usuarios user = null;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
            SELECT 1
            FROM Usuarios AS Usuario
            WHERE Usuario.NombreUsuario = @usuario
              AND Usuario.Contraseña = @password";

            var result = connection.QueryFirstOrDefault<int?>(query, new { usuario, password });

            if (result != null)
            {
                user = new Usuarios();
                user.Id = dameId(usuario);
                user.NombreUsuario = usuario;
                user.Contraseña = password;
                Usuarios data = getUserData(user.Id);
                user.Nombre = data.Nombre;
                user.Apellido = data.Apellido;
                user.TipoCuenta = data.TipoCuenta;
                user.fechaNacimiento = data.fechaNacimiento;
                user.IdInstitucion = data.IdInstitucion;
                user.fotoPerfil = data.fotoPerfil;
                user.Email = data.Email;
            }
        }
        return user;
    }

    private static Usuarios getUserData(int id)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
            SELECT Nombre, Apellido, TipoCuenta, fechaNacimiento, IdInstitucion, fotoPerfil, Email
            FROM Usuarios
            WHERE Id = @id";

            var result = connection.QueryFirstOrDefault<Usuarios>(query, new { id });
            return result;
        }
    }


    public static bool Existe(string usuario)
    {
        bool existe = false;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
                SELECT 1
                FROM Usuarios
                WHERE Usuarios.NombreUsuario = @usuario";

            var result = connection.QueryFirstOrDefault<int?>(query, new { usuario });
            if (result != null)
            {
                existe = true;
            }
        }
        return existe;
    }

    public static int dameId(string usuario)
    {
        int id = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
                SELECT Usuarios.Id
                FROM Usuarios
                WHERE Usuarios.NombreUsuario = @usuario";
            if (Existe(usuario))
            {
                var result = connection.QueryFirstOrDefault<int>(query, new { usuario });
                id = result;
            }

        }
        return id;
    }
    public static bool Registrarte(string Nombre, string Apellido, string NombreUsuario, string Contraseña, string Email, string TipoCuenta, DateTime fechaNacimiento, string NombreInstitucion)
    {
        bool registrado = false;
        //int IdInstitucion = pedirIdInstitucion(NombreInstitucion);
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"            
            INSERT INTO Usuarios (Nombre, Apellido, NombreUsuario, Contraseña, Email, TipoCuenta, fechaNacimiento, IdInstitucion)
            VALUES (@Nombre, @Apellido, @NombreUsuario, @Contraseña, @Email, @TipoCuenta, @fechaNacimiento, (SELECT Id FROM Instituciones WHERE nombre = @NombreInstitucion));";

            if(!ExisteInstitucion(NombreInstitucion)){
                AgregarInstitucion(NombreInstitucion);
            }

            if (!Existe(NombreUsuario) && !MismoMail(Email))
            {
                connection.Execute(query, new { Nombre, Apellido, NombreUsuario, Contraseña, Email, TipoCuenta, fechaNacimiento, NombreInstitucion });
                registrado = true;
            }
        }
        return registrado;
    }

    public static bool MismoMail(string email)
    {
        bool existe = false;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
                SELECT 1
                FROM Usuarios
                WHERE Usuarios.Email = @email";

            var result = connection.QueryFirstOrDefault<int?>(query, new { email });
            if (result != null)
            {
                existe = true;
            }
        }
        return existe;
    }

    public static void updateProfilePicture(string FileName,int userId)
    {
        string query = "UPDATE Usuarios SET fotoPerfil = @FileName WHERE Id = @userId";
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Execute(query, new { FileName,userId });
        }
    }


}