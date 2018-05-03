using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDbContext.Collection
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class CosmosDbCollectionAttribute : Attribute
    {
        public string CollectionName { get; set; }
    }
}
