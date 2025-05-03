namespace OnlineMarket.OrleansImpl.Infra.Adapter;

using OnlineMarket.Core.Ports;
             // 复用你原来的 IAuditLogger

public sealed class AuditLogAdapter : IAuditLog
{
    private readonly IAuditLogger _logger;      // 依然可以是 PostgresAuditLogger
    public AuditLogAdapter(IAuditLogger logger) => _logger = logger;

    public Task WriteAsync(string category, string key, string json)
        => _logger.Log(category, key, json);    // tableName 用默认值“log”
}