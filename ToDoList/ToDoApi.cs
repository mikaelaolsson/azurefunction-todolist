using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToDoList.Models;


namespace ToDoList {
    public class ToDoApi {
        private const string TableName = "todos";

        [FunctionName("Create")]
        public static async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] IAsyncCollector<ToDoTableEntity> toDoTableCollector,
            ILogger log) {
            log.LogInformation("Adding new Todo");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<ToDoDto>(requestBody);

            if (input.Text == null) {
                return new BadRequestObjectResult("Please provide some text");
            }

            var todo = new ToDo {
                Text = input.Text,
                Status = Status.NotStarted
            };

            await toDoTableCollector.AddAsync(todo.ToTable());

            return new OkObjectResult(todo);
        }

        [FunctionName("Get")]
        public static async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            ILogger log) {
            log.LogInformation("Getting all todos, or get todos by status");

            string status = req.Query["status"];

            // ev lägga till orderby?
            TableQuery<ToDoTableEntity> query = new TableQuery<ToDoTableEntity>();
            var segment = await cloudTable.ExecuteQuerySegmentedAsync(query, null);
            var data = segment.Select(ToDoExtensions.ToToDo);

            if (status != null) {
                if (Enum.TryParse(status, true, out Status currentStatus)) {
                    data = data.Where(t => t.Status == currentStatus);
                }
            }

            return new OkObjectResult(data);
        }
        // This might not be needed
        [FunctionName("GetById")]
        public static IActionResult GetById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = TableName + "/{id}")] HttpRequest req,
            [Table(TableName, "TODO", "{id}", Connection = "AzureWebJobsStorage")] ToDoTableEntity toDoTable,
            ILogger log, string id) {
            log.LogInformation("Getting a todo by Id");

            if (toDoTable == null) {
                log.LogInformation($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(toDoTable.ToToDo());
        }

        // Finns det något smidigare sätt för detta? Blir hella långt query :(
        [FunctionName("Delete")]
        public static async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = TableName + "/{partitionKey}/{rowKey}")] HttpRequest req,
            [Table(TableName, "{partitionKey}", "{rowKey}")] ToDoTableEntity toDoTable,
            [Table(TableName)] CloudTable cloudTable,
            ILogger log) {
            log.LogInformation("Delete a todo");

            var deleteOperation = TableOperation.Delete(toDoTable);
            var result = await cloudTable.ExecuteAsync(deleteOperation);
            return new OkObjectResult(result);
        }

        [FunctionName("DeleteMany")]
        public static async Task<IActionResult> DeleteMany(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            ILogger log) {
            log.LogInformation("Deleting all todos, or delete by status");

            string status = req.Query["status"];
            TableContinuationToken token = null;

            TableQuery<ToDoTableEntity> query = new TableQuery<ToDoTableEntity>();
            var segment = await cloudTable.ExecuteQuerySegmentedAsync(query, token);
            var data = new List<ToDoTableEntity>();

            if (status != null) {
                if (Enum.TryParse(status, true, out Status currentStatus)) {
                    data = segment.Where(t => t.Status == (int)currentStatus).ToList();
                }
            }
            else {
                data = segment.ToList();
            }
            do {
                foreach (var row in data) {
                    var operation = TableOperation.Delete(row);
                    cloudTable.Execute(operation); // might need 2 be async??
                }
            } while (token != null);

            return new OkResult();
        }

        [FunctionName("Update")]
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = TableName + "/{partitionKey}/{rowKey}")] HttpRequest req,
            [Table(TableName, "{partitionKey}", "{rowKey}")] ToDoTableEntity toDoTable,
            [Table(TableName)] CloudTable cloudTable,
            ILogger log) {
            log.LogInformation("Update a todo");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ToDoDto>(requestBody);

            if (!String.IsNullOrEmpty(data.Text)) {
                toDoTable.Text = data.Text;
            }
            if (data.Status != Status.NoInformation) {
                toDoTable.Status = (int)data.Status;
            }

            var updateOperation = TableOperation.Replace(toDoTable);
            var result = await cloudTable.ExecuteAsync(updateOperation);
            return new OkObjectResult(result);
        }
    }
}

