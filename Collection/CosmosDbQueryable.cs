using CosmosDbContext.Collection.FeedOptionsSupport;
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
    public abstract class CosmosDbQueryable<T> : IOrderedQueryable<T>, IOptionProvider
    {
        protected readonly DocumentClient client;
        protected readonly Uri uri;

        public event Action<string> Log
        {
            add => ((CosmosDbQueryProvider)Provider).Log += value;
            remove => ((CosmosDbQueryProvider)Provider).Log -= value;
        }

        public CosmosDbQueryable(Uri uri, DocumentClient client, CosmosDbQueryProvider provider)
        {
            Expression = Expression.Constant(this);
            this.uri = uri;
            this.client = client;
            this.Options = new CosmosDbQueryOptions();
            provider.OptionsProvider = this;
            this.Provider = provider;

        }

        internal CosmosDbQueryable(IQueryProvider provider, Expression expression)
        {
            Provider = provider;
            Expression = expression;
        }

        public CosmosDbQueryOptions Options { get; }

        public IEnumerator<T> GetEnumerator() => Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public Type ElementType => typeof(T);
        public Expression Expression { get; }
        public IQueryProvider Provider { get; }
    }
}
