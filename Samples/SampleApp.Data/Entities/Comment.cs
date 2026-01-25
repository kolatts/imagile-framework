namespace SampleApp.Data.Entities;

public class Comment
{
    public int CommentId { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedDate { get; set; }
    public int BlogPostId { get; set; }
    public BlogPost? BlogPost { get; set; }
    public int AuthorId { get; set; }
    public User? Author { get; set; }
}
