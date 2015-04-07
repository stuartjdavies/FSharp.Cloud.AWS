(*** hide ***)
#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"

open FSharp.Data
open System.Xml
open System
open System.IO

let a = <@ 1 @>

[<NoComparison; NoEquality; Sealed>]
type QuerySource<'T, 'Q> (source: seq<'T>, ?expr : Quotations.Expr<'T -> 'a>) = 
         member __.Expr = expr 
         member __.Source = source

type test = {
        a : int;
}

[<Class>]
type AwsDynamoDbScanBuilder() =
         member __.For(source:QuerySource<'T,'Q>, body: 'T -> QuerySource<'Result,'Q2>) : QuerySource<'Result,'Q> = QuerySource (Seq.collect (fun x -> (body x).Source) source.Source)
         [<CustomOperation("where",MaintainsVariableSpace=true,AllowIntoPattern=true)>]
         member __.Where(source:QuerySource<'T,'Q>,[<ProjectionParameter>] selector) : QuerySource<'T,'Q> = QuerySource ( (System.Linq.Enumerable.Where (source.Source, Func<_,_>(selector))), <@ selector @>)
         [<CustomOperation("all",MaintainsVariableSpace=true,AllowIntoPattern=true)>]
         member __.All(source:QuerySource<'T,'Q>) : QuerySource<'T,'Q> =  QuerySource (source.Source)        
         member __.Zero() = QuerySource Seq.empty
         member __.Select(source:QuerySource<'T,'Q>,selector) : QuerySource<'U,'Q>=  QuerySource (Seq.map selector source.Source)
         member __.Yield x = QuerySource (Seq.singleton x)

let AwsDynamoDbScan = AwsDynamoDbScanBuilder()


let xs = QuerySource<test,string>([ { test.a = 2; } ; { test.a = 4; }; { test.a = 3; } ])

let rows = AwsDynamoDbScan {
              for x in xs do
              where(x.a > 3)              
           }
rows.Expr.Value.Raw

rows.Source |> Seq.iter(fun r -> printfn "%d "r.a)
