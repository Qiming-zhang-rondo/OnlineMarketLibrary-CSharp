namespace OnlineMarket.OrleansImpl.Tests.Infra.Eventual;

[CollectionDefinition(Name)]
public class NonTransactionalClusterCollectionWithDb : ICollectionFixture<NonTransactionalClusterFixtureWithDb>
{
    public const string Name = "NonTransactionalClusterCollectionWithDb";
}