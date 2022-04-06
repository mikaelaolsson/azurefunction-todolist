using Microsoft.Azure.Cosmos.Table;
using System;

namespace ToDoList.Models {
    public class HistoryTableEntity : TableEntity {
        public string ToDoId { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Edited { get; set; }
        public string OldStatus { get; set; }
        public string OldText { get; set; }
        public string CurrentStatus { get; set; }
        public string CurrentText { get; set; }
    }
}
