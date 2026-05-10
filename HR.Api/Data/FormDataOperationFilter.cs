using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace internalEmployee.Data;

public class FormDataOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        try
        {
            var methodInfo = context.MethodInfo;
            var isUpdateMyProfile = methodInfo.Name == "UpdateMyProfile" && methodInfo.DeclaringType?.Name == "AuthController";
            var isUpdateEmployee = methodInfo.Name == "UpdateEmployee" && methodInfo.DeclaringType?.Name == "AuthController";
            var isImportEmployees = methodInfo.Name == "ImportEmployeesFromExcel" && methodInfo.DeclaringType?.Name == "AuthController";

            if (isUpdateMyProfile)
            {
                operation.RequestBody ??= new OpenApiRequestBody();
                operation.RequestBody.Content ??= new Dictionary<string, OpenApiMediaType>();

                operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["Email"] = new OpenApiSchema { Type = "string", Nullable = true },
                            ["FullName"] = new OpenApiSchema { Type = "string", Nullable = true },
                            ["PhoneNumber"] = new OpenApiSchema { Type = "string", Nullable = true },
                            ["DepartmentId"] = new OpenApiSchema { Type = "integer", Format = "int32", Nullable = true },
                            ["JobTitle"] = new OpenApiSchema { Type = "string", Nullable = true },
                            ["StartDate"] = new OpenApiSchema { Type = "string", Format = "date", Nullable = true, Description = "Date in format: YYYY-MM-DD" },
                            ["HasCustody"] = new OpenApiSchema { Type = "boolean", Nullable = true },
                            ["CustodyDetails"] = new OpenApiSchema
                            {
                                Type = "array",
                                Items = new OpenApiSchema { Type = "string" },
                                Nullable = true,
                                Description = "Array of custody detail strings"
                            },
                            ["CompanyPhoneNumber"] = new OpenApiSchema { Type = "string", Nullable = true },
                            ["CompanyEmail"] = new OpenApiSchema { Type = "string", Nullable = true },
                            ["image"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Nullable = true,
                                Description = "Profile image file"
                            }
                        }
                    }
                };
            }
            else if (isUpdateEmployee)
            {
                if (operation.RequestBody?.Content != null &&
                    operation.RequestBody.Content.ContainsKey("multipart/form-data"))
                {
                    var formData = operation.RequestBody.Content["multipart/form-data"];
                    if (formData?.Schema?.Properties != null)
                    {
                        foreach (var property in formData.Schema.Properties)
                        {
                            if (property.Value.Type == "string" && string.IsNullOrEmpty(property.Value.Example?.ToString()))
                            {
                                property.Value.Example = null;
                                property.Value.Default = null;
                            }
                        }
                    }
                }
            }
            else if (isImportEmployees)
            {
                operation.RequestBody ??= new OpenApiRequestBody();
                operation.RequestBody.Content ??= new Dictionary<string, OpenApiMediaType>();

                operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Description = "Excel file"
                            }
                        },
                        Required = new HashSet<string> { "file" }
                    }
                };
            }
        }
        catch
        {
        }
    }
}
