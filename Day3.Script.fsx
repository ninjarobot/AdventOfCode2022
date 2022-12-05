#r "nuget: Azure.Monitor.OpenTelemetry.Exporter, 1.0.0-beta.5"
#r "nuget: Microsoft.Extensions.DependencyInjection"
#r "nuget: Microsoft.Extensions.Hosting"
#r "nuget: Microsoft.Extensions.Logging"

open System
open System.Diagnostics.Metrics
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Azure.Monitor.OpenTelemetry.Exporter
open OpenTelemetry
open OpenTelemetry.Metrics

let priorities =
    seq {
        seq {'a'..'z'}
        seq {'A'..'Z'}
    } |> Seq.concat
    |> Seq.mapi (fun idx item -> (item, idx + 1)) |> dict

let priority (item:char) =
    priorities[item]

let findDuplicateItems(contents:string) =
    let midpoint = contents.Length / 2
    let firstHalf = contents.Substring(0, midpoint) |> Set
    let secondHalf = contents.Substring(midpoint) |> Set
    firstHalf |> Set.intersect secondHalf

let meter = new Meter("AdventOfCode.Elves", "1.0.0")
let counter : Counter<int> = meter.CreateCounter("error-priority")

Sdk.CreateMeterProviderBuilder()
    .AddMeter("AdventOfCode.Elves")
    .AddAzureMonitorMetricExporter(
        fun o -> o.ConnectionString <- Environment.GetEnvironmentVariable "APP_INSIGHTS_CONN_STRING"
    )
    .Build()

// Need to run as a background service so the application doesn't exit before metrics buffer is flushed.

type DataProcessor (logger:ILogger<DataProcessor>) =
    inherit BackgroundService ()
    override this.ExecuteAsync _ =
        logger.LogInformation "Reading data..."
        async {
            try
                IO.File.ReadAllLines (IO.Path.Combine(__SOURCE_DIRECTORY__, "data/Day3.data.txt"))
                |> Seq.map (findDuplicateItems >> Set.map priority >> Seq.sum)
                |> Seq.iteri (fun idx priority -> 
                    counter.Add(priority, Collections.Generic.KeyValuePair("suitcase", box(idx+1)))
                )
                logger.LogInformation "Published metrics"
            with
                ex -> logger.LogError (ex.ToString())
        } |> Async.StartAsTask :> _

let host =
    Host.CreateDefaultBuilder()
        .ConfigureServices(
            fun services -> 
                services.AddHostedService<DataProcessor>()
                |> ignore<IServiceCollection>
        )
        .Build()

host.Run()