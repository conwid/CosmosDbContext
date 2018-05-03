using CosmosDbContext.Collection.FeedOptionsSupport;
using System.Collections.Generic;

namespace CosmosDbContext.Collection
{
    // This interface is to support the scenario where a DocumentDbCollection<dynamic> would be used
    // DocumentDbCollection<dynamic> cannot be used, because it is an IQueryable, and composing any additional LInQ expression is not allowed based on IQueryable<dynamic>
    // This means that in untyped scenarios the collection must be evaluated on the server side 
    // Using a simple IEnumerable would work, but in that case, the FeedOptions and SQL support would be lost
    // This gives us the IEnumerable with the additional features
    // And it's probably not bad to have an interface for this anyway; later on if any additional CosmosDbQueryables are supported, an interface for each can be used to declare the special methods
    public interface ICosmosDbCollection<T> : IEnumerable<T>, IOptionProvider
    {
        IEnumerable<T2> ExecuteSql<T2>(string sql);
    }    
}