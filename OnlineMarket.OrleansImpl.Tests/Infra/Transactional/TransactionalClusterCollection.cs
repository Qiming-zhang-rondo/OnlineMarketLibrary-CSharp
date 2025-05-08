using OnlineMarket.OrleansImpl.Tests.Infra.Transactional;

namespace Test.Infra.Transactional;

[CollectionDefinition(Name)]
public class TransactionalClusterCollection : ICollectionFixture<TransactionalClusterFixture>
{
    public const string Name = "TransactionalClusterCollection";
}

