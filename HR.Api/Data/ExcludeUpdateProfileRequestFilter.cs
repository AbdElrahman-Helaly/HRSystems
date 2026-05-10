using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace internalEmployee.Data;

public class ExcludeUpdateProfileRequestFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.Name == "UpdateProfileRequest" ||
            context.Type.FullName?.Contains("UpdateProfileRequest") == true)
        {
            schema.Type = "object";
            schema.Properties = new Dictionary<string, OpenApiSchema>
            {
                ["Email"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["FullName"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["PhoneNumber"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["DepartmentId"] = new OpenApiSchema { Type = "integer", Format = "int32", Nullable = true },
                ["JobTitle"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["StartDate"] = new OpenApiSchema { Type = "string", Format = "date", Nullable = true },
                ["HasCustody"] = new OpenApiSchema { Type = "boolean", Nullable = true },
                ["CustodyDetails"] = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema { Type = "string" },
                    Nullable = true
                },
                ["CompanyPhoneNumber"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["CompanyEmail"] = new OpenApiSchema { Type = "string", Nullable = true }
            };
            schema.AdditionalProperties = null;
            schema.AllOf = null;
            schema.AnyOf = null;
            schema.OneOf = null;
            schema.Required = null;
        }
    }
}
