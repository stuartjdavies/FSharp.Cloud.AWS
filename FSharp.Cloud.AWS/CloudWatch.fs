namespace FSharp.Cloud.AWS

open System
open System.Collections.Generic
open Amazon.S3
open Amazon.S3.Model
open Amazon.Runtime
open Amazon.SecurityToken
open Amazon.CloudWatch
open Amazon.CloudWatch.Model
open Amazon.CloudWatchLogs
open Amazon.CloudWatchLogs.Model

module FCloudWatch =
        let createClientFromCsvFile fileName (region : Amazon.RegionEndpoint) =
                let accessKey, secretAccessKey = AwsUtils.getCredFromCsvFile fileName
                new AmazonCloudWatchClient(accessKey, secretAccessKey, region)      

module FCloudWatchLogs =
        let createClientFromCsvFile fileName (region : Amazon.RegionEndpoint) =
                let accessKey, secretAccessKey = AwsUtils.getCredFromCsvFile fileName
                new AmazonCloudWatchLogsClient(accessKey, secretAccessKey, region)

