//using Microsoft.Azure.Cosmos.Table;
using Azure;
using Azure.Data.Tables;
using System;

namespace ThingsAPI.Models
{
    public class ThingItem
    {
        public long Thingid { get; set; }
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Image { get; set; }
        public string? Text { get; set; }
        public string? Status { get; set; }
        public string? Data { get; set; }
    }

    public class ThingItemEntity : ITableEntity
    {
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public long Thingid { get; set; }
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Image { get; set; }
        public string? Text { get; set; }
        public string? Status { get; set; }
        public string? Data { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

    }

}
