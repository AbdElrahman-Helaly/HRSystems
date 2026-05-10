namespace internalEmployee.Auth;

public sealed class SmsOptions
{
    public string OtpTemplate { get; set; } = "MCI OTP is {otp}, valid for 3 minutes";
    public string ApiUrl { get; set; } = "https://bulk.whysms.com/api/http/sms/send";
    public string ApiToken { get; set; } = string.Empty;
    public string SenderId { get; set; } = "Mediconsult";
}
