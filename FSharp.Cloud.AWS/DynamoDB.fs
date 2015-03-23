namespace FSharp.Cloud.AWS

open System
open System.Collections.Generic
open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.Model
open Amazon.Runtime
open Amazon.Util
open System.Threading
open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.DocumentModel
open FSharp.Cloud.AWS.AwsUtils
open FSharp.Cloud.AWS.RequirementsChecker
 
// 
// DynamoDB Table schema requirements
// ==================================
//
// - A table name must:
//          - must contain characters ([ a .. z], [ 0 .. 9])
//          - must start with a character
//          - must be from 1 to 20 characters              
// - A table must have a primary key defined
// - A primary key must be a hash or hash and range
// - The primary key must be a scalar type type e.g. string, number, boolean, binary
// - Two columns must not have the same name 
// - 0 - 5 local indexes can be created
// - 0 - 5 global indexes can be created
// - A column can be of the following data types:
//      - Scalar types – Number, String, Binary, Boolean, and Null.
//      - Multi-valued types – String Set, Number Set, and Binary Set.
//      - Document types – List, Map.
// - The table names must:
//        - must contain characters ([ a .. z], [ 0 .. 9])
//        - must start with a character
//        - must be from 1 to 20 characters 
//                                              
type ColumnName = string
type IndexName = string
type DynamoDBProvisionedCapacity = | Standard | Custom of readCapacity : int64 * writeCapacity : int64                                            
type DynamoDBIndexColumnType = | ScalarTypeNumber | ScalarTypeString | ScalarTypeBoolean | ScalarTypeBinary                        
type DynamoDBHashIndex = | Hash of cn : ColumnName 
                         | HashAndRange of hashName : ColumnName * rangeName : ColumnName
type DynamoDBLocalIndex = string * ColumnName 
type DynamoDBProjectionType = | KeysOnly | IncludeOnly | All
type NonKeyAttributes = string Set
type ColumnTypeMap = Map<ColumnName, DynamoDBIndexColumnType>

type GlobalIndex = { Name : IndexName; 
                     Index : DynamoDBHashIndex;                     
                     ProjectionType : DynamoDBProjectionType; 
                     NonKeyAttributes : NonKeyAttributes;
                     ProvisionedCapacity : DynamoDBProvisionedCapacity }

type LocalIndex = { Name : IndexName;
                    Index : DynamoDBHashIndex; 
                    NonKeyAttributes : NonKeyAttributes;
                    ProjectionType : DynamoDBProjectionType }

type IndexList<'T> = ListFromZeroToFive<'T>                   

type DynamoDBTableSchema = {
        TableName : string;
        Columns : ColumnTypeMap;
        PrimaryKey : DynamoDBHashIndex;        
        GlobalSecondaryIndexes : IndexList<GlobalIndex>;
        LocalSecondaryIndexes : IndexList<LocalIndex>;
        ProvisionedCapacity : DynamoDBProvisionedCapacity;                
}


type DymamoDBTableSchemaRequirementCheck = | ValidTableSchema | InvalidTableSchema of reason : string

type DynamoDBTableSchemaRequirement = (DynamoDBTableSchema -> DymamoDBTableSchemaRequirementCheck)
  
type QueryExpr = 
     | Between of c : ColumnName * val1 : obj * val2 : obj 
     | GreaterThan of c : ColumnName * val1 : obj
     | LessThan of c : ColumnName * val1 : obj     
     | And of e1 : QueryExpr * e2 : QueryExpr
     | Or of e1  : QueryExpr * e2 : QueryExpr    
     static member (<&&>) (e1, e2) = And(e1,e2) 
     static member (<||>) (e1, e2) = Or(e1,e2)       

type DynamoDbScan = { From : String; Where : QueryExpr }
            
