using System;

namespace ToDoList.Models {
    public class ToDo {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; }
        public Status? Status { get; set; }
        public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.Now;
    }

    public enum Status {
        NotStarted,
        InProgress,
        Completed
    }
}
