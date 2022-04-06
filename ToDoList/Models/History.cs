using System;

namespace ToDoList.Models {
    public class History {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ToDoId { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Edited { get; set; }
        public Status OldStatus { get; set; }
        public string OldText { get; set; }
        public Status CurrentStatus { get; set; }
        public string CurrentText { get; set; }
    }
}
