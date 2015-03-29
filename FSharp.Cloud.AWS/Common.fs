namespace FSharp.Cloud.AWS

open FSharp.Data
open System.Xml
open System.Xml.Linq
open System
open System.IO
open ICSharpCode.SharpZipLib.Core;
open ICSharpCode.SharpZipLib.GZip;
open System.Text

type AWSCred = CsvProvider<"""AwsCredentialsSchema.csv""">


module DSL =
    let (!!) (xs : 'a seq) = new System.Collections.Generic.List<'a>(xs)

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

        let rec getFileNamesBy f (path : string) =             
                    Seq.append (Directory.GetFiles path)
                               (Directory.GetDirectories path |> Seq.map (fun d -> getFileNamesBy f d) 
                                                              |> Seq.concat)            
        let getFileNames =
                    getFileNamesBy (fun fn -> true)
                               

module GZip =
           let extract srcFile destFile =
                    // Use a 4K buffer. Any larger is a waste.    
                    let dataBuffer : byte array = Array.zeroCreate 4096
                    use fsIn = new FileStream(srcFile, FileMode.Open, FileAccess.Read)
                    use s = new GZipInputStream(fsIn)                                
                    let fsOut = File.Create(destFile) 
                    StreamUtils.Copy(s, fsOut, dataBuffer);
                    fsIn.Close()
                    fsOut.Close()     
           
           let readFromStream s =
                    // Use a 4K buffer. Any larger is a waste.    
                    let dataBuffer : byte array = Array.zeroCreate 4096                    
                    use s = new GZipInputStream(s)                                
                    use sOut = new MemoryStream()                   
                    StreamUtils.Copy(s, sOut, dataBuffer)
                    Encoding.UTF8.GetString(sOut.ToArray())
                    
           let unzipFilesInDir path = 
                        path |> AwsUtils.getFileNames 
                             |> Seq.filter(fun fn -> fn.Contains(".gz"))
                             |> Seq.iter(fun fn -> extract fn (fn.Replace(".gz", String.Empty)))    
          
            
            


