namespace OnlineMarket.Core.Ports;

//日志
public interface IAuditLog
{
    Task WriteAsync(string category, string key, string json);
}