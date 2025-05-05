using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using System.Collections.Generic;

namespace Test.Infra.Mocks
{
    public static class FakePaymentConfirmed
    {
        public static PaymentConfirmed Create(int customerId)
        {
            var fakeCheckout = new CustomerCheckout
            {
                CustomerId = customerId,
                FirstName = "Test",
                LastName = "User",
                Street = "Test Street",
                Complement = "",
                City = "Test City",
                State = "Test State",
                ZipCode = "00000",
                PaymentType = "CREDIT_CARD",
                CardNumber = "1234567890",
                CardHolderName = "Test User",
                CardExpiration = "12/24",
                CardSecurityNumber = "123",
                CardBrand = "VISA",
                Installments = 1,
                instanceId = customerId.ToString()
            };

            return new PaymentConfirmed(
                customer: fakeCheckout,
                orderId: 12345,
                totalAmount: 100.0f,
                items: new List<OrderItem>(),
                date: DateTime.UtcNow,
                instanceId: fakeCheckout.instanceId
            );
        }
    }
}