namespace FSharp.Cloud.AWS

module RequirementsChecker = 
           type RequirementResult = | ReqPass | ReqFail of reason : string
           type ReqValidationMessage = string
           type ReqCondition<'a> = ('a -> bool)
           type Requirement<'a> = ReqCondition<'a> * ReqValidationMessage -> RequirementResult
           let (=>) (s : ReqValidationMessage) (f : ReqCondition<'a>) = f, s

           let check (x : 'a) (rs : (ReqCondition<'a> * ReqValidationMessage) list) =
                   let rec aux lst =
                        match rs with
                        | (c,m)::t -> if c(x) = true then
                                        aux(t)
                                      else
                                        ReqFail(m.ToString())  
                        | [] -> ReqPass
                   aux(rs)     

                   


           

          