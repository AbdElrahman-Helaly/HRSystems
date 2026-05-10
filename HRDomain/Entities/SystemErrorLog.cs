namespace internalEmployee.Data.Entities;

public class SystemErrorLog
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? Path { get; set; }
    public string? Method { get; set; }
    public string? QueryString { get; set; }
    public int? StatusCode { get; set; }

    public string? Message { get; set; }
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }

    public string? TraceId { get; set; }
    public string? RemoteIp { get; set; }

    public Guid? UserId { get; set; }
}
