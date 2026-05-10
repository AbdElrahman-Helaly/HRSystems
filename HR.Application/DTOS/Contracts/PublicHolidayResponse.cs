namespace internalEmployee.Auth.Contracts;

public sealed class PublicHolidayResponse
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public int Year { get; init; }
    public bool IsActive { get; init; }
    public List<PublicHolidayExceptionResponse> Exceptions { get; init; } = new();
}
