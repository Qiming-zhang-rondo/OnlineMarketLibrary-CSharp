// // OnlineMarket.OrleansImpl.Tests/Fakes/DummyOrderActor.cs
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using OnlineMarket.OrleansImpl.Interfaces;           // IOrderActor
// using OnlineMarket.Core.Common.Entities;
// using OnlineMarket.Core.Common.Events;
// using Orleans;
//
// namespace OnlineMarket.OrleansImpl.Tests.Fakes;
//
// /// <summary>
// /// 不做任何持久化/逻辑的空实现，
// // — 只为让 ShipmentActor 的 OrderNotifier 有地方可投递。
// /// </summary>
// public sealed class DummyOrderActor : Grain, IOrderActor
// {
//     public Task Checkout(ReserveStock _)                       => Task.CompletedTask;
//     public Task ProcessPaymentConfirmed(PaymentConfirmed _)    => Task.CompletedTask;
//     public Task ProcessPaymentFailed(PaymentFailed _)          => Task.CompletedTask;
//     public Task ProcessShipmentNotification(ShipmentNotification _) => Task.CompletedTask;
//
//     public Task<List<Order>> GetOrders()   => Task.FromResult(new List<Order>());
//     public Task<int>        GetNumOrders() => Task.FromResult(0);
//     public Task Reset()                    => Task.CompletedTask;
// }