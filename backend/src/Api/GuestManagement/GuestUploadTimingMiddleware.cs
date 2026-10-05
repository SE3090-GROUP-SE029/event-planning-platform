using System.Diagnostics;

namespace Api.GuestManagement;

public class GuestUploadTimingMiddleware(
    RequestDelegate next,
    ILogger<GuestUploadTimingMiddleware> logger)
{
    public const string RequestStartedAtKey = "GuestUpload.RequestStartedAt";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = context.Request.Path.Value;
        if (!HttpMethods.IsPost(context.Request.Method) || requestPath is null ||
            !requestPath.EndsWith("/registration-form/upload", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var startedAt = Stopwatch.GetTimestamp();
        context.Items[RequestStartedAtKey] = startedAt;
        logger.LogInformation(
            "Guest upload HTTP request received. TraceId={TraceId}, ContentLength={ContentLength}",
            context.TraceIdentifier,
            context.Request.ContentLength);

        try
        {
            await next(context);
        }
        finally
        {
            logger.LogInformation(
                "Guest upload HTTP response returned. TraceId={TraceId}, StatusCode={StatusCode}, TotalElapsedMs={ElapsedMs}",
                context.TraceIdentifier,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }
}
