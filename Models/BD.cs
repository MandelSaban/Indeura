using Microsoft.Data.SqlClient;
using Dapper;

public static class BD
{
    private static string _connectionString = @"Server=localhost;DataBase=Indeura;Integrated Security=True;TrustServerCertificate=True;";

    public static User IniciarSesion(string usuario, string password)
    {
        User user = null;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
            SELECT 1
            FROM User AS User
            WHERE User.UserName = @usuario
              AND User.PasswordHash = @password";

            var result = connection.QueryFirstOrDefault<int?>(query, new { usuario, password });

            if (result != null)
            {
                user = new User();
                user.Id = dameId(usuario);
                user.UserName = usuario;
                user.PasswordHash = password;
                User data = getUserData(user.Id);                
                user.ProfilePicture = data.ProfilePicture;
                user.Email = data.Email;
                user.IsDeveloper = data.IsDeveloper;
                user.Followers = data.Followers;
            }
        }
        return user;
    }
    /*
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


}*/