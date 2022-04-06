using Microsoft.Azure.Cosmos.Table;
using System;

namespace ToDoList.Models {
    public class ToDoTableEntity : TableEntity {
        public string Text { get; set; }
        public int? Status { get; set; }
        public DateTime Created { get; set; }
    }
}