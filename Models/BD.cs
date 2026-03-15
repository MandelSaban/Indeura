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
            FROM [User] u
            WHERE u.UserName = @usuario
              AND u.PasswordHash = @password";

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

    public static bool Registrarte(string Nombre, string Contraseña, string Email, bool IsDeveloper)
    {
        bool registrado = false;
        string Password = Contraseña;

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"            
            INSERT INTO [User] (UserName, PasswordHash, Email, Followers, Followed,IsDeveloper, GamesOwned)
            VALUES (@Nombre, @Password, @Email, 0, 0, @IsDeveloper, 0);";

            if (!Existe(Nombre) && !MismoMail(Email))
            {
                connection.Execute(query, new { Nombre, Password, Email, IsDeveloper });
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
                SELECT Id
                FROM [User]
                WHERE UserName = @usuario";
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
                FROM [User]
                WHERE UserName = @usuario";

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
            FROM [User]
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
                FROM [User]
                WHERE Email = @email";

            var result = connection.QueryFirstOrDefault<int?>(query, new { email });
            if (result != null)
            {
                existe = true;
            }
        }
        return existe;
    }

    public static void updateProfilePicture(string FileName, int userId)
    {
        string query = "UPDATE [User] SET ProfilePicture = @FileName WHERE Id = @userId";
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Execute(query, new { FileName, userId });
        }
    }

    public static Game findGameById(int gameId)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
            SELECT Id, IdPublisher, Date, NumberOfAchievements,GameName,Description, PriceUSD, DiscountPercentage
            FROM Game
            WHERE Id = @gameId";

            var result = connection.QueryFirstOrDefault<Game>(query, new { gameId });
            return result;
        }
    }

    public static string getNameById(int userId)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
            SELECT UserName
            FROM [User]
            WHERE Id = @userId";

            var result = connection.QueryFirstOrDefault<string>(query, new { userId });
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
            WHERE IdGame = @gameId";

            var result = connection.Query<GamePictures>(query, new { gameId }).ToList();
            return result;
        }
    }

    public static List<Review> getGameReviews(int gameId)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
            SELECT *
            FROM Review
            WHERE IdGame = @gameId";

            var result = connection.Query<Review>(query, new { gameId }).ToList();
            return result;
        }
    }

    public static void publishReview(int idGame, int idUser, double rate, int seconds, string description)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"            
            INSERT INTO Review (IdUser,IdGame,Rating,Description,PlaytimeSecondsAtPublish)
            VALUES (@idUser, @idGame, @rate, @description, @seconds);";

            connection.Execute(query, new { idUser, idGame, rate, description, seconds });
        }
    }

    public static int GetPlaytime(int userId, int gameId)
{
    using (SqlConnection connection = new SqlConnection(_connectionString))
    {
        string query = @"
        SELECT PlaytimeSeconds
        FROM Ownership
        WHERE IdUser = @userId AND IdGame = @gameId";

        return connection.QueryFirstOrDefault<int>(query, new { userId, gameId });
    }
}

public static bool GetReviewed(int userId, int gameId)
{
    using (SqlConnection connection = new SqlConnection(_connectionString))
    {
        string query = @"
        SELECT 1
        FROM Review
        WHERE IdUser = @userId AND IdGame = @gameId";

        var result = connection.QueryFirstOrDefault<int>(query, new { userId, gameId });
        return (result == 1);
    }
}

    public static List<string> getGameTags(int gameId)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
            SELECT gt.TagName
            FROM GameTagXGame gxg
            JOIN GameTags gt ON gxg.IdTag = gt.Id
            WHERE gxg.IdGame = @gameId";

            var result = connection.Query<string>(query, new { gameId }).ToList();
            return result;
        }
    }

    public static bool IsOwner(int gameId, int userId)
    {
        bool existe = false;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"
                SELECT 1
                FROM Ownership
                WHERE IdUser = @userId AND IdGame = @gameId";

            var result = connection.QueryFirstOrDefault<int?>(query, new { userId, gameId });
            if (result != null)
            {
                existe = true;
            }
        }
        return existe;
    }

    public static void AddOwnership(int idGame, int idUser)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = @"            
            INSERT INTO Ownership (IdUser,IdGame,Date,PlaytimeSeconds)
            VALUES (@idUser, @idGame, CAST(GETDATE() AS DATE), 0);";

            connection.Execute(query, new { idUser, idGame });
        }
    }

}






