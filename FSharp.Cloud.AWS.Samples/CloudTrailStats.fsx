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
let myCloudTrailQuery = FCloudTrail.query s3 "stuartcloudtrail"

(** Retrieve the events logged in the last 7 days **)
CloudTrailQueryRequest(dateFilter=eventsInTheLast7Days)
|> myCloudTrailQuery
|> FCloudTrailStats.numberOfEventsByEventName 
|> Seq.sortBy(fun (_,count) -> -count)
|> Seq.take 8
|> Chart.Bar        


