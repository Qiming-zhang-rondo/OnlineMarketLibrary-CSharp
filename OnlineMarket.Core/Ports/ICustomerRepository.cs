// ICustomerRepository.cs
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports;

public interface ICustomerRepository
{
    Task SaveAsync(Customer c);           // Up‑sert
    Task<Customer> LoadAsync();           // 永远返回非 null（第一次会建空对象）
    Task ClearAsync();
}