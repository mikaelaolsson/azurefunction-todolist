namespace ToDoList.Models
{
    public static class ToDoExtensions
    {
        public static ToDoTableEntity ToTable(this ToDo todo)
        {
            return new ToDoTableEntity
            {
                PartitionKey = "TODO",
                RowKey = todo.Id,
                Created = todo.Created,
                Text = todo.Text,
                Status = (int)todo.Status
            };
        }
        public static ToDo ToToDo(this ToDoTableEntity todoTable)
        {
            return new ToDo
            {
                Id = todoTable.RowKey,
                Created = todoTable.Created,
                Text = todoTable.Text,
                Status = (Status)todoTable.Status
            };
        }
    }
}
