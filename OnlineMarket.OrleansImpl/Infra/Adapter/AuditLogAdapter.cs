namespace OnlineMarket.OrleansImpl.Infra.Adapter;

using OnlineMarket.Core.Ports;
             // Reuse IAuditLogger

             public sealed class AuditLogAdapter : IAuditLog
             {
                 private readonly IAuditLogger _logger;      
                 public AuditLogAdapter(IAuditLogger logger) => _logger = logger;

                 public Task WriteAsync(string category, string key, string json)
                     => _logger.Log(category, key, json);    
             }