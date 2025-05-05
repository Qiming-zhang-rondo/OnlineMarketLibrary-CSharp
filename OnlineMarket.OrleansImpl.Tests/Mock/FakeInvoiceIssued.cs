using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Entities;
using System;
using System.Collections.Generic;

namespace OnlineMarket.OrleansImpl.Tests.Infra.Mocks
{
    public static class FakeInvoiceIssued
    {
        public static InvoiceIssued Create(int customerId, int orderId)
        {
            var checkout = new CustomerCheckout
            {
                CustomerId = customerId,
                PaymentType = PaymentType.CREDIT_CARD.ToString(),
                Installments = 1,
                CardNumber = "4111111111111111",
                CardHolderName = "Test User",
                CardExpiration = "1225",
                CardSecurityNumber = "123",
                CardBrand = "VISA",
                instanceId = Guid.NewGuid().ToString()
            };

            var items = new List<OrderItem>
            {
                new OrderItem
                {
                    product_id = 1,
                    seller_id = 1,
                    quantity = 1,
                    voucher = 0
                }
            };

            return new InvoiceIssued(
                customer: checkout,
                orderId: orderId,
                invoiceNumber: $"INV-{orderId}",
                issueDate: DateTime.UtcNow,
                totalInvoice: 100.0f,
                items: items,
                instanceId: checkout.instanceId
            );
        }
    }
}