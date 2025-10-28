namespace ProjectBrain.Domain;

public class UserDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string? FirstName => FullName.Split(' ').FirstOrDefault();
    public DateOnly DoB { get; set; }
    public string FavoriteColour { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
    public bool IsOnboarded { get; set; }
    public string? PreferredPronoun { get; set; }
    public string? NeurodivergentDetails { get; set; }
    public string? Address { get; set; }
    public string? Experience { get; set; }
}