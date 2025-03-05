using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Middleware
{
    public class YarpDestinationLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public YarpDestinationLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Items.ContainsKey("YARP.DestinationAddress"))
            {
                var destinationAddress = context.Items["YARP.DestinationAddress"];
                Console.WriteLine($"Request is being forwarded to: {destinationAddress}");
            }
            else
            {
                Console.WriteLine("Request is NOT being forwarded by YARP.");
            }

            await _next(context);
        }
    }
}
