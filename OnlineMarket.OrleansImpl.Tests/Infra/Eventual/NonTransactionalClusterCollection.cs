namespace OnlineMarket.OrleansImpl.Tests.Infra.Eventual;

[CollectionDefinition(Name)]
public class NonTransactionalClusterCollection : ICollectionFixture<NonTransactionalClusterFixture>
{
    public const string Name = "NonTransactionalClusterCollection";
}

