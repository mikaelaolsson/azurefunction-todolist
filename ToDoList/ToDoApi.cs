using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ToDoList.Models;


namespace ToDoList {
    public class ToDoApi {
        private const string TableName = "todos";

        [FunctionName("Create")]
        [OpenApiOperation(operationId: "Create", tags: new[] { "Create Todo" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "text/json", bodyType: typeof(ToDoCreateModel), Required = true, Description = "Add todo")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(ToDo), Description = "The OK response")]
        public static async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] IAsyncCollector<ToDoTableEntity> toDoTableCollector,
            ILogger log) {
            log.LogInformation("Adding new Todo");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ToDoUpdateModel>(requestBody);

            if (String.IsNullOrEmpty(data.Text)) {
                return new BadRequestObjectResult("Please provide some text");
            }

            var todo = new ToDo {
                Text = data.Text,
                Status = Status.NotStarted
            };

            await toDoTableCollector.AddAsync(todo.ToTable());

            return new OkObjectResult(todo);
        }

        [FunctionName("Get")]
        [OpenApiOperation(operationId: "Get", tags: new[] { "Get Todos" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "status", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The **Status** parameter")]
        [OpenApiParameter(name: "id", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The **ID** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(ToDo), Description = "The OK response")]
        public static async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            ILogger log) {
            log.LogInformation("Getting all todos, or get todos by status/id");

            string status = req.Query["status"];
            string id = req.Query["id"];

            // ev lägga till orderby?
            TableQuery<ToDoTableEntity> query = new();
            var segment = await cloudTable.ExecuteQuerySegmentedAsync(query, null);
            var data = segment.Select(ToDoExtensions.ToToDo);

            if (!String.IsNullOrEmpty(status)) {
                if (Enum.TryParse(status, true, out Status currentStatus)) {
                    data = data.Where(t => t.Status == currentStatus);
                }
            }
            if (!String.IsNullOrEmpty(id)) {
                data = data.Where(t => t.Id == id);
            }

            return new OkObjectResult(data);
        }

        [FunctionName("DeleteMany")]
        [OpenApiOperation(operationId: "Delete", tags: new[] { "Delete Todos" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "status", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The **Status** parameter")]
        [OpenApiParameter(name: "id", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The **ID** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(ToDo), Description = "The OK response")]
        public static async Task<IActionResult> DeleteMany(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            ILogger log) {
            log.LogInformation("Deleting all todos, or delete by status/id");

            string status = req.Query["status"];
            string id = req.Query["id"];
            TableContinuationToken token = null;

            TableQuery<ToDoTableEntity> query = new();
            var segment = await cloudTable.ExecuteQuerySegmentedAsync(query, token);
            var data = segment.ToList();

            if (!String.IsNullOrEmpty(status)) {
                if (Enum.TryParse(status, true, out Status currentStatus)) {
                    data = segment.Where(t => t.Status == currentStatus.ToString()).ToList();
                }
            }
            if (!String.IsNullOrEmpty(id)) {
                data = segment.Where(t => t.RowKey == id).ToList();
            }

            do {
                foreach (var row in data) {
                    var operation = TableOperation.Delete(row);
                    cloudTable.Execute(operation);
                }
            } while (token != null);

            return new OkResult();
        }

        [FunctionName("Update")]
        [OpenApiOperation(operationId: "Update", tags: new[] { "Update todo" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "id", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **ID** parameter")]
        [OpenApiRequestBody(contentType: "text/json", bodyType: typeof(ToDoUpdateModel), Required = false, Description = "Update todo")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(ToDo), Description = "The OK response")]
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            ILogger log) {
            log.LogInformation("Update a todo");

            string id = req.Query["id"];

            if (String.IsNullOrEmpty(id)) {
                return new BadRequestObjectResult("Please provide a valid id");
            }

            TableQuery<ToDoTableEntity> query = new();
            var segment = await cloudTable.ExecuteQuerySegmentedAsync(query, null);
            var toDoTable = segment.FirstOrDefault(t => t.RowKey == id);

            if (toDoTable == null) {
                return new BadRequestObjectResult("There are no todos with that id");
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ToDoUpdateModel>(requestBody);

            if (!String.IsNullOrEmpty(data.Text)) {
                toDoTable.Text = data.Text;
            }
            if (!String.IsNullOrEmpty(data.Status)) {
                if (Enum.TryParse(data.Status, true, out Status currentStatus)) {
                    if ((int)currentStatus >= 0 && (int)currentStatus <= 2) {
                        toDoTable.Status = currentStatus.ToString();
                    }
                }
            }
            toDoTable.Updated = DateTimeOffset.Now;
            var updateOperation = TableOperation.Replace(toDoTable);
            var result = await cloudTable.ExecuteAsync(updateOperation);
            return new OkObjectResult(result);
        }
    }
}

