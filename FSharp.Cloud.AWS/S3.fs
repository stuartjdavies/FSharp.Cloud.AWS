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
open FSharp.Cloud.AWS.AwsUtils

let createS3ClientFromCsvFile fileName =
        let accessKey, secretAccessKey = AwsUtils.getCredFromCsvFile fileName
        new AmazonS3Client(accessKey, secretAccessKey, Amazon.RegionEndpoint.APSoutheast2)  

let displayBuckets (c: AmazonS3Client) = 
        c.ListBuckets().Buckets          
        |> Seq.iteri(fun i b -> printfn "%d. %s\t%s" (i + 1) b.BucketName (b.CreationDate.ToShortDateString()))
        
let getNumberOfBuckets (c : AmazonS3Client) =
        c.ListBuckets().Buckets |> Seq.length

let createBucket (name : string) (c : AmazonS3Client) =                 
        new PutBucketRequest(BucketName=name)
        |> c.PutBucket

let deleteBucket (name : string) (c : AmazonS3Client) =         
        new DeleteBucketRequest(BucketName=name)
        |> c.DeleteBucket

let deleteAllBuckets (c : AmazonS3Client) =         
        c.ListBuckets().Buckets |> Seq.map(fun b -> deleteBucket b.BucketName c)

let getObjectsInBucket (c : AmazonS3Client) bucketName =                 
        c.ListObjects(new ListObjectsRequest(BucketName=bucketName)).S3Objects
        
let createTextPlainObject (c : AmazonS3Client) bucketName key text  =
        new PutObjectRequest(BucketName=bucketName, Key=key, ContentType="text/plain", ContentBody=text)
        |> c.PutObject

let getTextPlainObject (c : AmazonS3Client) bucketName key text =        
        let response = c.GetObject(new GetObjectRequest(BucketName=bucketName, Key=key))
        (new StreamReader(response.ResponseStream)).ReadToEnd()         

let doesBucketExist (c : AmazonS3Client) bucketName  =
        c.ListBuckets().Buckets |> Seq.exists(fun b -> b.BucketName = bucketName)

let doesObjectExist (s3Client : AmazonS3Client) bucketName objectName =        
        (new S3FileInfo(s3Client, bucketName, objectName)).Exists
                
let uploadFile (c : AmazonS3Client) bucketName filePath  =         
        (new TransferUtility(c)).Upload(filePath, bucketName)        

let downloadFile (c : AmazonS3Client) filePath bucketName key =         
        (new TransferUtility(c)).Download(filePath, bucketName, key)        

let downloadDirectory (c : AmazonS3Client) bucketName s3Dir destDir =        
        (new TransferUtility(c)).DownloadDirectory(bucketName, s3Dir, destDir)        

let uploadDirectory (c : AmazonS3Client) bucketName srcDir  =
        (new TransferUtility(c)).UploadDirectory(bucketName, srcDir)        
    
    


