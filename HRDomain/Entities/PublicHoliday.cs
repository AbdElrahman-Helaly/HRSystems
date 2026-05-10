namespace internalEmployee.Data.Entities;

public sealed class PublicHoliday
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public required string Name { get; set; }
    public string? NameAr { get; set; }
    public int Year { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PublicHolidayException> Exceptions { get; set; } = new List<PublicHolidayException>();
}
