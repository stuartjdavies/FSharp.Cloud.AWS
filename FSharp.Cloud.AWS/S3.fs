module FSharp.Cloud.AWS.Fs3

open System
open System.Collections.Generic
open Amazon.S3
open Amazon.Runtime
open Amazon.SecurityToken
open Amazon.S3.Model
open System.IO
open Amazon.S3.Transfer
open Amazon.S3.IO
open System.Runtime.CompilerServices
open FSharp.Cloud.AWS

let createS3ClientFromCsvFile fileName =
        let accessKey, secretAccessKey = AWSCredentials.parseCsv fileName
        new AmazonS3Client(accessKey, secretAccessKey, Amazon.RegionEndpoint.APSoutheast2)


let ConvertToAwsResponse(r : _ ) =  Success(r())  

let displayBuckets (c: AmazonS3Client) = 
        (fun () -> c.ListBuckets().Buckets          
                   |> Seq.iteri(fun i b -> printfn "%d. %s\t%s" (i + 1) b.BucketName (b.CreationDate.ToShortDateString())))
        |> ConvertToAwsResponse

let getNumberOfBuckets (c : AmazonS3Client) =
        (fun () -> c.ListBuckets().Buckets |> Seq.length) |> ConvertToAwsResponse

let createBucket (name : string) (c : AmazonS3Client) =         
        (fun () -> c.PutBucket(new PutBucketRequest(BucketName=name))) |> ConvertToAwsResponse

let deleteBucket (name : string) (c : AmazonS3Client) = 
        (fun () -> c.DeleteBucket(new DeleteBucketRequest(BucketName=name))) |> ConvertToAwsResponse

let deleteAllBuckets (c : AmazonS3Client) = 
        (fun () -> c.ListBuckets().Buckets |> Seq.map(fun b -> deleteBucket b.BucketName c)) |> ConvertToAwsResponse

let getObjectsInBucket bucketName (c : AmazonS3Client) =         
        (fun () -> c.ListObjects(new ListObjectsRequest(BucketName=bucketName)).S3Objects) |> ConvertToAwsResponse
        
let createTextPlainObject bucketName key text (c : AmazonS3Client) =
        (fun () -> c.PutObject(new PutObjectRequest(BucketName=bucketName, Key=key, ContentType="text/plain", ContentBody=text)))
        |> ConvertToAwsResponse

let getTextPlainObject bucketName key text (c : AmazonS3Client) =
        (fun () -> let response = c.GetObject(new GetObjectRequest(BucketName=bucketName, Key=key))
                   (new StreamReader(response.ResponseStream)).ReadToEnd())
        |> ConvertToAwsResponse 

let doesBucketExist bucketName (c : AmazonS3Client) =
        (fun () -> c.ListBuckets().Buckets |> Seq.exists(fun b -> b.BucketName = bucketName))
        |> ConvertToAwsResponse 

let doesObjectExist bucketName objectName (s3Client : AmazonS3Client) =
        (fun () -> (new S3FileInfo(s3Client, bucketName, objectName)).Exists)
        |> ConvertToAwsResponse 
        
let uploadFile bucketName filePath (c : AmazonS3Client) = 
        (fun () -> (new TransferUtility(c)).Upload(filePath, bucketName))
        |> ConvertToAwsResponse 

let downloadFile filePath bucketName key (c : AmazonS3Client) = 
        (fun () -> (new TransferUtility(c)).Download(filePath, bucketName, key))
        |> ConvertToAwsResponse 

let downloadDirectory bucketName s3Dir destDir (c : AmazonS3Client) =
        (fun () -> (new TransferUtility(c)).DownloadDirectory(bucketName, s3Dir, destDir))
        |> ConvertToAwsResponse 

let uploadDirectory bucketName srcDir (c : AmazonS3Client) =
        (fun () -> (new TransferUtility(c)).UploadDirectory(bucketName, srcDir))
        |> ConvertToAwsResponse
    
    


