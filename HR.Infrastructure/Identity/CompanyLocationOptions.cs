namespace internalEmployee.Auth;

public sealed class CompanyLocationOptions
{
    public string Name { get; set; } = "Company";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; } = 100;
}

