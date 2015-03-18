#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"
(*** hide ***)
(**
FsLab Experiment
================
*)
open FSharp.Cloud.AWS
open FSharp.Cloud.AWS.AwsUtils

let dynamoDbClient= FDynamoDB.createDynamoDbClientFromCsvFile("""c:\AWS\Stuart.Credentials.csv""")

(** Create a list of new tables to create *)
let newTables = [ { DynamoDBTableSchema.TableName = "PurchaseOrders";
                                        Columns = Map [ ("Id", ScalarTypeString)];
                                        PrimaryKey = Hash("Id");  
                                        ProvisionedCapacity=Standard;
                                        GlobalSecondaryIndexes =  IndexList.empty;
                                        LocalSecondaryIndexes = IndexList.empty }; 
                  { DynamoDBTableSchema.TableName = "SalesOrders";
                                        Columns = Map [ ("Id", ScalarTypeString); 
                                                        ("DateSold", ScalarTypeString) ];                          
                                        PrimaryKey = HashAndRange("Id", "DateSold");                                                                          
                                        ProvisionedCapacity=Standard;  
                                        GlobalSecondaryIndexes = IndexList.empty;
                                        LocalSecondaryIndexes = IndexList.empty;  }; 
                  { DynamoDBTableSchema.TableName = "MusicCollection";
                                        Columns = Map [ ("Artist", ScalarTypeString); 
                                                        ("SongTitle", ScalarTypeString); 
                                                        ("AlbumTitle", ScalarTypeString) ];                           
                                        PrimaryKey = HashAndRange("Artist", "SongTitle");                                  
                                        ProvisionedCapacity=Standard;
                                        GlobalSecondaryIndexes = IndexList.empty;
                                        LocalSecondaryIndexes = IndexList({ LocalIndex.Name="AlbumTitleIndex";
                                                                                   Index=HashAndRange("Artist", "AlbumTitle"); 
                                                                                   NonKeyAttributes= Set ["Genre"; "Year"];
                                                                                   ProjectionType=IncludeOnly }) }; 
                  { DynamoDBTableSchema.TableName = "WeatherData";
                                        Columns = Map [ ("Location", ScalarTypeString); 
                                                        ("Date", ScalarTypeString); 
                                                        ("Precipitation", ScalarTypeNumber) ];                            
                                        PrimaryKey = HashAndRange("Location", "Date");                                  
                                        ProvisionedCapacity=Standard;
                                        GlobalSecondaryIndexes = IndexList({ GlobalIndex.Name="PrecipIndex";
                                                                                      Index=HashAndRange("Date", "Precipitation");                                                
                                                                                      ProjectionType=All;
                                                                                      NonKeyAttributes= Set [];
                                                                                      ProvisionedCapacity=Standard }); 
                                        LocalSecondaryIndexes = IndexList.empty }; ]

newTables |> Seq.map(fun t -> t |> FDynamoDB.createTable dynamoDbClient) 
          |> Seq.iter(fun r -> printfn "Created Table %s" r.TableDescription.TableName)

(** Delete the tables from dynamoDb **)
newTables |> Seq.map(fun t -> t.TableName |> FDynamoDB.deleteTable dynamoDbClient)
          |> Seq.iter(fun r -> printfn "Deleted Table %s" r.TableDescription.TableName)

