// ============================================================
// SECURITY: Security Headers Middleware
// Adds defensive HTTP headers to every response to protect against:
//   - Clickjacking (X-Frame-Options)
//   - MIME sniffing attacks (X-Content-Type-Options)
//   - XSS attacks (X-XSS-Protection, Content-Security-Policy)
//   - Information leakage (Referrer-Policy)
//   - Excess browser permissions (Permissions-Policy)
// ============================================================
namespace Project_DEPI_.Middleware
{
    /// <summary>
    /// Middleware that injects security-hardening HTTP response headers on every request.
    /// Registered in Program.cs via app.UseSecurityHeaders().
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // SECURITY: Prevent browsers from MIME-sniffing a response away from the declared Content-Type.
            headers["X-Content-Type-Options"] = "nosniff";

            // SECURITY: Deny rendering the page inside a frame to prevent Clickjacking attacks.
            headers["X-Frame-Options"] = "SAMEORIGIN";

            // SECURITY: Enable the browser's built-in XSS filter and block the page if an XSS attack is detected.
            headers["X-XSS-Protection"] = "1; mode=block";

            // SECURITY: Control how much referrer information is included with requests.
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // SECURITY: Disable access to sensitive browser APIs that the application does not use.
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";

            // SECURITY: Content Security Policy - restricts the sources from which the browser can load resources.
            // This prevents XSS by whitelisting trusted sources only.
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://js.stripe.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.tailwindcss.com; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com data:; " +
                "img-src 'self' data: https:; " +
                "frame-src https://js.stripe.com; " +
                "connect-src 'self' https://api.stripe.com;";

            // SECURITY: Remove the Server header to prevent server version disclosure.
            headers.Remove("Server");

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method for clean registration of SecurityHeadersMiddleware in Program.cs.
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
