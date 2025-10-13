using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public required string Id { get; init; }

    public required string Email { get; init; }

    public required string FullName { get; set; }
    public string? FirstName => FullName.Split(' ').FirstOrDefault();
    public required string FavoriteColor { get; set; }

    public required DateOnly DoB { get; set; }

    public bool IsOnboarded { get; set; } = false;
}