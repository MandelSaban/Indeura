using BCrypt.Net;
public static class Hashing
{
    public static string passwordHash(string password)
    {
        string hashedPassword="";
        hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        return hashedPassword;
    }

    public static bool stringEqualsHash(string text, string hash)
    {
        return (passwordHash(text)==hash);
    }
}