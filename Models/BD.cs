using Microsoft.Data.SqlClient;
using Dapper;
using System.Drawing;

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
                user.GamesOwned = data.GamesOwned;
                user.Followed = data.Followed;
                user.Description = data.Description;
                
            }
        }
        return user;
    }

    public static bool Registrarte(string Nombre,  string Contraseña, string Email, bool IsDeveloper)
    {
        bool registrado = false;
        string Password = Contraseña;
        //int IdInstitucion = pedirIdInstitucion(NombreInstitucion);
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"            
            INSERT INTO User (UserName, PasswordHash, Email, Followers, Followed,IsDeveloper, GamesOwned)
            VALUES (@Nombre, @Password, @Email, 0, 0, @IsDeveloper, 0);";

            if (!Existe(Nombre) && !MismoMail(Email))
            {
                connection.Execute(query, new { Nombre, Contraseña, Email });
                registrado = true;
            }
        }
        return registrado;
    }

    public static int dameId(string usuario)
    {
        int id = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
                SELECT User.Id
                FROM User
                WHERE User.UserName = @usuario";
            if (Existe(usuario))
            {
                var result = connection.QueryFirstOrDefault<int>(query, new { usuario });
                id = result;
            }

        }
        return id;
    }

    public static bool Existe(string usuario)
    {
        bool existe = false;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
                SELECT 1
                FROM User
                WHERE User.UserName = @usuario";

            var result = connection.QueryFirstOrDefault<int?>(query, new { usuario });
            if (result != null)
            {
                existe = true;
            }
        }
        return existe;
    }

    private static User getUserData(int id)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
            SELECT Id, IsDeveloper, Followers, GamesOwned, Followed, UserName, ProfilePicture,Description,Email,PasswordHash
            FROM User
            WHERE Id = @id";

            var result = connection.QueryFirstOrDefault<User>(query, new { id });
            return result;
        }
    }

    public static bool MismoMail(string email)
    {
        bool existe = false;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
                SELECT 1
                FROM User
                WHERE User.Email = @email";

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
        string query = "UPDATE User SET ProfilePicture = @FileName WHERE Id = @userId";
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Execute(query, new { FileName,userId });
        }
    }

    public static Game findGameById(int gameId)
    {
         using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
            SELECT Id, IdPublisher, Date, NumberOfAchievements,GameName,Description
            FROM Game
            WHERE Id = @gameId";

            var result = connection.QueryFirstOrDefault<Game>(query, new { gameId });
            return result;
        }
    }
    public static List<GamePictures> getGamePictures(int gameId)
{
    using (SqlConnection connection = new SqlConnection(_connectionString))
    {
        string query = @"
            SELECT *
            FROM GamePictures
            WHERE GamePictures.IdGame = @gameId";

        var result = connection.Query<GamePictures>(query, new { gameId }).ToList();
        return result;
    }
}
}
    

    

    

