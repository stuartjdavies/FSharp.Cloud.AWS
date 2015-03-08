(*** hide ***)
#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"

(**
Import Stock Data into Dynamo DB
================================
*)
open System
open Amazon.Util
open FSharp.Cloud.AWS
open FSharp.Cloud.AWS.FDynamoDB
open FSharp.Data

(** Get microsoft stock data **)
type Stocks = CsvProvider<"C:\Users\stuart\Documents\GitHub\FSharp.CodeSnippets\FSharp.CodeSnippets.AWS\Data\YahooStockPriceSchema.csv">
let msft = Stocks.Load("http://ichart.finance.yahoo.com/table.csv?s=MSFT").Cache()
let msftRows = msft.Rows |> Seq.take 1000 |> Seq.toArray

(** Create Amazon Client string **)
let dynamoDbClient = FDynamoDB.createDynamoDbClientFromCsvFile """c:\AWS\Stuart.Credentials.csv"""

(** Create Dynamo DB Database in the cloud **)
{ DynamoDBTableSchema.TableName = "MicrosoftStockPrices";
                      Columns = Map [ ("ODate", ScalarTypeString) ];                            
                      PrimaryKey = Hash("ODate");                                  
                      ProvisionedCapacity=Standard;
                      GlobalSecondaryIndexes=Set.empty;
                      LocalSecondaryIndexes=Set.empty } 
|> FDynamoDB.createTable dynamoDbClient 

dynamoDbClient |> FDynamoDB.waitUntilTableIsCreated "MicrosoftStockPrices" 3000

(** Insert Microsoft's stock prices in the DynamoDB NoSql Database **)
msftRows
|> Array.Parallel.map(fun row -> FDynamoDB.toDocument [ "ODate" ==> row.Date.ToString(AWSSDKUtils.ISO8601DateFormat)
                                                        "OpenPrice" ==> row.Open
                                                        "HighPrice" ==> row.High
                                                        "LowPrice" ==> row.Low
                                                        "ClosePrice" ==> row.Close 
                                                        "Volume" ==> row.Volume 
                                                        "AdjClose" ==> row.``Adj Close`` ])                                                                         
|> FDynamoDB.uploadToDynamoDB "MicrosoftStockPrices" dynamoDbClient

(** Run a query **)
let query = { DynamoDbScan.From="MicrosoftStockPrices";
                           Where=(Between("OpenPrice", 45, 46) <&&> 
                                  Between("ClosePrice", 45, 45.5) <&&>
                                  GreaterThan("AdjClose", 44.8)) }

(** Query DynamoDB **)            
query |> FDynamoDB.runScan dynamoDbClient
      |> Seq.iteri(fun i item -> printfn "%d. Date - %s, Open - %s, Close - %s, Adj. Close=%s"
                                                i item.["ODate"].S item.["OpenPrice"].N 
                                                  item.["ClosePrice"].N item.["AdjClose"].N)

    
(** Print the Table Summary **)
let info = FDynamoDB.getTableInfo "MicrosoftStockPrices" dynamoDbClient 
printfn "Table Summary"
printfn "-------------"
printfn "Name: %s" info.TableName
printfn "# of items: %d" info.ItemCount
printfn "Provision Throughput (reads/sec): %d" info.ProvisionedThroughput.ReadCapacityUnits
printfn "Provision Throughput (writes/sec): %d" info.ProvisionedThroughput.WriteCapacityUnits
                        
(** Run a query on the data **)                                                           
"MicrosoftStockPrices" |> FDynamoDB.deleteTable dynamoDbClient   