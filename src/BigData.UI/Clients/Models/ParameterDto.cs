namespace BigData.UI.Clients.Models;

public sealed record ParameterDto
{
    public string Type { get; set; } = string.Empty;
    public double Value { get; set; }
}
