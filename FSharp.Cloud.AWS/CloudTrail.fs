namespace FSharp.Cloud.AWS

open System
open System.Collections.Generic
open Amazon.S3
open Amazon.S3.Model
open Amazon.Runtime
open Amazon.SecurityToken
open Amazon.CloudTrail
open Amazon.CloudTrail.Model
open FSharp.Data

module FCloudTrail =
        let createClientFromCsvFile fileName (region : Amazon.RegionEndpoint) =
                let accessKey, secretAccessKey = AwsUtils.getCredFromCsvFile fileName
                new AmazonCloudTrailClient(accessKey, secretAccessKey, region)      

// How do I embed the resourse                                            
type CloudTrailFileSchema = JsonProvider<"""C:\Users\stuart\Documents\GitHub\FSharp.Cloud.AWS\FSharp.Cloud.AWS\CloudTrailFileSchema.json""", EmbeddedResource="FSharp.Cloud.AWS, CloudTrailFileSchema.json">

// Thought this would be a good candidate for partial application, 
// Getting message: Either make the arguments to 'numberOfEventsByEventSource' explicit or, if you do not intend for it to be generic, add a type annotation.
module FQueryCloudTrail = 
            let numberOfEventsBy f (es : CloudTrailFileSchema.Record seq) = es |> Seq.groupBy f |> Seq.map(fun (g, es) -> g, Seq.length es)
            let numberOfEventsByEventSource es = es |> numberOfEventsBy (fun e  -> e.EventSource)
            let numberOfEventsByEventName es = es |> numberOfEventsBy (fun e -> e.EventName)                                
            let numberOfEventsByEventType es = es |> numberOfEventsBy (fun e -> e.EventType)   
            let numberOfEventsByRegion es = es |> numberOfEventsBy (fun e -> e.AwsRegion)                                         
            let numberOfEventsPerMonth es = es |> numberOfEventsBy (fun e -> new DateTime(e.EventTime.Year, e.EventTime.Month, e.EventTime.Day))            
            let numberOfEventsPerDay es = es |> numberOfEventsBy (fun e -> new DateTime(e.EventTime.Year, e.EventTime.Month, e.EventTime.Day))
            let numberOfEventsPerHour es = es |> numberOfEventsBy (fun e -> new DateTime(e.EventTime.Year, e.EventTime.Month, e.EventTime.Day, e.EventTime.Hour, 0,0)) 
