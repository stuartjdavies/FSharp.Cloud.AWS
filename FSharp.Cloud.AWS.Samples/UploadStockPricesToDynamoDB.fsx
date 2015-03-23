(*** hide ***)
#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"

(**
Import Stock Data into Dynamo DB
================================

Step 1: Get Microsoft Stock data using CsvTypeProvider
Step 2: Create a DynamoDB table 
Step 3: Import Data
Step 4: Run a query
Step 5: Print table statistics
Step 6: Delete the table
*)
open System
open Amazon.Util
open FSharp.Cloud.AWS
open FSharp.Cloud.AWS.FDynamoDB
open FSharp.Cloud.AWS.AwsUtils
open FSharp.Data
open Amazon

(** Step 1: Get Microsoft Stock data using CsvTypeProvider **)
type Stocks = CsvProvider<"C:\Users\stuart\Documents\GitHub\FSharp.CodeSnippets\FSharp.CodeSnippets.AWS\Data\YahooStockPriceSchema.csv">
let msft = Stocks.Load("http://ichart.finance.yahoo.com/table.csv?s=MSFT").Cache()
let msftRows = msft.Rows |> Seq.take 1000 |> Seq.toArray

let dynamoDbClient = FDynamoDB.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2

(** Step 2: Create a DynamoDB table  **)
{ DynamoDBTableSchema.TableName = "MicrosoftStockPrices";
                      Columns = Map [ "ODate", ScalarTypeString ];                            
                      PrimaryKey = Hash "ODate";                                  
                      ProvisionedCapacity=Standard;
                      GlobalSecondaryIndexes=IndexList.empty;
                      LocalSecondaryIndexes=IndexList.empty } 
|> FDynamoDB.createTable dynamoDbClient

FDynamoDB.waitUntilTableIsCreated "MicrosoftStockPrices" 3000 dynamoDbClient


(** Step 3: Import Data **)
msftRows
|> Array.Parallel.map(
            fun row -> FDynamoDB.toDocument [ "ODate" ==> row.Date.ToString(AWSSDKUtils.ISO8601DateFormat)
                                              "OpenPrice" ==> row.Open
                                              "HighPrice" ==> row.High
                                              "LowPrice" ==> row.Low
                                              "ClosePrice" ==> row.Close 
                                              "Volume" ==> row.Volume 
                                              "AdjClose" ==> row.``Adj Close`` ])                                                                         
|> FDynamoDB.uploadToDynamoDB "MicrosoftStockPrices" dynamoDbClient
                                                   
(** Run a query **)
{ DynamoDbScan.From="MicrosoftStockPrices";
               Where=(Between("OpenPrice", 45, 46) <&&> 
                      Between("ClosePrice", 45, 45.5) <&&>
                      GreaterThan("AdjClose", 44.8)) }
|> FDynamoDB.runScan dynamoDbClient
|> Seq.iteri(fun i item -> printfn "%d. Date - %s, Open - %s, Close - %s, Adj. Close=%s"
                                                    i item.["ODate"].S item.["OpenPrice"].N 
                                                      item.["ClosePrice"].N item.["AdjClose"].N)                        
    
(** Step 5: Print table statistics **)
let info = "MicrosoftStockPrices" |> FDynamoDB.getTableInfo dynamoDbClient 
printfn "Table Summary"
printfn "-------------"
printfn "Name: %s" info.TableName
printfn "# of items: %d" info.ItemCount
printfn "Provision Throughput (reads/sec): %d" info.ProvisionedThroughput.ReadCapacityUnits
printfn "Provision Throughput (writes/sec): %d" info.ProvisionedThroughput.WriteCapacityUnits
                        
(** Step 6: Delete the table **)                                                           
"MicrosoftStockPrices" |> FDynamoDB.deleteTable dynamoDbClient   