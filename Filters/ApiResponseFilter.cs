using AlumniManagementApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;
using System.Threading.Tasks;

namespace AlumniManagementApi.Filters
{
    public class ApiResponseFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult objectResult)
            {
                // If it is already an ApiResponse, do not wrap it again
                var valueType = objectResult.Value?.GetType();
                if (valueType != null && valueType.IsGenericType &&
                    valueType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
                {
                    await next();
                    return;
                }

                var statusCode = objectResult.StatusCode ?? 200;
                var isSuccess = statusCode >= 200 && statusCode < 300;

                string? message = isSuccess ? "Request completed successfully." : "An error occurred.";
                object? data = isSuccess ? objectResult.Value : null;
                object? errors = isSuccess ? null : objectResult.Value;

                if (objectResult.Value != null)
                {
                    var type = objectResult.Value.GetType();
                    var messageProp = type.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (messageProp != null)
                    {
                        var val = messageProp.GetValue(objectResult.Value)?.ToString();
                        if (!string.IsNullOrEmpty(val))
                        {
                            message = val;
                        }
                    }
                }

                var apiResponse = new ApiResponse<object>
                {
                    Success = isSuccess,
                    StatusCode = statusCode,
                    Message = message,
                    Data = data,
                    Errors = errors
                };

                objectResult.Value = apiResponse;
            }
            else if (context.Result is StatusCodeResult statusCodeResult)
            {
                var statusCode = statusCodeResult.StatusCode;
                var isSuccess = statusCode >= 200 && statusCode < 300;

                var apiResponse = new ApiResponse<object>
                {
                    Success = isSuccess,
                    StatusCode = statusCode,
                    Message = isSuccess ? "Request completed successfully." : "An error occurred.",
                    Data = null,
                    Errors = null
                };

                context.Result = new ObjectResult(apiResponse)
                {
                    StatusCode = statusCode
                };
            }

            await next();
        }
    }
}
