(*** hide ***)
#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"
(**
Getting Started with Amazon VPC
===============================

This guide provides a hands-on introduction to using Amazon VPC through the AWS Management Console. 
The exercise in this guide walks you through a simple scenario in which you set up a VPC with a single public subnet containing a
running EC2 instance with an Elastic IP address.

Important
Before you can use Amazon VPC for the first time, you must sign up for Amazon Web Services (AWS). 
When you sign up, your AWS account is automatically signed up for all services in AWS, including Amazon VPC. 
If you haven't created an AWS account already, go to http://aws.amazon.com, and then click Sign Up.

Steps in Excercie:
==================

Step 1: Set Up the VPC and Internet Gateway
Step 2: Set Up a Security Group for Your VPC
Step 3: Launch an Instance into Your VPC
Step 4: Assign an Elastic IP Address to Your Instance
For an overview of the exercise, see Overview of the Exercise. For a basic overview of Amazon VPC, see What is Amazon VPC? in the Amazon VPC User Guide.
*)

open FSharp.Cloud.AWS
open Amazon.EC2
open Amazon.EC2.Model
open FSharp.Cloud.AWS.AwsUtils
open System.Collections.Generic
open Amazon
open FSharp.Cloud.AWS.DSL

(** Create ec2 client **)
let ec2 = FEc2.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2
    
(** Step 1: Set up the VPC, Subnets, Route Table and internet Gateway **) 
let vpcId = ec2.CreateVpc(CreateVpcRequest(CidrBlock="10.0.0.0/16", InstanceTenancy=Tenancy.Default)).Vpc.VpcId                     
CreateTagsRequest(Resources= !~ vpcId, Tags= !~ Amazon.EC2.Model.Tag("Name", "Getting Started VPC")) |> SendTo ec2 |> ignore
ModifyVpcAttributeRequest(EnableDnsSupport=true,VpcId=vpcId) |> SendTo ec2 |> ignore
ModifyVpcAttributeRequest(EnableDnsHostnames=true,VpcId=vpcId) |> SendTo ec2 |> ignore          

let routeTableId = ec2.DescribeRouteTables().RouteTables.Find(fun rt -> rt.VpcId = vpcId).RouteTableId
CreateTagsRequest(Resources= !~ routeTableId, Tags= !~ Tag("Name", "Getting Started VPC Routing Table")) |> SendTo ec2 |> ignore 

let internetGatewayId = ec2.CreateInternetGateway(CreateInternetGatewayRequest()).InternetGateway.InternetGatewayId                         
AttachInternetGatewayRequest(VpcId=vpcId, InternetGatewayId=internetGatewayId) |> SendTo ec2 |> ignore 
CreateRouteRequest(RouteTableId=routeTableId, GatewayId=internetGatewayId, DestinationCidrBlock="0.0.0.0/0") |> SendTo ec2 |> ignore  

let subnetId = ec2.CreateSubnet(CreateSubnetRequest(VpcId=vpcId, CidrBlock="10.0.0.0/24", AvailabilityZone="ap-southeast-2b")).Subnet.SubnetId            
CreateTagsRequest(Resources= !~ subnetId, Tags= !~ Tag("Name", "Public subnet")) |> SendTo ec2 |> ignore  
AssociateRouteTableRequest(RouteTableId=routeTableId, SubnetId=subnetId) |> SendTo ec2 |> ignore
     
printfn "VPC Setup Finished"
printfn "Created Vpc with Id : %s" vpcId
printfn "Created Internet Gateway with Id : %s" internetGatewayId
printfn "Created Subnet with Id : %s" subnetId           

(** Step 2: Set Up a Security Group for Your VPC **)
let securityGroupId = ec2.CreateSecurityGroup(CreateSecurityGroupRequest(VpcId=vpcId, GroupName="WebServerSG",
                                                                         Description="VPC Getting started Security Group")).GroupId    
let ps = [ IpPermission(FromPort=80, IpProtocol="tcp", IpRanges= !~ "0.0.0.0/0", ToPort=80)
           IpPermission(FromPort=443, IpProtocol="tcp", IpRanges= !~ "0.0.0.0/0", ToPort=443)
           IpPermission(FromPort=22, IpProtocol="tcp", IpRanges= !~ "192.0.2.0/24", ToPort=22)
           IpPermission(FromPort=3389, IpProtocol="tcp", IpRanges= !~ "192.0.2.0/24", ToPort=3389) ] 
AuthorizeSecurityGroupIngressRequest(GroupId=securityGroupId, IpPermissions= !! ps) |> SendTo ec2 |> ignore
    
(** Step 3: Launch an Instance into Your VPC **)
if (ec2.DescribeKeyPairs().KeyPairs |> Seq.exists(fun kp -> kp.KeyName = "VpcGettingStarted") = false) then
    CreateKeyPairRequest(KeyName="VpcGettingStarted") |> SendTo ec2 |> ignore
       
// Microsoft Windows Server 2012 R2 Base - ami-89a2d5b3              
let r = RunInstancesRequest(ImageId="ami-89a2d5b3", InstanceType=InstanceType.T1Micro,                                                  
                            SubnetId=subnetId, MinCount=1, MaxCount=2, KeyName="VpcGettingStarted", 
                            SecurityGroupIds = !~ securityGroupId) |> SendTo ec2 :?> RunInstancesResponse
let ec2ReservationId = r.Reservation.ReservationId
       
for inst in r.Reservation.Instances do
   CreateTagsRequest(Resources= !~ inst.InstanceId, 
                     Tags= !~ Tag("Name", sprintf "VpcGS instance %s" inst.InstanceId)) |> SendTo ec2 |> ignore
                                        
// TODO: Public IP: Select this check box to request that your instance receives a public IP address.                                                                     
                    
(** Step 4: Assign an Elastic IP Address to Your Instance **)
let instanceId = ec2.DescribeInstances().Reservations |> Seq.find(fun r -> r.ReservationId = ec2ReservationId) |> (fun r -> r.Instances.[0].InstanceId)       
let allocationId = ec2.AllocateAddress(AllocateAddressRequest(Domain=DomainType.Vpc)).AllocationId
AssociateAddressRequest(AllocationId=allocationId, InstanceId=instanceId) |> SendTo ec2 

(** Step 5: Cleanup **) 
let reservation = ec2.DescribeInstances().Reservations |> Seq.find(fun r -> r.ReservationId = ec2ReservationId)
let instanceIds = reservation.Instances |> Seq.map(fun inst -> inst.InstanceId)                
ec2.TerminateInstances(TerminateInstancesRequest(InstanceIds= !! instanceIds)).TerminatingInstances
|> Seq.iter(fun inst -> printfn "Terminating ec2 instance %s" inst.InstanceId)

            

