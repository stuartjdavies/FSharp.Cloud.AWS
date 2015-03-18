namespace FSharp.Cloud.AWS

open FSharp.Data
open System.Xml
open System.Xml.Linq
open System

type AWSCred = CsvProvider<"""AwsCredentials.csv""">

type AwsWorkflowFailureType =
     | AwsException of e : Exception
     | AwsFailureMessage of code : int * message : string

type AwsWorkflowResult<'T> =
        | AwsWorkflowSuccessResult of 'T
        | AwsWorkflowFailureResult of ft : AwsWorkflowFailureType

type AwsWorkflowBuilder() =
        member this.Bind(x, f) = 
                    match(x) with
                    | AwsWorkflowSuccessResult x -> f(x) 
                    | AwsWorkflowFailureResult x -> AwsWorkflowFailureResult x                     
        //member this.Delay(f) = f()
        member this.Return(x) = x 
        member this.ReturnFrom(x) = AwsWorkflowSuccessResult(x)

type ListFromZeroToFive<'T> =
        val Indexes : 'T list
        new () = { Indexes = [] }
        new (item1 : 'T) = { Indexes = [ item1 ] }
        new (item1, item2) = { Indexes = [item1; item2; ] }        
        new (item1, item2, index3) = { Indexes = [item1; item2; index3] }
        new (item1, item2, item3, item4) = { Indexes = [item1; item2; item3; item4] }
        new (item1, item2, item3, item4, item5) = { Indexes = [item1; item2; item3; item4; item5] }
        static member empty = ListFromZeroToFive<'T>()


module AwsUtils = 
        let AwsWorkflow = new AwsWorkflowBuilder()

        let ReturnAwsWorkflowResult2(r : _ ) =  
                 try    
                    AwsWorkflowSuccessResult(r())  
                 with 
                 | ex -> AwsWorkflowFailureResult(AwsException(ex))

        //let RunAwsWorkflowAndDisplayResult ()

        let getCredFromCsvFile (fileName : string) =                  
            let cred = (AWSCred.Load(fileName).Rows |> Seq.nth 0) 
            cred.``Access Key Id``, cred.``Secret Access Key`` 
       
            
         
            


