using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class YonetimSozlesmesi : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<YonetimAttribute>().Any()) return;
        operation.Security = [new OpenApiSecurityRequirement {
            [new OpenApiSecuritySchemeReference("Yonetim",context.Document)] = []
        }];
        operation.Responses!["401"] = new OpenApiResponse { Description = "Yönetim yetkisi gerekiyor." };
    }
}

