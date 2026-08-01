using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace CashFlow.Infrastructure.Services;

public interface IAuditService
{
    Task LogAsync(
        int userId,
        string action,
        string entity,
        int? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        string? reference = null,
        int? storeId = null,
        int? organizationId = null,
        HttpRequest? request = null);

    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        int organizationId,
        int? storeId = null,
        string? action = null,
        string? entity = null,
        int? userId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? searchTerm = null);
}

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(
        int userId,
        string action,
        string entity,
        int? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        string? reference = null,
        int? storeId = null,
        int? organizationId = null,
        HttpRequest? request = null)
    {
        try
        {
            var audit = new AuditLog
            {
                UserId = userId,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                Description = description,
                Reference = reference,
                StoreId = storeId,
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            if (request != null)
            {
                audit.IpAddress = GetClientIpAddress(request);
                audit.UserAgent = request.Headers["User-Agent"].ToString();
            }

            await _unitOfWork.AuditLogs.AddAsync(audit);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            // Swallow exceptions - audit should not break the application
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        int organizationId,
        int? storeId = null,
        string? action = null,
        string? entity = null,
        int? userId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? searchTerm = null)
    {
        var logs = await _unitOfWork.AuditLogs
            .FindAsync(a => a.OrganizationId == organizationId);

        if (storeId.HasValue)
        {
            logs = logs.Where(a => a.StoreId == storeId.Value || a.StoreId == null);
        }

        if (!string.IsNullOrEmpty(action))
        {
            logs = logs.Where(a => a.Action == action);
        }

        if (!string.IsNullOrEmpty(entity))
        {
            logs = logs.Where(a => a.Entity == entity);
        }

        if (userId.HasValue)
        {
            logs = logs.Where(a => a.UserId == userId.Value);
        }

        if (dateFrom.HasValue)
        {
            logs = logs.Where(a => a.Timestamp >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            logs = logs.Where(a => a.Timestamp <= dateTo.Value.AddDays(1));
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            logs = logs.Where(a =>
                (a.Description != null && a.Description.ToLower().Contains(searchTerm)) ||
                (a.Entity != null && a.Entity.ToLower().Contains(searchTerm)) ||
                (a.Action != null && a.Action.ToLower().Contains(searchTerm)) ||
                (a.Reference != null && a.Reference.ToLower().Contains(searchTerm)));
        }

        return logs.OrderByDescending(a => a.Timestamp);
    }

    private string GetClientIpAddress(HttpRequest request)
    {
        // Check for forwarded IP (when behind proxy/load balancer)
        var forwardedHeader = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHeader))
        {
            return forwardedHeader.Split(',').First().Trim();
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}