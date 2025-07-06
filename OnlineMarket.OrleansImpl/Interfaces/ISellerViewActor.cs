

namespace OnlineMarket.OrleansImpl.Interfaces;



// "Seller View" Grain interface. Currently completely equivalent to ISellerActor,
// If additional read-only queries are required, they can be added to this interface later.
    public interface ISellerViewActor : ISellerActor
    {
        // No new members are added now, pure inheritance is enough.
    }
