using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;


namespace UISupportBlazor
{
    /// <summary>
    /// Middleware to handle HTTP POST requests to a configurable endpoint.
    /// It processes the request body, executes a command using the provided API command set,
    /// and returns a JSON response.
    /// </summary>
    public class ApiMiddleware
    {
        // Delegate to invoke the next middleware in the pipeline.
        private readonly RequestDelegate _next;

        // The type containing the set of API commands to execute.
        private Type _apiCommandSet;

        // And the RSA ApiPubblicKey field of _apiCommandSet if set
        private string? _apiPubblicKey;

        // The API endpoint path.
        private readonly string _apiPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="apiCommandSet">The type containing the API commands to execute.</param>
        /// <param name="apiPath">The path where the middleware should intercept POST requests.</param>
        /// <param name="apiPubblicKey"></param>
        public ApiMiddleware(RequestDelegate next, Type apiCommandSet, string? apiPubblicKey = null, string? apiPath = "/api")
        {
            if (apiPath?.StartsWith("/") != true)
                apiPath = "/" + apiPath; // Ensure the path starts with a "/".
            _apiCommandSet = apiCommandSet;
            _apiPubblicKey = String.IsNullOrEmpty(apiPubblicKey) ? null : apiPubblicKey;
            _apiPubblicKey ??=  apiCommandSet.GetField("ApiPubblicKey", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string; _next = next;
            _apiPath = apiPath ?? "/api"; // Ensure default path if none is provided.
        }

        /// <summary>
        /// Middleware logic to handle incoming HTTP requests.
        /// If the request is a POST to the configured API endpoint, it processes the request body,
        /// executes the corresponding API command, and writes the response.
        /// Otherwise, it passes the request to the next middleware in the pipeline.
        /// </summary>
        /// <param name="context">The HTTP context of the current request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the request is a POST to the configured endpoint.
            if (context.Request.Path.StartsWithSegments(_apiPath))
            {
                if (context.Request.Method == HttpMethods.Post)
                {


                    var segments = context.Request.Path.Value.Split('/');
                    var methodName = segments.Length > 2 ? segments[2] : null;

                    // Read the request body as a string.
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    using JsonDocument doc = JsonDocument.Parse(requestBody);
                    var methodInfo = UISupportGeneric.API.GetMethodInfo(doc, ref methodName, _apiCommandSet);
                    if (!String.IsNullOrEmpty(_apiPubblicKey) && methodInfo?.GetCustomAttribute<IsPubblicAPIAttribute>() == null)
                    {
                        // Validate the digital signature in the request header
                        if (!context.Request.Headers.TryGetValue("X-Signature", out var signatureHeader) || string.IsNullOrEmpty(signatureHeader))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync("{\"error\": \"Missing digital signature\"}");
                            return;
                        }

                        try
                        {
                            // Convert signature from Base64
                            var signature = Convert.FromBase64String(signatureHeader!);

                            // Convert request body to bytes
                            var data = System.Text.Encoding.UTF8.GetBytes(requestBody);

                            // Import RSA public key and verify signature
                            using var rsa = System.Security.Cryptography.RSA.Create();
                            rsa.ImportFromPem(_apiPubblicKey);

                            bool isValid = rsa.VerifyData(data, signature,
                                System.Security.Cryptography.HashAlgorithmName.SHA256,
                                System.Security.Cryptography.RSASignaturePadding.Pkcs1);

                            if (!isValid)
                            {
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                await context.Response.WriteAsync("{\"error\": \"Invalid digital signature\"}");
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync($"{{\"error\": \"Signature verification failed: {ex.Message}\"}}");
                            return;
                        }

                        // Validate timestamp to prevent replay attacks
                        if (!doc.RootElement.TryGetProperty("timestamp", out var timestampElement))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync("{\"error\": \"Missing timestamp in request\"}");
                            return;
                        }

                        long requestTimestamp;
                        try
                        {
                            requestTimestamp = timestampElement.GetInt64();
                        }
                        catch
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync("{\"error\": \"Invalid timestamp format\"}");
                            return;
                        }

                        var serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var timeDifference = Math.Abs(serverTimestamp - requestTimestamp);
                        const int maxTimeDifferenceSeconds = 300; // 5 minutes tolerance

                        if (timeDifference > maxTimeDifferenceSeconds)
                        {
                            var errorMessage = requestTimestamp > serverTimestamp
                                ? $"{{\"error\": \"Request timestamp is in the future. Check client system clock. Time difference: {timeDifference} seconds\"}}"
                                : $"{{\"error\": \"Request timestamp is too old (possible replay attack). Time difference: {timeDifference} seconds. Max allowed: {maxTimeDifferenceSeconds} seconds\"}}";
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync(errorMessage);
                            return;
                        }
                    }
                    // Set the response content type to JSON.
                    context.Response.ContentType = "application/json";

                    // Execute the API command and set the response status code and body.
                    context.Response.StatusCode = (int)UISupportGeneric.API.Post(_apiCommandSet, methodName, requestBody, out string response);
                    await context.Response.WriteAsync(response);
                    return; // End the middleware pipeline for this request.
                }
                else if (context.Request.Method == HttpMethods.Get)
                {
                    // Set the response content type text/plain for c# code.
                    context.Response.ContentType = "text/plain";

                    context.Response.StatusCode = (int)UISupportGeneric.API.Get(_apiCommandSet, out string response);
                    await context.Response.WriteAsync(response);
                    return; // End the middleware pipeline for this request.
                }
            }
            try
            {
                // If the request does not match, pass it to the next middleware.
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync($"{{\"error\": \"Bad Request: {ex.Message}\"}}");
            }
        }
    }
}