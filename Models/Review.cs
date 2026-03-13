public class Review
{
    public int Id { get; set; }

    public int IdUser { get; set; }
    public int IdGame { get; set; }

    public int Rating { get; set; }
    public string Description { get; set; }

    public long PlaytimeSecondsAtPublish { get; set; }

    public Review(){

    }
}