module DynamoDBTableSchemaValidator =           
           let holdsTrueForAllItems (cond : ('a -> bool)) (xs : 'a seq) =                        
                    not (xs |> Seq.exists(fun x -> not (cond(x))))   

           let doesHashIndexColumnsExist (index : DynamoDBHashIndex) (columns : ColumnTypeMap) = 
                    match index with
                    | Hash(h) -> columns.ContainsKey(h)
                    | HashAndRange(h,r) -> columns.ContainsKey(h) && 
                                             columns.ContainsKey(r)

           let doesStringHaveOnlyLettersAndDigits (s : string) = 
                    s |> Seq.toList |> holdsTrueForAllItems Char.IsLetterOrDigit     
           
           let doesColumnNamesContainsValidCharacters s = 
                    s.GlobalSecondaryIndexes.Indexes 
                    |> Seq.map (fun index -> index.Index)        
                    |> holdsTrueForAllItems(fun index -> doesHashIndexColumnsExist index s.Columns )                  
                                                  
           let doGlobalIndexColumnNamesExist s = 
                    s.GlobalSecondaryIndexes.Indexes 
                    |> Seq.map (fun index -> index.Index)        
                    |> holdsTrueForAllItems(fun index -> doesHashIndexColumnsExist index s.Columns )
           
           let SchemaRequirements : (ReqCondition<'a> * ReqValidationMessage) list = 
                [ "Schema must have a least one column defined" => (fun s -> s.Columns |> Seq.length >= 1) 
                  "Table name length must be 3 to 30 characters" => (fun s -> s.TableName.Length > 3 && s.TableName.Length > 30)                   
                  "Table name contains invalid characters" => (fun s -> doesColumnNamesContainsValidCharacters s)                                   
                  "Table names can only contain letter and digits" => (fun s -> doesStringHaveOnlyLettersAndDigits(s.TableName))                      
                  "Primary key doesn't exist in columns" =>  (fun s -> doesHashIndexColumnsExist s.PrimaryKey s.Columns) 
                  "Global index column doesn't exist" => (fun s -> doGlobalIndexColumnNamesExist(s)) ]
                        
           let isValid (s : DynamoDBTableSchema) =
                    RequirementsChecker.check s SchemaRequirements

// 
// Amazon client factories
//
module FDynamoDB = 
            let createClientFromCsvFile fileName (region : Amazon.RegionEndpoint) =                    
                     let accessKey, secretAccessKey = AwsUtils.getCredFromCsvFile fileName
                     new AmazonDynamoDBClient(accessKey, secretAccessKey, region)
                      
            //
            // Helper methods
            //                         
            let getAttributeValue (v : obj) = 
                                    match v with
                                    | :? int as n -> AttributeValue(N=n.ToString())  
                                    | :? float as n -> AttributeValue(N=n.ToString())  
                                    | :? decimal as n -> AttributeValue(N=n.ToString())   
                                    | :? string as s -> AttributeValue(S=s)
                                    | :? DateTime as d -> AttributeValue(S=d.ToString())
                                    | _ -> raise(Exception("Unsupported data type"))
                                
            let doesHashIndexColumnsExist (index : DynamoDBHashIndex) (columns : ColumnTypeMap) = 
                    match index with
                    | Hash(h) -> columns.ContainsKey(h)
                    | HashAndRange(h,r) -> columns.ContainsKey(h) && 
                                             columns.ContainsKey(r)

            let seqToDic (s:('a * 'b) seq) = Dictionary<'a,'b>(s |> Map.ofSeq)   

            let getFilterExpr (q : QueryExpr) =                                     
                      let rec evalQExpr i e =              
                                match e with
                                | Between(c, v1, v2) -> (i + 2), sprintf "(%s between :val%d and :val%d)" c (i + 1) (i + 2)
                                | GreaterThan(c, v) -> (i + 1), sprintf "(%s > :val%d)" c (i + 1)                     
                                | LessThan(c, v) -> (i + 1), sprintf "(%s < :val%d)" c (i + 1)                     
                                | And(e1, e2) | Or(e1, e2)  -> let i1, s1 = evalQExpr i e1
                                                               let i2, s2 = evalQExpr i1 e2
                                                               i2, (sprintf "(%s and %s)" s1 s2)                    
                      evalQExpr 0 q |> snd

            let getFilterValues (q : QueryExpr) =                                              
                      let rec evalQExpr i e =              
                                match e with
                                | Between(c, v1, v2) -> (i + 2), [ (sprintf ":val%d" (i + 1)), getAttributeValue v1
                                                                   (sprintf ":val%d" (i + 2)), getAttributeValue v2]
                                | GreaterThan(c, v) | LessThan(c, v) ->  (i + 1), [ (sprintf ":val%d" (i + 1)), getAttributeValue v ]
                                | And(e1, e2) | Or(e1, e2) -> let i1, vs1 = evalQExpr i e1
                                                              let i2, vs2 = evalQExpr i1 e2
                                                              i2, vs1 @ vs2                                                                                                                             
                      evalQExpr 0 q |> snd

            let runScan (c : AmazonDynamoDBClient) q =
                   let sr = ScanRequest()                                                                                                        
                   sr.TableName <- q.From 
                   sr.Select <- Select.ALL_ATTRIBUTES
                   sr.ExpressionAttributeValues <- getFilterValues q.Where |> seqToDic
                   sr.FilterExpression <- getFilterExpr q.Where
                   c.Scan(sr).Items                      

            let toDocument (rds : (string * DynamoDBEntry) seq ) =
                    Document((seqToDic rds))

            let toDynamoDbEntry (v : 'T) = 
                      let success, v = DynamoDBEntryConversion.V2.TryConvertToEntry(v)
                      if success = true then
                        v
                      else
                        raise(Exception("Invalid value"))  

            let inline (==>) (k : string) (v : 'T) = (k,  toDynamoDbEntry(v)) 

            let uploadToDynamoDB (tableName : string) (c : AmazonDynamoDBClient) (ds : Document array) =        
                    let msftStockPriceTable = Table.LoadTable(c, "MicrosoftStockPrices")                   
                    let batchWrite = msftStockPriceTable.CreateBatchWrite()        
                    ds |> Array.iter(fun d -> batchWrite.AddDocumentToPut(d))
                    batchWrite.Execute()
                                                                                                                                                                                   
            let createTable (c : AmazonDynamoDBClient) (s : DynamoDBTableSchema) =                               
                    let STANDARD_READ_CAPACITY_UNITS = (int64 5)
                    let STANDARD_WRITE_CAPACITY_UNITS = (int64 6)

                    let createProvisionThroughPut (c: DynamoDBProvisionedCapacity) =
                           match c with
                           | Standard -> ProvisionedThroughput(STANDARD_READ_CAPACITY_UNITS,
                                                                   STANDARD_WRITE_CAPACITY_UNITS)                               
                           | Custom(r, w) -> ProvisionedThroughput(r, w)                               
               
                    let createScalarAttributeType (t : DynamoDBIndexColumnType) =                
                            match t with
                            | ScalarTypeString  -> ScalarAttributeType.S 
                            | ScalarTypeNumber | ScalarTypeBoolean -> ScalarAttributeType.N                                               
                            | ScalarTypeBinary -> ScalarAttributeType.B                

                    let createProjectectionType (t : DynamoDBProjectionType) =
                            match t with
                            | KeysOnly -> ProjectionType.ALL
                            | IncludeOnly -> ProjectionType.INCLUDE
                            | All -> ProjectionType.ALL
                        
                    let createAttributeDefinitions() =                    
                               s.Columns 
                               |> Seq.map(fun kvps -> AttributeDefinition(kvps.Key, createScalarAttributeType kvps.Value))
                               |> (fun items -> List<_>(items))
                
                    let createKeySchema (index : DynamoDBHashIndex ) =                
                            (match index with
                             | Hash h -> [ KeySchemaElement(AttributeName=h, KeyType=KeyType.HASH) ]
                             | HashAndRange(h, r) -> [ KeySchemaElement(AttributeName=h,KeyType=KeyType.HASH);
                                                        KeySchemaElement(AttributeName=r,KeyType=KeyType.RANGE) ])                                                                        
                            |> (fun items -> List<_>(items))

                    let createLocalSecondaryIndexes() =
                           s.LocalSecondaryIndexes.Indexes
                           |> Seq.map(fun index -> LocalSecondaryIndex(IndexName=index.Name,
                                                                       KeySchema=createKeySchema(index.Index),
                                                                       Projection=Projection(ProjectionType=createProjectectionType(index.ProjectionType), 
                                                                                                   NonKeyAttributes=List<_>(index.NonKeyAttributes))))                                     
                           |> (fun items -> List<_>(items))                       

                    let createGlobalSecondaryIndexes() =
                            s.GlobalSecondaryIndexes.Indexes 
                            |> Seq.map(fun index -> let g=GlobalSecondaryIndex()
                                                    g.IndexName <- index.Name
                                                    g.KeySchema <- createKeySchema(index.Index)
                                                    g.Projection <- Projection(ProjectionType=createProjectectionType(index.ProjectionType), 
                                                                                   NonKeyAttributes=List<_>(index.NonKeyAttributes))
                                                    g.ProvisionedThroughput <- createProvisionThroughPut(Standard)
                                                    g)
                            |> (fun items -> List<_>(items))       
                                        
                    
                    CreateTableRequest(TableName=s.TableName, KeySchema=createKeySchema(s.PrimaryKey),
                                                             AttributeDefinitions=createAttributeDefinitions(),  
                                                             LocalSecondaryIndexes=createLocalSecondaryIndexes(),
                                                             GlobalSecondaryIndexes=createGlobalSecondaryIndexes(),                                  
                                                             ProvisionedThroughput=createProvisionThroughPut(s.ProvisionedCapacity))
                    |> c.CreateTable
                
        

            let waitUntilTableIsCreated tableName intervalMilliseconts (c : AmazonDynamoDBClient) =        
                    let rec aux(tblName) =                                  
                                Thread.Sleep(millisecondsTimeout=intervalMilliseconts) // Wait 5 seconds.
                                try                
                                    let res = c.DescribeTable(request=DescribeTableRequest(TableName=tableName))
                    
                                    printfn "Table name: %s, status: %s" res.Table.TableName (res.Table.TableStatus.ToString())
                                                                  
                                    if (res.Table.TableStatus = TableStatus.ACTIVE) then
                                       res.Table.TableStatus
                                    else
                                      aux(tblName)                                              
                                with
                                // DescribeTable is eventually consistent. So you might
                                // get resource not found. So we handle the potential exception.                    
                                | :? ResourceNotFoundException as re -> aux(tblName)         
                    aux(tableName)

            let deleteTable (c : AmazonDynamoDBClient) (tableName : string) =
                    c.DeleteTable tableName

            let getTableInfo (c : AmazonDynamoDBClient) tableName=  
                    c.DescribeTable(tableName=tableName).Table

            let getListOfTableNames (c : AmazonDynamoDBClient) =
                    c.ListTables().TableNames