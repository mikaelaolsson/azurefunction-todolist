using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;


namespace ToDoList.Models {
    public class ToDo {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Status? Status { get; set; }
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.Now;
    }

    public enum Status {
        NotStarted,
        InProgress,
        Completed
    }
}
