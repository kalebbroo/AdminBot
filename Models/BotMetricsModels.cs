using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Models;

namespace AdminBot.Models
{
    /// <summary>
    /// Represents collected bot metrics at a specific timestamp.
    /// </summary>
    public class BotMetrics : BaseModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("metrics")]
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    }
}
