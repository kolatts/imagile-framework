namespace SampleApp.Data.Entities;

public class BlogPost
{
    public int BlogPostId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime PublishedDate { get; set; }
    public bool IsPublished { get; set; }
    public int AuthorId { get; set; }
    public User? Author { get; set; }
}
