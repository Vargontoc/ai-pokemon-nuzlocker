using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace es.vargontoc.nuzlocke.ai.Configuration;

/// <summary>
/// Manually adds non-controller endpoints to the Swagger document:
/// - WebSocket: GET /ws/advice
/// - Health checks: GET /health, /health/ready, /health/live
/// </summary>
public class ExtraEndpointsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        AddWebSocketEndpoint(swaggerDoc);
        AddHealthEndpoints(swaggerDoc);
    }

    private static void AddWebSocketEndpoint(OpenApiDocument doc)
    {
        doc.Paths.Add("/ws/advice", new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new() { Name = "WebSocket" } },
                    Summary = "Connect to real-time advice stream (WebSocket)",
                    Description = """
                        Establishes a WebSocket connection to receive AI advice pushed asynchronously
                        after calling `POST /nuzlocke/advice`.

                        **Protocol:** Send the HTTP `Upgrade: websocket` header. The server will push
                        JSON messages whenever advice for the given `sessionId` is ready.

                        **Message format received from server:**
                        ```json
                        {
                          "correlationId": "abc123",
                          "sessionId": "my-session-id",
                          "advice": "Your strategic advice text here..."
                        }
                        ```

                        **Flow:**
                        1. Connect to `ws://host/ws/advice?sessionId=<id>`
                        2. Call `POST /nuzlocke/advice` with the same `sessionId`
                        3. Receive the advice message on the WebSocket when LLM finishes
                        """,
                    Parameters = new List<OpenApiParameter>
                    {
                        new()
                        {
                            Name = "sessionId",
                            In = ParameterLocation.Query,
                            Required = true,
                            Schema = new OpenApiSchema { Type = "string" },
                            Description = "The Nuzlocke session ID. Must match the sessionId used in POST /nuzlocke/advice."
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["101"] = new OpenApiResponse
                        {
                            Description = "Switching Protocols — WebSocket connection established."
                        },
                        ["400"] = new OpenApiResponse
                        {
                            Description = "Bad request — not a WebSocket upgrade request, or missing/empty `sessionId`."
                        }
                    }
                }
            }
        });
    }

    private static void AddHealthEndpoints(OpenApiDocument doc)
    {
        var healthTag = new List<OpenApiTag> { new() { Name = "Health" } };
        var healthResponse200 = new OpenApiResponse
        {
            Description = "Health status JSON",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["status"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("Healthy") },
                            ["checks"] = new() { Type = "array" },
                            ["totalDuration"] = new() { Type = "number" }
                        }
                    }
                }
            }
        };

        doc.Paths.Add("/health", new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = healthTag,
                    Summary = "Full health report (all checks)",
                    Description = "Returns status of all registered health checks: SQLite database and PokeAPI reachability.",
                    Responses = new OpenApiResponses { ["200"] = healthResponse200 }
                }
            }
        });

        doc.Paths.Add("/health/ready", new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = healthTag,
                    Summary = "Readiness check (db + external deps)",
                    Description = "Checks only `ready`-tagged checks: database and PokeAPI. Use for Kubernetes readiness probes.",
                    Responses = new OpenApiResponses { ["200"] = healthResponse200 }
                }
            }
        });

        doc.Paths.Add("/health/live", new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = healthTag,
                    Summary = "Liveness check (process alive)",
                    Description = "Always returns Healthy if the process is running. No external checks. Use for Kubernetes liveness probes.",
                    Responses = new OpenApiResponses { ["200"] = healthResponse200 }
                }
            }
        });
    }
}
