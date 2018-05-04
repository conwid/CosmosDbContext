using CosmosDbContext.Collection.FeedOptionsSupport;
using CosmosDbContext.Collection.LinqProvider.ExpressionVisitors;
using CosmosDbContext.Extensions;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDbContext.Collection.LinqProvider
{
    public abstract class CosmosDbQueryProvider : IQueryProvider
    {
        private readonly DocumentClient client;
        private readonly string database;
        private readonly string collectionName;
        private readonly Uri uri;
        internal IOptionProvider OptionsProvider { get; set; }

        internal event Action<string> Log;

        private readonly Type queryType;

        public CosmosDbQueryProvider(string database, string collectionName, Uri uri, DocumentClient client, Type queryType)
        {
            this.client = client;
            this.database = database;
            this.collectionName = collectionName;
            this.uri = uri;
            this.queryType = queryType;
        }

        public abstract IQueryable<T> CreateQuery<T>(Expression expression);

        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = expression.Type.GetElementTypeForExpression();
            try
            {
                return (IQueryable)Activator.CreateInstance(queryType.MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public object Execute(Expression expression)
        {
            Type elementType = expression.Type.GetElementTypeForExpression();
            try
            {
                return this.GetType().GetMethods().Where(m => m.Name == nameof(Execute) && m.IsGenericMethod).Single().Invoke(this, new[] { expression });
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        // See https://msdn.microsoft.com/en-us/library/bb546158.aspx for more details
        public TResult Execute<TResult>(Expression expression)
        {
            var root = new RootExpressionFinderVisitor().GetRoot(expression);
            var type = root.Type.GetElementTypeForExpression();

            var newRoot = CreateNewRoot(type, client, uri, GetFeedOptions());

            RootReplacingVisitor treeCopier = new RootReplacingVisitor(newRoot);
            Expression newExpressionTree = treeCopier.Visit(expression);

            Log?.Invoke(GetQueryTextUNSAFE(newExpressionTree));

            bool isEnumerable = (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (isEnumerable)
            {
                return (TResult)newRoot.Provider.CreateQuery(newExpressionTree);
            }
            var result = newRoot.Provider.Execute(newExpressionTree);
            try
            {
                return (TResult)result;
            }
            // Here the type is not known at compile time (Execute returns an object)
            // The query is executed using the CosmosDb SDK which uses Json.NET
            // When the type is not specified at compile time (like know), Json.NET deserializes integers as Int64
            // This can lead to problems when e.g. Count() is used, which expects an Int32
            // Since this will be deserialized as Int64 and then boxed to an object, it cannot be cast to an Int32
            // This is a combined limitation of json and Json.NET
            // This here is to support some basic scenarios and make the solution more robust
            catch (InvalidCastException)
            {
                return (TResult)Convert.ChangeType(result, typeof(TResult), CultureInfo.InvariantCulture);
            }
        }

        // HACK: This is built on internal APIs based on my explorations of the source code using ILSpy
        // There's a whole lot of reflection here that's definitely gonna break - tread carefully
        private string GetQueryTextUNSAFE(Expression newExpressionTree)
        {
            try
            {
                var translator = client.GetType().Assembly.GetType("Microsoft.Azure.Documents.Linq.DocumentQueryEvaluator");
                var translatorMethod = translator.GetMethod("Evaluate", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var result = translatorMethod.Invoke(null, new[] { newExpressionTree });
                if (result == null)
                    return string.Empty;
                var queryText = (string)result.GetType().GetProperty("QueryText").GetValue(result);
                return queryText;
            }
            catch (Exception ex)
            {
                return $"Could not get query text: {ex.ToString()}";
            }
        }

        protected abstract IOrderedQueryable CreateNewRoot(Type elementType, DocumentClient client, Uri uri, FeedOptions feedOptions);

        private FeedOptions GetFeedOptions()
        {
            return new FeedOptions
            {
                DisableRUPerMinuteUsage = this.OptionsProvider.Options.DisableRUPerMinuteUsage,
                EnableCrossPartitionQuery = this.OptionsProvider.Options.EnableCrossPartitionQuery,
                EnableLowPrecisionOrderBy = this.OptionsProvider.Options.EnableLowPrecisionOrderBy,
                EnableScanInQuery = this.OptionsProvider.Options.EnableScanInQuery,
                MaxBufferedItemCount = this.OptionsProvider.Options.MaxBufferedItemCount,
                MaxDegreeOfParallelism = this.OptionsProvider.Options.MaxDegreeOfParallelism,
                MaxItemCount = this.OptionsProvider.Options.MaxItemCount,
                PartitionKeyRangeId = this.OptionsProvider.Options.PartitionKeyRangeId,
                PopulateQueryMetrics = this.OptionsProvider.Options.PopulateQueryMetrics,
                RequestContinuation = this.OptionsProvider.Options.RequestContinuation,
                ResponseContinuationTokenLimitInKb = this.OptionsProvider.Options.ResponseContinuationTokenLimitInKb,
                SessionToken = this.OptionsProvider.Options.SessionToken
            };
        }
    }
}
