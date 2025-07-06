// ICustomerRepository.cs
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports;

public interface ICustomerRepository
{
    Task SaveAsync(Customer c);         
    //Always returns non-null (an empty object will be created the first time)
    Task<Customer> LoadAsync();           
    Task ClearAsync();
}