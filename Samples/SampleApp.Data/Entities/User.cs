namespace SampleApp.Data.Entities;

public class User
{
    public int UserId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public UserRoleType RoleType { get; set; }
}

public enum UserRoleType
{
    Admin = 1,
    User = 2,
    Guest = 3
}
