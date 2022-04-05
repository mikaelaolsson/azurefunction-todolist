using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoList.Models
{
    public class ToDo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; }
        public Status Status { get; set; }
        //public bool Status { get; set; }
        public DateTime Created {get; set; } = DateTime.Now;
    }

    public enum Status
    {
        NoInformation,
        NotStarted,
        InProgress,
        Completed
    }
}
