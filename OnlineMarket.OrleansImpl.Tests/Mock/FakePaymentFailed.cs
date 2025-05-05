using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using System.Collections.Generic;

namespace OnlineMarket.OrleansImpl.Tests.Mock
{
    public static class FakePaymentFailed
    {
        public static PaymentFailed Create(int customerId)
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

            return new PaymentFailed(
                status: "FAILED",
                customer: fakeCheckout,
                orderId: 12345,
                items: new List<OrderItem>(),
                totalAmount: 100.0f,
                instanceId: fakeCheckout.instanceId
            );
        }
    }
}