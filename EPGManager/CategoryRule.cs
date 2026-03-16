namespace EPGManager.API;

public class CategoryRule
{
    public string OriginalCategory { get; set; } = string.Empty;
    public string NewCategory { get; set; } = string.Empty;
    // Optional: filters like by ID, name, etc.
}
