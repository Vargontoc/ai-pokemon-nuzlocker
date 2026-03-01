using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace es.vargontoc.nuzlocke.ai.Configuration;

/// <summary>
/// Removes [FromServices] parameters from Swagger operation descriptions.
/// These are DI-injected services and are not API parameters.
/// </summary>
public class FromServicesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fromServicesParams = context.MethodInfo
            .GetParameters()
            .Where(p => p.GetCustomAttributes(typeof(FromServicesAttribute), false).Length > 0)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (fromServicesParams.Count == 0) return;

        var toRemove = operation.Parameters
            .Where(p => fromServicesParams.Contains(p.Name))
            .ToList();

        foreach (var param in toRemove)
            operation.Parameters.Remove(param);
    }
}
