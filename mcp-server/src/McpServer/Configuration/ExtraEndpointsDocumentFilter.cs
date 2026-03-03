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
                        Establishes a WebSocket connection keyed to a Nuzlocke run.
                        One connection per nuzlocke — any previous connection for the same ID is replaced.

                        **Pre-requisite:** The nuzlocke must exist (created via `POST /nuzlocke/sessions`).

                        **Auto-initialization:** If the game state has never been set up, the server
                        initializes it automatically using the generation and locke type stored in the session.

                        **On connect, the server immediately sends a `connected` message:**
                        ```json
                        {
                          "type": "connected",
                          "nuzlockeId": "uuid",
                          "initialized": false,
                          "generation": 1,
                          "lockeType": "standard"
                        }
                        ```
                        `initialized: true` means the game state was auto-initialized in this connection.

                        **Subsequent server-push messages:**

                        | type | When |
                        |---|---|
                        | `workflow_event` | After any `POST /nuzlocke/workflow` completes |
                        | `advice_start` | LLM advice generation begins |
                        | `advice_chunk` | Streaming token from the LLM |
                        | `advice_end` | Full advice text ready |
                        | `advice_error` | LLM call failed |

                        **Flow:**
                        1. `POST /nuzlocke/sessions` → get `nuzlockeId`
                        2. `GET ws://host/ws/advice?nuzlockeId=<id>` → receive `connected`
                        3. `POST /nuzlocke/workflow` → receive `workflow_event` + async advice stream
                        """,
                    Parameters = new List<OpenApiParameter>
                    {
                        new()
                        {
                            Name = "nuzlockeId",
                            In = ParameterLocation.Query,
                            Required = true,
                            Schema = new OpenApiSchema { Type = "string" },
                            Description = "The Nuzlocke session ID obtained from POST /nuzlocke/sessions."
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["101"] = new OpenApiResponse
                        {
                            Description = "Switching Protocols — WebSocket connection established. Server sends `connected` message immediately."
                        },
                        ["400"] = new OpenApiResponse
                        {
                            Description = "Bad request — not a WebSocket upgrade request, or missing `nuzlockeId`."
                        },
                        ["404"] = new OpenApiResponse
                        {
                            Description = "Nuzlocke not found — create it first via POST /nuzlocke/sessions."
                        },
                        ["500"] = new OpenApiResponse
                        {
                            Description = "Failed to load or initialize the nuzlocke game state."
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
