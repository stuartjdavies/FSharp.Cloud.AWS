namespace FSharp.Cloud.AWS

open System
open Amazon.EC2
open Amazon.EC2.Model
open System.Collections.Generic
open Amazon.Util
open FSharp.Cloud.AWS.AwsUtils
open System.Collections.Generic

type Vpc = Vpc of id : string option * Name : string * instanceTenancy : Tenancy  
type Subnet = Subnet of name : string * CidrBlock : string * AvailabilityZone : string 
type InternetGateway = InternetGateway of id : String option             
type RouteTable = RouteTable of id : string option

//type Vpc = {
//        Id : string; Name : string; InstanceTencacy : Tenancy    
//        Subnets : Subnet list;  
//     } 
//
//type AWSCommand<'a> =
//
//CreateVPCWith [ InternetGateway 


//type VpcOption =
//        | InternetGateway 
//        | RouteTable 
//        | Subnets of subnets list Subnet
//        | DnsSupport of Enable : bool
//        | DnsHostNames of Enabled : bool



type CreateNewVPCRequest = { Name : String;
                             CidrBlock : String;
                             InstanceTenancy : Tenancy; }

type FSecurityGroupFilter = FSecurityGroupFilter of name : string * values : string list

//module FIam =
//         let describeSecruityGroups (c : AmazonEC2Client) =
//                let r = new DescribeSecurityGroupsRequest() |> c.DescribeSecurityGroups 
//                r.SecurityGroups, r
//
//         let describeSecruityGroupsBy (c : AmazonEC2Client) (fs : FSecurityGroupFilter seq) =                               
//                let filters = (List<Filter> (Seq.map(fun (FSecurityGroupFilter(n, vs)) -> new Filter(n, (List vs))) fs))
//                let r = new DescribeSecurityGroupsRequest(Filters=filters) |> c.DescribeSecurityGroups
//                r.SecurityGroups, r
//                c.
//
//         // let createSecurityGroupRequest (c : AmazonEC2Client) groupName description vpcId =                 
//                
//         let terminateEc2Instance (c : AmazonEC2Client) (instanceIds : string list) =
//                let r = new TerminateInstancesRequest(
//                                    InstanceIds= (List<string> instanceIds)) |> c.TerminateInstances
//                r.TerminatingInstances |> Seq.toList, r              
//                
        


//        var newSGRequest = new CreateSecurityGroupRequest()
//    {
//        GroupName = secGroupName,
//        Description = "My sample security group for EC2-VPC",
//        VpcId = vpcID
//    };
//    var csgResponse = ec2Client.CreateSecurityGroup(newSGRequest);
//    Console.WriteLine();
//    Console.WriteLine("New security group: " + csgResponse.GroupId);
//
//    List<string> Groups = new List<string>() { csgResponse.GroupId };
//    var newSgRequest = new DescribeSecurityGroupsRequest() { GroupIds = Groups };
//    var newSgResponse = ec2Client.DescribeSecurityGroups(newSgRequest);
//    mySG = newSgResponse.SecurityGroups[0];
//}

      
module FEc2 =
        let createEC2ClientFromCsvFile fileName =
                let accessKey, secretAccessKey = AwsUtils.getCredFromCsvFile fileName
                new AmazonEC2Client(accessKey, secretAccessKey, Amazon.RegionEndpoint.APSoutheast2)
        
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
        