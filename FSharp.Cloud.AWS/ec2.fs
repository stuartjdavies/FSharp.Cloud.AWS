namespace FSharp.Cloud.AWS

open System
open Amazon.EC2
open Amazon.EC2.Model
open System.Collections.Generic
open Amazon.Util

type CreateNewVPCRequest = { Name : String;
                             CidrBlock : String;
                             InstanceTenancy : Tenancy; }

module FEc2 =
        let createEC2ClientFromCsvFile fileName =
                let accessKey, secretAccessKey = AWSCredentials.parseCsv fileName
                new AmazonEC2Client(accessKey, secretAccessKey, Amazon.RegionEndpoint.APSoutheast2)
  
        let createNewVPC (c : AmazonEC2Client) (d : CreateNewVPCRequest) =
                let cVpcRequest = new CreateVpcRequest()  
                cVpcRequest.CidrBlock <- d.CidrBlock
                cVpcRequest.InstanceTenancy <- d.InstanceTenancy
                let response = c.CreateVpc cVpcRequest

                let ctReq = new CreateTagsRequest()
                ctReq.Resources <- new List<string>([response.Vpc.VpcId]) 
                ctReq.Tags <- new List<Tag>([ new Tag("Name", d.Name) ])
                c.CreateTags ctReq |> ignore
                response.Vpc.VpcId

        
        
  
        