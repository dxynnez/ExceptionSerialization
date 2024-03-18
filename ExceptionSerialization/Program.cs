using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(
        new Action<WorkerOptions>(builder =>
        {
            // ATTENTION: turning on the UserCodeException will make all FailureDetails.ErrorType become unknown, including the build-in System.Exception!
            // builder.EnableUserCodeException = true;
        }))
    .Build();

host.Run();