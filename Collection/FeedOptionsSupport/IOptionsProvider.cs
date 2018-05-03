namespace CosmosDbContext.Collection.FeedOptionsSupport
{
    public interface IOptionProvider
    {
        CosmosDbQueryOptions Options { get; }
    }
}