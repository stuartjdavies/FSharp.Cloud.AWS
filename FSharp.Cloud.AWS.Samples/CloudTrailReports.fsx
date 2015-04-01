(*** hide ***)
#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"
#load "packages/FsLab/FsLab.fsx"

open FSharp.Cloud.AWS
open FSharp.Cloud.AWS.FCloudTrail
open System
open Amazon
open FSharp.Charting

(** Create clients **)
let cloudTrail = FCloudTrail.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2
let s3 = Fs3.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2

(** Retrieve the events logged in the last 7 days **)
let events = QueryEventsInTheLast30Days |> getEventsBy s3 "stuartcloudtrail"                     
                      
(** Graph the usage per day**)                                    
events |> FCloudTrailStats.numberOfEventsPerDay |> Chart.Line 

events |> FCloudTrailStats.numberOfEventsByEventName 
       |> Seq.sortBy(fun (_,count) -> -count)
       |> Seq.take 5
       |> Chart.Bar
        


