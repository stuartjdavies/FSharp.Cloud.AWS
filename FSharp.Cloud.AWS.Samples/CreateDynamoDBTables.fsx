#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"
(*** hide ***)
(**
Example of creating different indexes in DynamoDB
=================================================

Global secondary index — an index with a hash and range key that can be different from those on the table. 
A global secondary index is considered "global" because queries on the index can span all of the data in a table, across all partitions.

Local secondary index — an index that has the same hash key as the table, but a different range key. 
A local secondary index is "local" in the sense that every partition of a local secondary index is scoped to a table partition that has the same hash key.

*)
open FSharp.Cloud.AWS
open FSharp.Cloud.AWS.AwsUtils
open FSharp.Cloud.AWS.DSL
open Amazon.DynamoDBv2.Model
open Amazon

let dynamoDb = FDynamoDB.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2

(** Create a list of new tables to create *)
let requests = [ CreateDynamoDbTableRequest(tableName="PurchaseOrders", columnTypes = Map [ "Id", ScalarTypeString ],
                                            primaryKey = Hash "Id", provisionedCapacity=Standard);
                 CreateDynamoDbTableRequest(tableName="SalesOrders", columnTypes = Map [ "Id", ScalarTypeString ; "DateSold", ScalarTypeString ],                          
                                            primaryKey = HashAndRange("Id", "DateSold"), provisionedCapacity=Standard);                                        
                 CreateDynamoDbTableRequest(tableName="MusicCollection", 
                                            columnTypes = Map [ "Artist", ScalarTypeString; "SongTitle", ScalarTypeString; "AlbumTitle", ScalarTypeString ],                          
                                            primaryKey = HashAndRange("Artist", "SongTitle"),                                                             
                                            localIndexes = IndexList({ LocalIndex.Name="AlbumTitleIndex"; 
                                                                       Index=HashAndRange("Artist", "AlbumTitle"); 
                                                                       NonKeyAttributes= Set ["Genre"; "Year"];
                                                                       ProjectionType=IncludeOnly }));
                 CreateDynamoDbTableRequest(tableName="WeatherData",
                                            columnTypes = Map [ "Location", ScalarTypeString; 
                                                                "Date", ScalarTypeString; 
                                                                "Precipitation", ScalarTypeNumber ],
                                            primaryKey = HashAndRange("Location", "Date"),                                                           
                                            globalIndexes = IndexList({ GlobalIndex.Name="PrecipIndex";
                                                                        Index=HashAndRange("Date", "Precipitation");                                                
                                                                        ProjectionType=All;
                                                                        NonKeyAttributes=Set.empty;
                                                                        ProvisionedCapacity=Standard })) ]

let responses = requests |> List.map dynamoDb.CreateDynamoDbTable
                         
responses |> List.iter(fun r -> printfn "Created Table %s" r.TableDescription.TableName)

responses |> List.map(fun r -> DeleteTableRequest(r.TableDescription.TableName) |> dynamoDb.DeleteTable)
          |> List.iter(fun r -> printfn "Deleted Table %s" r.TableDescription.TableName)