using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncAPI.Classes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class BasicAuthentication
    {
        private readonly RequestDelegate _next;
        public string KeyInfo = "dc:authorizedSecretKey";

        public BasicAuthentication(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            string authHeader = httpContext.Request.Headers["Authorization"];
            if(authHeader != null && authHeader.StartsWith("Basic"))
            {
                string encodeInfo = authHeader.Substring("Basic ".Length).Trim();
                Encoding encoding = Encoding.GetEncoding("UTF-8");
                encodeInfo = encoding.GetString(Convert.FromBase64String(encodeInfo));
                int index = encodeInfo.IndexOf(':');
                var username = encodeInfo.Substring(0, index);
                var pass = encodeInfo.Substring(index + 1);
                if(username.Equals("dc") && pass.Equals("authorizedSecretKey"))
                {
                    await _next.Invoke(httpContext);
                }
                httpContext.Response.StatusCode = 401;
                return;
            }
            else
            {
                httpContext.Response.StatusCode = 401;
                return;
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class BasicAuthenticationExtensions
    {
        public static IApplicationBuilder UseBasicAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BasicAuthentication>();
        }
    }
}
