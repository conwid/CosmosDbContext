namespace CosmosDbContext.Collection.FeedOptionsSupport
{
    public class CosmosDbQueryOptions
    {
        internal CosmosDbQueryOptions()
        {

        }
        public int? MaxItemCount { get; set; }
        public string RequestContinuation { get; set; }
        public string SessionToken { get; set; }
        public bool? EnableScanInQuery { get; set; }
        public bool EnableCrossPartitionQuery { get; set; }
        public bool? EnableLowPrecisionOrderBy { get; set; }                    
        public string PartitionKeyRangeId { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public int MaxBufferedItemCount { get; set; }
        public bool PopulateQueryMetrics { get; set; }
        public int? ResponseContinuationTokenLimitInKb { get; set; }
        public bool DisableRUPerMinuteUsage { get; set; }
    }
}
