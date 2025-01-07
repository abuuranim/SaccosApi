//namespace SaccosApi.Services
//{
//    public class CustomUnauthorizedMiddleware
//    {
//        private readonly RequestDelegate _next;

//        public CustomUnauthorizedMiddleware(RequestDelegate next)
//        {
//            _next = next;
//        }

//        public async Task InvokeAsync(HttpContext context)
//        {
//            await _next(context);

//            if (context.Response.StatusCode == 204)
//            {
//                context.Response.ContentType = "application/json";
//                var response = new
//                {
//                    StatusCode = 401, //context.Response.StatusCode,
//                    Message = "Custom 401 Unauthorized: Access is denied due to invalid credentials."
//                };
//                await context.Response.WriteAsJsonAsync(response);
//            }
//        }
//    }
// }
