using CosmosDbContext.Collection;
using CosmosDbContext.Extensions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDbContext
{
    /// <summary>
    /// This base class represents the connection to CosmosDb
    /// </summary>
    public abstract class CosmosDbContext : IDisposable
    {
        private readonly DocumentClient client;
        private readonly string database;

        /// <summary>
        /// This event is fired when a LInQ query executed on the server side.
        /// The query SQL text is the parameter
        /// </summary>
        public event Action<string> Log
        {
            add => Subscribe(value);
            remove => Unsubscribe(value);
        }

        /// <summary>
        /// Creates a new instance of the context
        /// </summary>
        /// <param name="baseUri">Database uri (see the Azure portal for details)</param>
        /// <param name="authKey">Authentication key for the database (see the portal for details)</param>
        /// <param name="database">The name of the database</param>
        public CosmosDbContext(Uri baseUri, string authKey, string database)
        {
            this.client = new DocumentClient(baseUri, authKey);
            this.database = database;
            Init();
        }

        /// <summary>
        /// Creates a new instance of the context
        /// </summary>
        /// <param name="baseUri">Database uri (see the Azure portal for details)</param>
        /// <param name="authKey">Authentication key for the database (see the portal for details)</param>
        /// <param name="database">The name of the database</param>
        public CosmosDbContext(string baseUri, string authKey, string database) : this(new Uri(baseUri), authKey, database)
        {

        }

        /// <summary>
        /// Closes the connection to the database
        /// </summary>
        public void Dispose()
        {
            client?.Dispose();
        }

        private List<PropertyInfo> GetDocumentDbRelatedProperties()
        {
            // We are interested in properties that have this special attribute           
            return this.GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && (p.GetCustomAttribute<CosmosDbCollectionAttribute>() != null)).ToList();
        }

        private void Init()
        {
            var properties = GetDocumentDbRelatedProperties();
            foreach (var property in properties)
            {
                // When initializing a collection, the algorithm looks at the value specified in the attribute
                // If no collection name is specified, then name of the property is used
                var composingType = property.PropertyType.GetGenericArguments()[0];
                var collectionName = property.GetCustomAttribute<CosmosDbCollectionAttribute>()?.CollectionName ?? property.Name;
                var collectionType = typeof(CosmosDbCollection<>).MakeGenericType(composingType);
                property.SetValue(this, Activator.CreateInstance(collectionType, new object[] { database, collectionName, client }));

            }
        }

        // The event at the context level is an "aggregate event" for all the same events at collection level
        private void Unsubscribe(Action<string> value)
        {
            List<PropertyInfo> properties = GetDocumentDbRelatedProperties();
            foreach (var property in properties)
            {
                var propValue = property.GetValue(this);
                propValue.GetType().GetEvent("Log").RemoveEventHandler(propValue, value);
            }
        }
        private void Subscribe(Action<string> value)
        {
            List<PropertyInfo> properties = GetDocumentDbRelatedProperties();
            foreach (var property in properties)
            {
                var propValue = property.GetValue(this);
                propValue.GetType().GetEvent("Log").AddEventHandler(propValue, value);
            }
        }


        // These are used for executing stored procedures
        protected IEnumerable<T> ExecuteStoredProcedure<T>(string storedProcName, string collectionName, params dynamic[] parameters)
        {
            var r = client.ExecuteStoredProcedureAsync<string>(UriFactory.CreateStoredProcedureUri(database, collectionName, storedProcName), parameters).Result.Response;
            using (JsonTextReader reader = new JsonTextReader(new StringReader(r)))
            {
                if (!reader.Read())
                {
                    return new List<T>().AsQueryable();
                }

                if (reader.TokenType == JsonToken.Null || reader.TokenType == JsonToken.None || reader.TokenType == JsonToken.Undefined)
                {
                    return new List<T>().AsQueryable();
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    T instance = JsonConvert.DeserializeObject<T>(r);
                    return new List<T>() { instance }.AsQueryable();
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    return JsonConvert.DeserializeObject<List<T>>(r).AsQueryable();
                }
                else if (reader.TokenType == JsonToken.Boolean || reader.TokenType == JsonToken.Date || reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.String)
                {
                    T instance = JsonConvert.DeserializeObject<T>(r);
                    return new List<T>() { instance }.AsQueryable();
                }
                throw new InvalidOperationException($"Token {reader.TokenType} cannot be the first token in the result");
            }
        }
        protected IEnumerable<dynamic> ExecuteDynamicStoredProcedure(string storedProcName, string collectionName, params dynamic[] parameters)
        {
            var r = client.ExecuteStoredProcedureAsync<string>(UriFactory.CreateStoredProcedureUri(database, collectionName, storedProcName), parameters).Result.Response;
            return JsonExtensions.ReadJsonAsDynamicQueryable(r);
        }
    }
}
