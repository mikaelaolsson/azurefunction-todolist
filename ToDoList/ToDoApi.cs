using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToDoList.Models;


namespace ToDoList
{
    public class ToDoApi
    {
        private const string TableName = "todos";

        [FunctionName("Create")]
        public static async Task<IActionResult> Create(
                [HttpTrigger(AuthorizationLevel.Function, "post", Route = TableName)] HttpRequest req,
                [Table(TableName, Connection = "AzureWebJobsStorage")] IAsyncCollector<ToDoTableEntity> toDoTableCollector,
                ILogger log)
        {
            log.LogInformation("Adding new Todo");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoDto>(requestBody);

            if (input.Text == null)
            {
                return new BadRequestObjectResult("Please provide some text");
            }

            var todo = new ToDo
            {
                Text = input.Text,
                // Den lägger till rätt status men den sparar den INTE
                Status = Status.NotStarted
            };

            var table = todo.ToTable();

            await toDoTableCollector.AddAsync(table);

            return new OkObjectResult(todo);
        }

        [FunctionName("GetAll")]
        public static async Task<IActionResult> GetAll(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            ILogger log)
        {
            log.LogInformation("Getting all Todos");

            TableQuery<ToDoTableEntity> query = new TableQuery<ToDoTableEntity>();
            var segment = await cloudTable.ExecuteQuerySegmentedAsync(query, null);
            var data = segment.Select(ToDoExtensions.ToToDo);

            return new OkObjectResult(data);
        }
        // This might not be needed
        [FunctionName("GetTodoById")]
        public static IActionResult GetTodoById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = TableName + "/{id}")] HttpRequest req,
            [Table(TableName, "TODO", "{id}", Connection = "AzureWebJobsStorage")] ToDoTableEntity toDoTable,
            ILogger log, string id)
        {
            log.LogInformation("Getting Todos by Id");

            if (toDoTable == null)
            {
                log.LogInformation($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(toDoTable.ToToDo());
        }

        // Finns det något smidigare sätt för detta? Blir hella långt query :(
        [FunctionName("Delete")]
        public static async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = TableName + "/{partitionKey}/{rowKey}")] HttpRequest req,
            [Table("todos", "{partitionKey}", "{rowKey}")] ToDoTableEntity toDoTable,
            [Table("todos")] CloudTable cloudTable,
            ILogger log)
        {
            log.LogInformation("Delete a todo");

            var deleteOperation = TableOperation.Delete(toDoTable);
            var result = await cloudTable.ExecuteAsync(deleteOperation);
            return new OkObjectResult(result);
        }
        // Update: bara ändra status eller också ändra texten? hmm
        // ev ha dem uppdelade? en för status, en för text
        // Parse för Enum Status?

        [FunctionName("Update")]
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = TableName + "/{partitionKey}/{rowKey}")] HttpRequest request,
            [Table("todos", "{partitionKey}", "{rowKey}")] ToDoTableEntity toDoTable,
            [Table("todos")] CloudTable cloudTable,
            ILogger log)
        {
            log.LogInformation("Update a todo");

            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<TodoDto>(requestBody);

            toDoTable.Text = data.Text;
            toDoTable.Status = data.Status;


            var updateOperation = TableOperation.Replace(toDoTable);
            var result = await cloudTable.ExecuteAsync(updateOperation);
            return new OkObjectResult(result);
        }
    }
}

