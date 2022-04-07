using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace ToDoList.Models {
    public class History {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ToDoId { get; set; }
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTimeOffset Created { get; set; }
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTimeOffset Edited { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Status? OldStatus { get; set; }
        public string OldText { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Status? CurrentStatus { get; set; }
        public string CurrentText { get; set; }
    }
}
