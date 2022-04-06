using System;

namespace ToDoList.Models {
    public static class HistoryExtensions {
        public static HistoryTableEntity ToHistoryTable(this History history) {
            return new HistoryTableEntity {
                PartitionKey = "HISTORY",
                RowKey = history.Id,
                ToDoId = history.ToDoId,
                Created = history.Created,
                Edited = history.Edited,
                OldStatus = history.OldStatus.ToString(),
                OldText = history.OldText,
                CurrentStatus = history.CurrentStatus.ToString(),
                CurrentText = history.CurrentText,
            };
        }

        public static History ToHistory(this HistoryTableEntity historyTable) {
            return new History { 
                Id = historyTable.RowKey,
                ToDoId = historyTable.ToDoId,
                Created = historyTable.Created,
                Edited = historyTable.Edited,
                OldStatus = (Status)Enum.Parse(typeof(Status), historyTable.OldStatus, true),
                OldText = historyTable.OldText,
                CurrentStatus = (Status)Enum.Parse(typeof(Status), historyTable.CurrentStatus, true),
                CurrentText = historyTable.CurrentText
            };
        }
    }
}
