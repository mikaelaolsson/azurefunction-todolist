using Microsoft.Azure.Cosmos.Table;
using System;

namespace ToDoList.Models {
    public class ToDoTableEntity : TableEntity {
        public string Text { get; set; }
        public string Status { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
    }
}