using CosmosDbContext.Collection.LinqProvider;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDbContext.Collection
{
    public class CosmosDbCollection<T> : CosmosDbQueryable<T>, ICosmosDbCollection<T>
    {
        public CosmosDbCollection(string database, string collectionName, DocumentClient client)
            : base(UriFactory.CreateDocumentCollectionUri(database, collectionName), client, new CosmosDbCollectionQueryProvider(database, collectionName, client))
        {
        }
        internal CosmosDbCollection(IQueryProvider provider, Expression expression) : base(provider, expression)
        {
        }
        public IEnumerable<T2> ExecuteSql<T2>(string sql)
        {
            return string.IsNullOrWhiteSpace(sql)
                ? throw new ArgumentException("Provided sql string is empty")
                : client.CreateDocumentQuery<T2>(uri, sql).ToList();

            // NOTE: Currently, it is not supported to add Linq standard query operators to existing queries built from sql string
            // See: "AsSQl is not supported"
            // This means that currently there is no point in returning an IQueryable, because it cannot be appended further
        }        
    }
}
