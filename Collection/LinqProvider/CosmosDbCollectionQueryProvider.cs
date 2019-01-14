using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDbContext.Collection.LinqProvider
{
    internal class CosmosDbCollectionQueryProvider : CosmosDbQueryProvider
    {
        private static readonly MethodInfo createDocumentQueryMethod;
        static CosmosDbCollectionQueryProvider()
        {
            createDocumentQueryMethod = typeof(DocumentClient).GetMethods().Single(m => m.Name == nameof(DocumentClient.CreateDocumentQuery) &&
                                                                                       m.IsGenericMethod &&
                                                                                       m.GetParameters().Count() == 2 &&
                                                                                       m.GetParameters().First().ParameterType == typeof(Uri) &&
                                                                                       m.GetParameters().ElementAt(1).ParameterType == typeof(FeedOptions));
        }
        public CosmosDbCollectionQueryProvider(string database, string collectionName, DocumentClient client)
            : base(database, collectionName, UriFactory.CreateDocumentCollectionUri(database, collectionName), client, typeof(CosmosDbCollection<>))
        {
        }


        public override IQueryable<T> CreateQuery<T>(Expression expression) => new CosmosDbCollection<T>(this, expression);

        protected override IOrderedQueryable CreateNewRoot(Type elementType, DocumentClient client, Uri uri, FeedOptions feedOptions) 
            => (IOrderedQueryable)createDocumentQueryMethod.MakeGenericMethod(elementType).Invoke(client, new object[] { uri, feedOptions });            
        
    }
}
