(*** hide ***)
#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"
#load "packages/FsLab/FsLab.fsx"

open FSharp.Cloud.AWS
open FSharp.Cloud.AWS.FCloudTrail
open FSharp.Cloud.AWS.FQueryCloudTrail
open System
open Amazon
open FSharp.Charting

(** Create clients **)
let cloudTrail = FCloudTrail.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2
let s3 = Fs3.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2

(** get logs stored **)
s3 |> getLogFileInfos "stuartcloudtrail" |> Seq.iter(fun x -> printfn "%s" (x.Date.ToString()))

(** Retrieve the events logged in the last 7 days **)
let events = s3 |> getEventsInTheLast30Days "stuartcloudtrail"                      
                      
(** Graph the usage per day**)                                    
events |> numberOfEventsPerDay |> Chart.Line 
        


