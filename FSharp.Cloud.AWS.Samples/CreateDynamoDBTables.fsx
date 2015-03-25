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
open Amazon

let dynamoDbClient= FDynamoDB.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2

(** Create a list of new tables to create *)
let newTables = [ { DynamoDBTableSchema.TableName = "PurchaseOrders";
                                        Columns = Map [ "Id", ScalarTypeString ];
                                        PrimaryKey = Hash "Id";  
                                        ProvisionedCapacity=Standard;
                                        GlobalSecondaryIndexes = IndexList.empty;
                                        LocalSecondaryIndexes = IndexList.empty }; 
                  { DynamoDBTableSchema.TableName = "SalesOrders";
                                        Columns = Map [ "Id", ScalarTypeString ; 
                                                        "DateSold", ScalarTypeString ];                          
                                        PrimaryKey = HashAndRange("Id", "DateSold");                                                                          
                                        ProvisionedCapacity=Standard;  
                                        GlobalSecondaryIndexes = IndexList.empty;
                                        LocalSecondaryIndexes = IndexList.empty;  }; 
                  { DynamoDBTableSchema.TableName = "MusicCollection";
                                        Columns = Map [ "Artist", ScalarTypeString; 
                                                        "SongTitle", ScalarTypeString; 
                                                        "AlbumTitle", ScalarTypeString ];                           
                                        PrimaryKey = HashAndRange("Artist", "SongTitle");                                  
                                        ProvisionedCapacity=Standard;
                                        GlobalSecondaryIndexes = IndexList.empty;
                                        LocalSecondaryIndexes = IndexList({ LocalIndex.Name="AlbumTitleIndex";
                                                                                   Index=HashAndRange("Artist", "AlbumTitle"); 
                                                                                   NonKeyAttributes= Set ["Genre"; "Year"];
                                                                                   ProjectionType=IncludeOnly }) }; 
                  { DynamoDBTableSchema.TableName = "WeatherData";
                                        Columns = Map [ "Location", ScalarTypeString; 
                                                        "Date", ScalarTypeString; 
                                                        "Precipitation", ScalarTypeNumber ];                            
                                        PrimaryKey = HashAndRange("Location", "Date");                                  
                                        ProvisionedCapacity=Standard;
                                        GlobalSecondaryIndexes = IndexList({ GlobalIndex.Name="PrecipIndex";
                                                                                      Index=HashAndRange("Date", "Precipitation");                                                
                                                                                      ProjectionType=All;
                                                                                      NonKeyAttributes= Set.empty;
                                                                                      ProvisionedCapacity=Standard }); 
                                        LocalSecondaryIndexes = IndexList.empty }; ]

newTables |> Seq.map(fun t -> t |> FDynamoDB.createTable dynamoDbClient) 
          |> Seq.iter(fun r -> printfn "Created Table %s" r.TableDescription.TableName)

(** Delete the tables from dynamoDb **)
newTables |> Seq.map(fun t -> t.TableName |> FDynamoDB.deleteTable dynamoDbClient)
          |> Seq.iter(fun r -> printfn "Deleted Table %s" r.TableDescription.TableName)

