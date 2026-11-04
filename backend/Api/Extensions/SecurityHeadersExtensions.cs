namespace Api.Extensions;

public static class SecurityHeadersExtensions
{
    private const string HstsValue = "max-age=31536000"; // one year; no includeSubDomains: the host owns the parent domain

    /// <summary>
    /// Response headers for a JSON-only API: nothing it returns should ever be rendered, framed, sniffed or cached.
    /// Everything is set in OnStarting so it also lands on error responses: the exception handler clears headers set
    /// before an exception, which is why HSTS is set here rather than with UseHsts().
    /// The Angular frontend gets its own CSP wherever it is served; this one is deliberately as strict as possible.
    /// </summary>
    /// <param name="sendHsts">False in Development, where the app runs on plain http://localhost.</param>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool sendHsts) =>
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                // Swagger UI (development only) is an HTML page with scripts, so it can't live under the API's CSP.
                if (!context.Request.Path.StartsWithSegments("/api/swagger"))
                {
                    headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
                    // Responses carry tokens (login) and live data; no browser or proxy cache should keep them.
                    headers.CacheControl = "no-store";
                }
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                // IsHttps reflects X-Forwarded-Proto behind Render's proxy (ASPNETCORE_FORWARDEDHEADERS_ENABLED).
                if (sendHsts && context.Request.IsHttps)
                    headers.StrictTransportSecurity = HstsValue;
                return Task.CompletedTask;
            });
            return next(context);
        });
}
