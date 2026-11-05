using ApiRestProject.Application.Dtos;
using ApiRestProject.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiRestProject.Api;

/// <summary>Traduce excepciones de dominio/aplicación a respuestas HTTP (ProblemDetails).</summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                DomainException => (StatusCodes.Status400BadRequest, "Regla de negocio incumplida"),
                UnauthorizedException => (StatusCodes.Status401Unauthorized, "No autorizado"),
                NotFoundException => (StatusCodes.Status404NotFound, "No encontrado"),
                DbUpdateException => (StatusCodes.Status409Conflict, "Conflicto con datos existentes (¿duplicado o registros relacionados?)"),
                _ => (StatusCodes.Status500InternalServerError, "Error interno")
            };

            if (status == 500) logger.LogError(ex, "Error no controlado");

            ctx.Response.StatusCode = status;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == 500 ? null : ex is DbUpdateException ? null : ex.Message
            });
        }
    }
}
