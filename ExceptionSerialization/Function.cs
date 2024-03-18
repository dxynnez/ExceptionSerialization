using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace ExceptionSerialization
{
    using System.Text;

    public static class Function
    {
        [Function(nameof(Function))]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(Function));

            try
            {
                await context.CallActivityAsync<string>(nameof(ThrowException), "CustomException");
            }
            catch (TaskFailedException ex)
            {
                logger.LogCritical("Error type is \"{FailureDetailsErrorType}\" <- extra space in the end!", ex.FailureDetails.ErrorType);
            }

            try
            {
                await context.CallActivityAsync<string>(nameof(ThrowException), "UnknownException");
            }
            catch (TaskFailedException ex)
            {
                logger.LogCritical("Error type is \"{FailureDetailsErrorType}\" <- unknown as the type is not included in the ToString method", ex.FailureDetails.ErrorType);
            }

            try
            {
                await context.CallActivityAsync<string>(nameof(ThrowException), "Normal Exception");
            }
            catch (TaskFailedException ex)
            {
                logger.LogCritical("Error type is \"{FailureDetailsErrorType}\"", ex.FailureDetails.ErrorType);
            }

            return "whatever";
        }

        [Function(nameof(ThrowException))]
        public static string ThrowException([ActivityTrigger] string name, FunctionContext executionContext)
        {
            switch (name)
            {
                case "CustomException":
                    throw new ExceptionOverridesToString();
                case "UnknownException":
                    throw new UnknownException();
                default:
                    throw new Exception("Exception with default ToString");
            }
        }

        [Function("Function_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(Function));
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        public class ExceptionOverridesToString : Exception
        {
            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(this.GetType().FullName);
                stringBuilder.Append(" : "); // ATTENTION: this has an extra space between the type name and the colon, and the ErrorType would contain an extra space in the end

                return stringBuilder.ToString();
            }
        }

        public class UnknownException : Exception
        {
            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("I am not going to tell you my type"); // ATTENTION: this would cause the ErrorType to always be null as it doesn't contain the exception type.

                return stringBuilder.ToString();
            }
        }
    }
}