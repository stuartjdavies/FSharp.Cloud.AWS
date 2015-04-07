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
CloudTrailQueryRequest(s3client=s3, bucketName="stuartcloudtrail", dateFilter=QueryEventsInTheLast7Days)
|> FCloudTrail.query 
|> FCloudTrailStats.numberOfEventsByEventName 
|> Seq.sortBy(fun (_,count) -> -count)
|> Seq.take 8
|> Chart.Bar        


