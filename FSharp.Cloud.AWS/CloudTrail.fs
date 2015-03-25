namespace FSharp.Cloud.AWS

open System
open System.Collections.Generic
open Amazon.S3
open Amazon.S3.Model
open Amazon.Runtime
open Amazon.SecurityToken
open Amazon.CloudTrail
open Amazon.CloudTrail.Model
open Amazon.S3.Transfer
open Amazon.S3.Model
open Amazon.S3.IO
open FSharp.Data
open Amazon
open System.Globalization


// How do I embed the resourse                                            
type CloudTrailFileSchema = JsonProvider<"""C:\Users\stuart\Documents\GitHub\FSharp.Cloud.AWS\FSharp.Cloud.AWS\CloudTrailFileSchema.json""", EmbeddedResource="FSharp.Cloud.AWS, CloudTrailFileSchema.json">

module FCloudTrail =
        type CloudTrailFileLogInfo = { BucketName : string; Region : string; Date : DateTime; Key : string; }
        
        let createClientFromCsvFile fileName (region : Amazon.RegionEndpoint) =
                let accessKey, secretAccessKey = AwsUtils.getCredFromCsvFile fileName
                new AmazonCloudTrailClient(accessKey, secretAccessKey, region)  
                      
        let getLogFileInfosBy (f : CloudTrailFileLogInfo -> bool) (bucketName : string) (c : AmazonS3Client) =        
                c.ListObjects(bucketName).S3Objects 
                |> Seq.filter(fun s3obj -> s3obj.Key.Contains(".gz"))
                |> Seq.map(fun s3obj -> let fields = s3obj.Key.Substring(s3obj.Key.LastIndexOf("/")).Split('_')                                                       
                                        let date = DateTime.ParseExact(fields.[3].Substring(0,8), "yyyyMMdd", CultureInfo.InvariantCulture)                                 
                                        { CloudTrailFileLogInfo.BucketName=bucketName; Region=fields.[2]; Date=date; Key=s3obj.Key; })
                |> Seq.filter f
                |> Seq.sortBy(fun l -> l.Date)

        let downloadLog (l : CloudTrailFileLogInfo) dest (c : AmazonS3Client) =                                
                GetObjectRequest(BucketName=l.BucketName, Key=l.Key) |> c.GetObject |> (fun r -> r.WriteResponseStreamToFile(dest))
        
        let getLog (l : CloudTrailFileLogInfo) (c : AmazonS3Client) = 
                use s = c.GetObject(GetObjectRequest(BucketName=l.BucketName, Key=l.Key)).ResponseStream                
                GZip.readFromStream s                              

        let getLogFileInfos bucketName = 
                getLogFileInfosBy (fun _ -> true) bucketName
        
        let getEventsBy f bucketName s3Client = 
                    s3Client |> getLogFileInfosBy f bucketName
                             |> Seq.toArray
                             |> Array.Parallel.map(fun l -> let s = getLog l s3Client
                                                            CloudTrailFileSchema.Parse(s).Records)
                             |> Array.concat

        let sinceDays (day : int) (l : CloudTrailFileLogInfo) = (l.Date >= DateTime.Now.Subtract(new TimeSpan(day,0,0,0,0)))             
        let getEventsToday bucketName = getEventsBy (sinceDays 0) bucketName
        let getEventssinceYesterday bucketName = getEventsBy (sinceDays 0) bucketName
        let getEventsInTheLast2Days bucketName = getEventsBy (sinceDays 2) bucketName
        let getEventsInTheLast7Days bucketName = getEventsBy (sinceDays 7) bucketName
        let getEventsInTheLast30Days bucketName = getEventsBy (sinceDays 30) bucketName
         
        let downloadLogFilesBy f (fs : CloudTrailFileLogInfo seq) (c : AmazonS3Client) = 
                (new TransferUtility(c)).Download(new TransferUtilityDownloadRequest() )                

        let getEventsFromFilesBy (filter : CloudTrailFileSchema.Record -> bool) (fileNames : string seq)  =                        
                seq { for fn in fileNames do yield! CloudTrailFileSchema.Load(fn).Records |> Seq.filter filter }

        let getEventsFromFile (fileNames : string seq) =
                getEventsBy (fun _ -> true) 

module FQueryCloudTrail = 
            let numberOfEventsBy f (es : CloudTrailFileSchema.Record seq) = es |> Seq.groupBy f |> Seq.map(fun (g, es) -> g, Seq.length es)
            let numberOfEventsByEventSource es = numberOfEventsBy (fun e -> e.EventSource) es
            let numberOfEventsByEventName es =  numberOfEventsBy (fun e -> e.EventName) es                             
            let numberOfEventsByEventType es = numberOfEventsBy (fun e -> e.EventType) es   
            let numberOfEventsByRegion es = numberOfEventsBy (fun e -> e.AwsRegion) es                                        
            let numberOfEventsPerMonth es = numberOfEventsBy (fun e -> new DateTime(e.EventTime.Year, e.EventTime.Month, e.EventTime.Day)) es           
            let numberOfEventsPerDay es = numberOfEventsBy (fun e -> new DateTime(e.EventTime.Year, e.EventTime.Month, e.EventTime.Day)) es
            let numberOfEventsPerHour es = numberOfEventsBy (fun e -> new DateTime(e.EventTime.Year, e.EventTime.Month, e.EventTime.Day, e.EventTime.Hour, 0,0)) es
