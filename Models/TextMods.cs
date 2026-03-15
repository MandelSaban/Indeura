public static class TextMods
{
    public static string numberToStars(int n)
    {
        string text="";
        for (int i = 0; i < 5; i++)
        {
            if (i < n)
            {
                text+="★";
            }
            else
            {
                text+="☆";
            }
            
        }
        return text;
    }
}