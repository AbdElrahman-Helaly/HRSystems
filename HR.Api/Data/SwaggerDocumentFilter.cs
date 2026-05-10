using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace internalEmployee.Data;

public class SwaggerDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        try
        {
            if (swaggerDoc.Components?.Schemas != null)
            {
                var keysToRemove = swaggerDoc.Components.Schemas.Keys
                    .Where(k => k.Contains("UpdateProfileRequest"))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    swaggerDoc.Components.Schemas.Remove(key);
                }
            }
        }
        catch
        {
        }
    }
}
