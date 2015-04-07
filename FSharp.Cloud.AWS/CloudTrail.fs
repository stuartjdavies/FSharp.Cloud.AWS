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
                      
       
        let getLogFileInfosBy (c : AmazonS3Client) (bucketName : string) (f : CloudTrailFileLogInfo -> bool) =        
                c.ListObjects(bucketName).S3Objects 
                |> Seq.filter(fun s3obj -> s3obj.Key.Contains(".gz"))
                |> Seq.map(fun s3obj -> let fields = s3obj.Key.Substring(s3obj.Key.LastIndexOf("/")).Split('_')                                                       
                                        let date = DateTime.ParseExact(fields.[3].Substring(0,8), "yyyyMMdd", CultureInfo.InvariantCulture)                                 
                                        { CloudTrailFileLogInfo.BucketName=bucketName; Region=fields.[2]; Date=date; Key=s3obj.Key; })
                |> Seq.filter f
                |> Seq.sortBy(fun l -> l.Date)

        let downloadLog (l : CloudTrailFileLogInfo) dest (c : AmazonS3Client) =                                
                GetObjectRequest(BucketName=l.BucketName, Key=l.Key) |> c.GetObject |> (fun r -> r.WriteResponseStreamToFile(dest))
        
        let getLog (c : AmazonS3Client) (l : CloudTrailFileLogInfo)  = 
                use s = c.GetObject(GetObjectRequest(BucketName=l.BucketName, Key=l.Key)).ResponseStream                
                GZip.readFromStream s                              

        let getLogFileInfos c bucketName = 
                getLogFileInfosBy c bucketName 
                
        let sinceDays (day : int) (l : CloudTrailFileLogInfo) = (l.Date >= DateTime.Now.Subtract(new TimeSpan(day,0,0,0,0)))             
            
        let QueryEventsToday = sinceDays 0
        let QueryEventsSinceYesterday = sinceDays 1
        let QueryEventsInTheLast2Days = sinceDays 2
        let QueryEventsInTheLast7Days = sinceDays 7 
        let QueryEventsInTheLast30Days = sinceDays 30
         
        let downloadLogFilesBy f (fs : CloudTrailFileLogInfo seq) (c : AmazonS3Client) = 
                (new TransferUtility(c)).Download(new TransferUtilityDownloadRequest() )                

        let getEventsFromFilesBy (filter : CloudTrailFileSchema.Record -> bool) (fileNames : string seq)  =                        
                seq { for fn in fileNames do yield! CloudTrailFileSchema.Load(fn).Records |> Seq.filter filter }
        
        type CloudTrailQueryRequest(s3client : AmazonS3Client,
                                    bucketName : string,                                    
                                    dateFilter : CloudTrailFileLogInfo -> bool,
                                    ?eventFilter : CloudTrailFileSchema.Record -> bool) =                     
                    member __.S3Client with get() = s3client 
                    member __.BucketName with get() = bucketName
                    member __.FilterDatesBy with get() = dateFilter
                    member __.FilterEventsBy with get()  = eventFilter 

        let query (q : CloudTrailQueryRequest) = 
                getLogFileInfosBy q.S3Client q.BucketName q.FilterDatesBy
                |> Seq.toArray
                |> Array.Parallel.map(fun l -> let s = getLog q.S3Client l 
                                               CloudTrailFileSchema.Parse(s).Records)
                |> Array.concat
                |> Array.filter(fun e -> match(q.FilterEventsBy) with
                                         | Some f -> f(e) 
                                         | None -> true)

module FCloudTrailStats = 
            let numberOfEventsBy f (es : CloudTrailFileSchema.Record seq) = es |> Seq.groupBy f |> Seq.map(fun (g, es) -> g, Seq.length es)
            let numberOfEventsByEventSource es = numberOfEventsBy (fun e -> e.EventSource) es
            let numberOfEventsByEventName es =  numberOfEventsBy (fun e -> e.EventName) es                             
            let numberOfEventsByEventType es = numberOfEventsBy (fun e -> e.EventType) es   
            let numberOfEventsByRegion es = numberOfEventsBy (fun e -> e.AwsRegion) es                                        
            let numberOfEventsPerMonth es = numberOfEventsBy (fun e -> new DateTime(e.EventTime.Year, e.EventTime.Month, e.EventTime.Day)) es           
            let numberOfEventsPerDay es = numberOfEventsBy (fun e -> new DateTime(e.EventTime.Year, e.EventTime.Month, e.EventTime.Day)) es
            let numberOfEventsPerHour es = numberOfEventsBy (fun e -> new DateTime(e.EventTime.Year, e.EventTime.Month, e.EventTime.Day, e.EventTime.Hour, 0,0)) es

