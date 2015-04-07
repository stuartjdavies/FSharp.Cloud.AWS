namespace FSharp.Cloud.AWS

open FSharp.Data
open System.Xml
open System.Xml.Linq
open System
open System.IO
open ICSharpCode.SharpZipLib.Core;
open ICSharpCode.SharpZipLib.GZip;
open System.Text
open System.Reflection
open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.DocumentModel

module DSL =
    let (!!) (xs : 'a seq) = new System.Collections.Generic.List<'a>(xs)
    let (!~) (x : 'a) = System.Collections.Generic.List<'a>((Seq.singleton x)) 

    let SendTo client request = let methodName = request.GetType().Name.Replace("Request", String.Empty)
                                let m = client.GetType().GetMethod(methodName) 
                                m.Invoke(client, [|request|])

    let TrySendTo client request = try
                                       let methodName = request.GetType().Name.Replace("Request", String.Empty)
                                       let m = client.GetType().GetMethod(methodName) 
                                       AwsRequestSuccessResult(m.Invoke(client, [|request|]))
                                   with 
                                   | ex -> AwsRequestFailureResult(AwsException(ex))
       
    type Amazon.DynamoDBv2.AmazonDynamoDBClient with 
            member this.CreateDynamoDbTable r = r |> FDynamoDB.createDynamoDbTable this
            member this.UploadDocuments tableName ds = FDynamoDB.uploadDocuments tableName this ds
            member this.Scan c tableName q = FDynamoDB.scan this tableName                
                                                   
    let mutable DynamoDB_DSL_Client : AmazonDynamoDBClient Option = None
     
    let Set_DynamoDB_Client(fileName, region : Amazon.RegionEndpoint) =                    
                     let accessKey, secretAccessKey = AwsUtils.getCredFromCsvFile fileName
                     DynamoDB_DSL_Client <- Some(new AmazonDynamoDBClient(accessKey, secretAccessKey, region))

    let Print_Table_Info tableName = 
                match DynamoDB_DSL_Client with
                | Some c -> let info = c.DescribeTable(tableName=tableName).Table
                            printfn "Table Summary"
                            printfn "-------------"
                            printfn "Name: %s" info.TableName
                            printfn "# of items: %d" info.ItemCount
                            printfn "Provision Throughput (reads/sec): %d" info.ProvisionedThroughput.ReadCapacityUnits
                            printfn "Provision Throughput (writes/sec): %d" info.ProvisionedThroughput.WriteCapacityUnits 
                | None -> raise(new Exception("Amazon Dynamo DB Client must be set"))                       
        
    let Delete_Table (name : string) =  
                match DynamoDB_DSL_Client with
                | Some c -> c.DeleteTable name                          
                | None -> raise(new Exception("Amazon Dynamo DB Client must be set"))  

    let Aws = new AwsRequestBuilder()



