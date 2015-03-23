(*** hide ***)
#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"
#load "packages/FsLab/FsLab.fsx"

open FSharp.Cloud.AWS
open Amazon.CloudWatch
open Amazon.CloudWatch.Model
open Amazon.CloudWatchLogs
open Amazon.CloudWatchLogs.Model
open FSharp.Cloud.AWS.AwsUtils
open System.Collections.Generic
open System
open Amazon.S3.Transfer
open Amazon.S3.IO
open Amazon
open FSharp.Charting

let ctClient = FCloudTrail.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2
let s3Client = Fs3.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2

let trails = ctClient.DescribeTrails().TrailList
let bucketName = ctClient.DescribeTrails().TrailList.[0].S3BucketName

let dir = "AWSLogs/359282757674/CloudTrail/ap-southeast-2/2015/03/20"        
(new TransferUtility(s3Client)).DownloadDirectory(bucketName, dir, """C:\AWSCloudTrail""")
GZip.unzipFilesInDir """C:\AWSCloudTrail"""


let events = AwsUtils.getFileNames """C:\AWSCloudTrail""" 
             |> Seq.filter(fun fn -> fn.EndsWith(".json"))
             |> Seq.map(fun fn -> CloudTrailFileSchema.Load(fn).Records)
             |> Seq.concat                            

(** Describe the frequency of events per hour **)              
events |> FQueryCloudTrail.numberOfEventsPerHour       
       |> Chart.Line             
                        
events |> FQueryCloudTrail.numberOfEventsPerDay
       |> Chart.Line     




