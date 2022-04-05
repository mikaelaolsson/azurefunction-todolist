﻿using Microsoft.Azure.Cosmos.Table;
using System;

namespace ToDoList.Models
{
    public class ToDoTableEntity : TableEntity
    {
        public string Text { get; set; }
        public Status Status { get; set; }
        //public bool Status { get; set; }
        public DateTime Created {get; set;}
    }
}