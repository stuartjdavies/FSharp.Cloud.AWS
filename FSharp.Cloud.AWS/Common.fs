namespace FSharp.Cloud.AWS

open FSharp.Data
open System.Xml
open System.Xml.Linq
open System

type AWSCred = CsvProvider<"""AwsCredentials.csv""">

module AWSCredentials =         
        let parseCsv (fileName : string) =                  
            let cred = (AWSCred.Load(fileName).Rows |> Seq.nth 0) 
            cred.``Access Key Id``, cred.``Secret Access Key``  
            

type AWSResponseFailure =
     | AWSException of Exception

type AWSResponse<'T> =
        | Success of 'T
        | Failure of AWSResponseFailure


