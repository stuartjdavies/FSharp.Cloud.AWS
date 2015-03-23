namespace FSharp.Cloud.AWS

open System
open Amazon.EC2
open Amazon.EC2.Model
open System.Collections.Generic
open Amazon.Util
open FSharp.Cloud.AWS.AwsUtils
open System.Collections.Generic
      
module FEc2 =
        let createClientFromCsvFile fileName (region : Amazon.RegionEndpoint) =
                let accessKey, secretAccessKey = AwsUtils.getCredFromCsvFile fileName
                new AmazonEC2Client(accessKey, secretAccessKey, region)
        
        let setVpcName (c : AmazonEC2Client) vpcId name =                 
                (new CreateTagsRequest(Resources=new List<string>([vpcId]), Tags=new List<Tag>([ new Tag("Name", name) ])))
                |> c.CreateTags                
                        
        let createVpc (c : AmazonEC2Client) cidrBlock instanceTenancy name =                
                let r = new CreateVpcRequest(CidrBlock=cidrBlock, InstanceTenancy=instanceTenancy) |> c.CreateVpc                
                setVpcName c r.Vpc.VpcId name |> ignore
                r.Vpc.VpcId, r
        
        let createInternetGatway (c : AmazonEC2Client) =
                let r = new CreateInternetGatewayRequest() |> c.CreateInternetGateway
                r.InternetGateway.InternetGatewayId, r        
               
        let createRouteTable (c : AmazonEC2Client) vpcId =
               let r = new CreateRouteTableRequest(VpcId=vpcId) |> c.CreateRouteTable
               r.RouteTable.RouteTableId, r

        let createSubnet (c : AmazonEC2Client) vpcId cidrBlock availablityZone =                               
               let r = (match availablityZone with
                        | Some(aZone) -> new CreateSubnetRequest(VpcId=vpcId, CidrBlock=cidrBlock, AvailabilityZone=aZone)
                        | None -> new CreateSubnetRequest(VpcId=vpcId, CidrBlock=cidrBlock)) |> c.CreateSubnet                
               r.Subnet.SubnetId, r
        
        let associateSubnetToRouteTable (c : AmazonEC2Client) routeTableId subnetId =                
               new AssociateRouteTableRequest(RouteTableId=routeTableId, SubnetId=subnetId) 
               |> c.AssociateRouteTable
        
        let attachInternetGateway (c : AmazonEC2Client) vpcId internetGatewayId =               
               new AttachInternetGatewayRequest(InternetGatewayId=internetGatewayId,VpcId=vpcId)
               |> c.AttachInternetGateway
          
        let enableVpcDnsSupport (c : AmazonEC2Client) vpcId value =                 
               new ModifyVpcAttributeRequest(EnableDnsSupport=value,VpcId=vpcId) 
               |> c.ModifyVpcAttribute

        let enableVpcDnsHostnames (c : AmazonEC2Client) vpcId value =                
               new ModifyVpcAttributeRequest(EnableDnsHostnames=value,VpcId=vpcId) 
               |> c.ModifyVpcAttribute
        
        let deleteSubnet (c : AmazonEC2Client) subnetId =
               new DeleteSubnetRequest(subnetId) 
               |> c.DeleteSubnet
            
        let deleteVpc (c : AmazonEC2Client) vpcId =
               new DeleteVpcRequest(vpcId) 
               |> c.DeleteVpc
        