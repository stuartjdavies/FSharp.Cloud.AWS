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
                                              
//
// Define the domain model                                                                                                       
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
                    
type DynamoDBTableSchema = {
        TableName : string;
        Columns : ColumnTypeMap;
        PrimaryKey : DynamoDBHashIndex;        
        GlobalSecondaryIndexes : GlobalIndex Set;
        LocalSecondaryIndexes : LocalIndex Set;
        ProvisionedCapacity : DynamoDBProvisionedCapacity;                
}


type DymamoDBTableSchemaRequirementCheck = | ValidTableSchema | InvalidTableSchema of reason : string

type DynamoDBTableSchemaRequirement = (DynamoDBTableSchema -> DymamoDBTableSchemaRequirementCheck)
  
type QExpr = 
     | Between of c : ColumnName * val1 : obj * val2 : obj 
     | GreaterThan of c : ColumnName * val1 : obj
     | LessThan of c : ColumnName * val1 : obj     
     | And of e1 : QExpr * e2 : QExpr
     | Or of e1  : QExpr * e2 : QExpr    
     static member (<&&>) (e1, e2) = And(e1,e2) 
     static member (<||>) (e1, e2) = Or(e1,e2)       

type DynamoDbScan = { From : String; Where : QExpr }
          
// 
// Amazon client factories
//
module FDynamoDB = 
            let createDynamoDbClientFromCsvFile fileName =
                    let accessKey, secretAccessKey = AWSCredentials.parseCsv fileName
                    new AmazonDynamoDBClient(accessKey, secretAccessKey, Amazon.RegionEndpoint.APSoutheast2)
                      
            //
            // Helper methods
            // 
            let rec doesTableSchemaPassReqs (s : DynamoDBTableSchema) 
                                            (rs : DynamoDBTableSchemaRequirement list) =                                         
                     match rs with
                     | h::t -> let result = h(s) 
                               match result with
                               | ValidTableSchema -> doesTableSchemaPassReqs s t
                               | InvalidTableSchema(r) -> result
                     | [] -> ValidTableSchema
                    
            let getTableSchemaValidationErrors (s : DynamoDBTableSchema) 
                                               (rs : DynamoDBTableSchemaRequirement list) =
                    rs |> List.map(fun r -> r(s))

            let holdsTrueForAllItems (cond : ('a -> bool)) (xs : 'a seq) =                        
                    not (xs |> Seq.exists(fun x -> not (cond(x))))             

            let doesStringHaveOnlyLettersAndDigits(s : string) = 
                    s |> Seq.toList |> holdsTrueForAllItems Char.IsLetterOrDigit     
        
            let returnValidationResult errorMsg result = 
                            if (result = true) then
                              ValidTableSchema
                            else
                              InvalidTableSchema errorMsg  

            let getAttributeValue (v : obj) = 
                                    match v with
                                    | :? int as n -> new AttributeValue(N=n.ToString())  
                                    | :? float as n -> new AttributeValue(N=n.ToString())  
                                    | :? decimal as n -> new AttributeValue(N=n.ToString())   
                                    | :? string as s -> new AttributeValue(S=s)
                                    | :? DateTime as d -> new AttributeValue(S=d.ToString())
                                    | _ -> raise(new Exception("Unsupported data type"))
                                
            let doesHashIndexColumnsExist (index : DynamoDBHashIndex) (columns : ColumnTypeMap) = 
                    match index with
                    | Hash(h) -> columns.ContainsKey(h)
                    | HashAndRange(h,r) -> columns.ContainsKey(h) && 
                                             columns.ContainsKey(r)

            let seqToDic (s:('a * 'b) seq) = new Dictionary<'a,'b>(s |> Map.ofSeq)   

         

            let getFilterExpr (q : QExpr) =                                     
                      let rec evalQExpr i e =              
                                match e with
                                | Between(c, v1, v2) -> (i + 2), sprintf "(%s between :val%d and :val%d)" c (i + 1) (i + 2)
                                | GreaterThan(c, v) -> (i + 1), sprintf "(%s > :val%d)" c (i + 1)                     
                                | LessThan(c, v) -> (i + 1), sprintf "(%s < :val%d)" c (i + 1)                     
                                | And(e1, e2) | Or(e1, e2)  -> let i1, s1 = evalQExpr i e1
                                                               let i2, s2 = evalQExpr i1 e2
                                                               i2, (sprintf "(%s and %s)" s1 s2)                    
                      evalQExpr 0 q |> snd

            let getFilterValues (q : QExpr) =                                              
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
                   let sr = new ScanRequest()                                                                                                        
                   sr.TableName <- q.From 
                   sr.Select <- Select.ALL_ATTRIBUTES
                   sr.ExpressionAttributeValues <- getFilterValues q.Where |> seqToDic
                   sr.FilterExpression <- getFilterExpr q.Where
                   c.Scan(sr).Items                      

            let toDocument (rds : (string * DynamoDBEntry) seq ) =
                    new Document((seqToDic rds))

            let toDynamoDbEntry (v : 'T) = 
                      let success, v = DynamoDBEntryConversion.V2.TryConvertToEntry(v)
                      if success = true then
                        v
                      else
                        raise(new Exception("Invalid value"))  

            let inline (==>) (k : string) (v : 'T) = (k,  toDynamoDbEntry(v)) 

            let uploadToDynamoDB (tableName : string) (c : AmazonDynamoDBClient) (ds : Document array) =        
                    let msftStockPriceTable = Table.LoadTable(c, "MicrosoftStockPrices")                   
                    let batchWrite = msftStockPriceTable.CreateBatchWrite()        
                    ds |> Array.iter(fun d -> batchWrite.AddDocumentToPut(d))
                    batchWrite.Execute()
                                                                                                                                                                                   
            //
            // Validate Columns 
            //
            let hasColumns (s : DynamoDBTableSchema) =
                    ((s.Columns |> Seq.length) >= 1) 
                    |> returnValidationResult "Schema must have a least one column defined"
            
            let doesColumnNamesContainsValidCharacters (s : DynamoDBTableSchema) =
                  s.Columns |> Seq.map(fun kvp -> kvp.Key) 
                            |> holdsTrueForAllItems doesStringHaveOnlyLettersAndDigits 
                            |> returnValidationResult "Table name contains invalid characters"
            //
            // Validate table name
            //
            let isTableNameLengthValid (s : DynamoDBTableSchema) =
                  (s.TableName.Length > 5) 
                  |> returnValidationResult "Table name length must be 3 to 30 characters"

            let doesTableNameContainValidCharacter (s : DynamoDBTableSchema) =
                  doesStringHaveOnlyLettersAndDigits(s.TableName)
                  |> returnValidationResult "Table names can only contain letter and digits"  

            //
            // Validate Primary Key
            //
            let doesPrimaryKeyColumnNameExist (s : DynamoDBTableSchema) =       
                    doesHashIndexColumnsExist s.PrimaryKey s.Columns
                    |> returnValidationResult "Primary key doesn't exist in columns"
          
            //
            // Validate global indexes         
            //
            let isThereZeroToFiveGlobalIndexes (s : DynamoDBTableSchema) =
                    ((s.GlobalSecondaryIndexes |> Seq.length) <= 5) 
                    |> returnValidationResult "Table must have 0 to 5 global indexes"

            let doGlobalIndexColumnNamesExist (s : DynamoDBTableSchema) =       
                    s.GlobalSecondaryIndexes 
                    |> Seq.map (fun index -> index.Index)        
                    |> holdsTrueForAllItems(fun index -> doesHashIndexColumnsExist index s.Columns )                  
                    |> returnValidationResult "Global index column doesn't exist" 

            //
            // Validate local indexes
            //
            let isThereZeroToFiveLocalIndexes (s : DynamoDBTableSchema) =
                    ((s.LocalSecondaryIndexes |> Seq.length) <= 5)
                    |> returnValidationResult "Table must have 0 to 5 local indexes"

            let doLocalIndexColumnNamesExist (s : DynamoDBTableSchema) =       
                    s.LocalSecondaryIndexes 
                    |> Seq.map (fun index -> index.Index)        
                    |> holdsTrueForAllItems(fun index -> doesHashIndexColumnsExist index s.Columns)                  
                    |> returnValidationResult "Local secondary index name does not exist" 
                                                  
            //
            // Build validation groups 
            //
            let checkTableNameReqs (s : DynamoDBTableSchema) =
                 [ isTableNameLengthValid              
                   doesColumnNamesContainsValidCharacters ]     
                 |> doesTableSchemaPassReqs s          

            let checkColumnReqs (s : DynamoDBTableSchema) =
                 [ hasColumns
                   doesColumnNamesContainsValidCharacters ]     
                 |> doesTableSchemaPassReqs s          

            let checkIndexReq(s : DynamoDBTableSchema) = 
                    [ doesPrimaryKeyColumnNameExist          
                      isThereZeroToFiveGlobalIndexes
                      doGlobalIndexColumnNamesExist          
                      isThereZeroToFiveLocalIndexes
                      doLocalIndexColumnNamesExist ]
                    |> doesTableSchemaPassReqs s     

            let createTable (c : AmazonDynamoDBClient) (s : DynamoDBTableSchema) =                               
                    let STANDARD_READ_CAPACITY_UNITS = (int64 5)
                    let STANDARD_WRITE_CAPACITY_UNITS = (int64 6)

                    let createProvisionThroughPut (c: DynamoDBProvisionedCapacity) =
                           match c with
                           | Standard -> new ProvisionedThroughput(STANDARD_READ_CAPACITY_UNITS,
                                                                   STANDARD_WRITE_CAPACITY_UNITS)                               
                           | Custom(r, w) -> new ProvisionedThroughput(r, w)                               
               
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
                               |> Seq.map(fun kvps -> new AttributeDefinition(kvps.Key, createScalarAttributeType kvps.Value))
                               |> (fun items -> new List<_>(items))
                
                    let createKeySchema (index : DynamoDBHashIndex ) =                
                            (match index with
                             | Hash h -> [ new KeySchemaElement(AttributeName=h, KeyType=KeyType.HASH) ]
                             | HashAndRange(h, r) -> [new KeySchemaElement(AttributeName=h,KeyType=KeyType.HASH);
                                                        new KeySchemaElement(AttributeName=r,KeyType=KeyType.RANGE) ])                                                                        
                            |> (fun items -> new List<_>(items))

                    let createLocalSecondaryIndexes() =
                           s.LocalSecondaryIndexes 
                           |> Seq.map(fun index -> new LocalSecondaryIndex(IndexName=index.Name,
                                                                         KeySchema=createKeySchema(index.Index),
                                                                         Projection=new Projection(ProjectionType=createProjectectionType(index.ProjectionType), 
                                                                                                   NonKeyAttributes=new List<_>(index.NonKeyAttributes))))                                     
                           |> (fun items -> new List<_>(items))                       

                    let createGlobalSecondaryIndexes() =
                            s.GlobalSecondaryIndexes 
                            |> Seq.map(fun index -> let g=new GlobalSecondaryIndex()
                                                    g.IndexName <- index.Name
                                                    g.KeySchema <- createKeySchema(index.Index)
                                                    g.Projection <- new Projection(ProjectionType=createProjectectionType(index.ProjectionType), 
                                                                                   NonKeyAttributes=new List<_>(index.NonKeyAttributes))
                                                    g.ProvisionedThroughput <- createProvisionThroughPut(Standard)
                                                    g)
                            |> (fun items -> new List<_>(items))       
                                        
                    new CreateTableRequest(TableName=s.TableName, KeySchema=createKeySchema(s.PrimaryKey),
                                           AttributeDefinitions=createAttributeDefinitions(),  
                                           LocalSecondaryIndexes=createLocalSecondaryIndexes(),
                                           GlobalSecondaryIndexes=createGlobalSecondaryIndexes(),                                  
                                           ProvisionedThroughput=createProvisionThroughPut(s.ProvisionedCapacity))
                    |> c.CreateTable
                
        

            let waitUntilTableIsCreated tableName intervalMilliseconts (c : AmazonDynamoDBClient) =        
                    let rec aux(tblName) =                                  
                                Thread.Sleep(millisecondsTimeout=intervalMilliseconts) // Wait 5 seconds.
                                try                
                                    let res = c.DescribeTable(request=new DescribeTableRequest(TableName=tableName))
                    
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

            let getTableInfo tableName (c : AmazonDynamoDBClient) =  
                    c.DescribeTable(tableName=tableName).Table

            let getListOfTableNames (c : AmazonDynamoDBClient) =
                    c.ListTables().TableNames 

            let getAllTableInfos (c : AmazonDynamoDBClient) =
                    c |> getListOfTableNames |> Seq.map (fun tn -> c.DescribeTable(tableName=tn))


            let printTableSummary (c: AmazonDynamoDBClient) =
                    let tableNames = c |> getListOfTableNames 
                    printfn "Table names"
                    printfn "-----------"     
                    tableNames |> Seq.iteri(fun i t -> printfn "%d. %s" i t)

            let printTableInfo tableName (c : AmazonDynamoDBClient) =        
                    printfn "Table Summary"
                    printfn "-------------"
                    getTableInfo tableName c 
                    |> (fun info -> 
                                printfn "Name: %s" info.TableName
                                printfn "# of items: %d" info.ItemCount
                                printfn "Provision Throughput (reads/sec): %d" info.ProvisionedThroughput.ReadCapacityUnits
                                printfn "Provision Throughput (writes/sec): %d" info.ProvisionedThroughput.WriteCapacityUnits
                                ())