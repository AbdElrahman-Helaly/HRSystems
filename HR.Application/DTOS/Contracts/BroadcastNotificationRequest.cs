using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class BroadcastNotificationRequest
{
    [Required(ErrorMessage = "نوع المستهدف مطلوب")]
    public NotificationTargetType TargetType { get; set; }

    public Guid? TargetUserId { get; set; }

    public int? TargetDepartmentId { get; set; }

    [Required(ErrorMessage = "عنوان الإشعار مطلوب")]
    [MaxLength(100, ErrorMessage = "يجب ألا يتجاوز العنوان 100 حرف")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "محتوى الإشعار مطلوب")]
    [MaxLength(500, ErrorMessage = "يجب ألا يتجاوز المحتوى 500 حرف")]
    public string Message { get; set; } = string.Empty;
}

public enum NotificationTargetType
{
    All = 1,
    Admins = 2,
    Employees = 3,
    SpecificUser = 4,
    SpecificDepartment = 5
}
