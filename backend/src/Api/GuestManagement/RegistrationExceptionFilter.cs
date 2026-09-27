using Application.GuestManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.GuestManagement;

public class RegistrationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not RegistrationException exception) return;
        context.Result = new ObjectResult(new { code = exception.Code, error = exception.Message }) { StatusCode = exception.StatusCode };
        context.ExceptionHandled = true;
    }
}
