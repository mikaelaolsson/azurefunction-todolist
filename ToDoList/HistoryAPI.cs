using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
using ToDoList.Models;

namespace ToDoList {
    public class HistoryAPI {
        private const string TableName = "histories";

        [FunctionName("GetHistories")]
        [OpenApiOperation(operationId: "Get", tags: new[] { "Histories" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "todoId", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The ID of the todo")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(History), Description = "The OK response")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] CloudTable cloudTable, 
            ILogger log) {
            log.LogInformation("Getting all histories, or get histories by todoId");

            string todoId = req.Query["todoId"];

            TableQuery<HistoryTableEntity> query = new();
            var segment = await cloudTable.ExecuteQuerySegmentedAsync(query, null);
            var data = segment.Select(HistoryExtensions.ToHistory);

            if (!String.IsNullOrEmpty(todoId)) {
                data = data.Where(t => t.ToDoId == todoId);
            }
            data = data.OrderBy(h => h.ToDoId).ThenByDescending(h => h.Edited);
            return new OkObjectResult(data);
        }

        [FunctionName("DeleteHistories")]
        [OpenApiOperation(operationId: "Delete", tags: new[] { "Histories" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "todoId", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The ID of the todo")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(ToDo), Description = "The OK response")]
        public static async Task<IActionResult> DeleteMany(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = TableName)] HttpRequest req,
            [Table(TableName, Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            ILogger log) {
            log.LogInformation("Deleting all histories, or delete histories by todoId");

            string todoId = req.Query["todoId"];
            TableContinuationToken token = null;

            TableQuery<HistoryTableEntity> query = new();
            var segment = await cloudTable.ExecuteQuerySegmentedAsync(query, token);
            var data = segment.ToList();

            if (!String.IsNullOrEmpty(todoId)) {
                data = segment.Where(t => t.ToDoId == todoId).ToList();
            }

            do {
                foreach (var row in data) {
                    var operation = TableOperation.Delete(row);
                    cloudTable.Execute(operation);
                }
            } while (token != null);

            return new OkResult();
        }
    }
}

