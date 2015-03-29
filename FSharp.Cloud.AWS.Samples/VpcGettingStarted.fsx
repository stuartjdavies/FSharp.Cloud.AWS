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
let ec2Client = FEc2.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2
let mutable vpcId = ""
let mutable internetGatewayId = ""
let mutable routeTableId=""
let mutable securityGroupId=""
let mutable subnetId=""
let mutable ec2ReservationId = ""
      
(** Step 1: Set up the VPC and internet Gateway **)
let ``Step 1: Setup the VPC and Internet Gateway``() =          
        (** Create a new VPC **)
        vpcId <- ec2Client.CreateVpc(CreateVpcRequest(CidrBlock="10.0.0.0/16", InstanceTenancy=Tenancy.Default))
                          .Vpc.VpcId                     
        ec2Client.CreateTags(CreateTagsRequest(Resources=List<string>([vpcId]), Tags= !! [ Tag("Name", "Getting Started VPC") ])) |> ignore

        (** Update vpc attributes **)                             
        ec2Client.ModifyVpcAttribute(ModifyVpcAttributeRequest(EnableDnsSupport=true,VpcId=vpcId)) |> ignore
        ec2Client.ModifyVpcAttribute(ModifyVpcAttributeRequest(EnableDnsHostnames=true,VpcId=vpcId)) |> ignore          
     
        (** Get the created route table id **) 
        routeTableId <- ec2Client.DescribeRouteTables().RouteTables.Find(fun rt -> rt.VpcId = vpcId).RouteTableId
    
        (** Add Tags to route table **)
        ec2Client.CreateTags(CreateTagsRequest(Resources= !! [routeTableId], Tags= !! [ Tag("Name", "Getting Started VPC Routing Table") ] )) |> ignore
           
        (** Create new Internet Gateway **)
        internetGatewayId <- ec2Client.CreateInternetGateway(CreateInternetGatewayRequest())
                                      .InternetGateway.InternetGatewayId                         
    
        (** Attach Internet Gateway to VPC **)
        ec2Client.AttachInternetGateway(AttachInternetGatewayRequest(VpcId=vpcId, InternetGatewayId=internetGatewayId)) |> ignore
                                 
        (** Create new Route **)
        ec2Client.CreateRoute(CreateRouteRequest(RouteTableId=routeTableId, GatewayId=internetGatewayId, 
                                                 DestinationCidrBlock="0.0.0.0/0")) |> ignore 

        (** Create Subnet1 & associate to route table **)
        subnetId <- ec2Client.CreateSubnet(CreateSubnetRequest(VpcId=vpcId, CidrBlock="10.0.0.0/24",
                                                               AvailabilityZone="ap-southeast-2b")).Subnet.SubnetId            
        ec2Client.CreateTags(CreateTagsRequest(Resources= !! [subnetId], Tags= !! [ Tag("Name", "Public subnet") ])) |> ignore

        ec2Client.AssociateRouteTable(AssociateRouteTableRequest(RouteTableId=routeTableId, SubnetId=subnetId)) |> ignore
     
        printfn "VPC Setup Finished"
        printfn "Created Vpc with Id : %s" vpcId
        printfn "Created Internet Gateway with Id : %s" internetGatewayId
        printfn "Created Subnet with Id : %s" subnetId           

let ``Step 2: Set Up a Security Group for Your VPC``() =     
    securityGroupId <- ec2Client.CreateSecurityGroup(CreateSecurityGroupRequest(VpcId=vpcId,GroupName="WebServerSG",Description="VPC Getting started Security Group")).GroupId    
    let ps = [ IpPermission(FromPort=80, IpProtocol="tcp", IpRanges= !! ["0.0.0.0/0"], ToPort=80)
               IpPermission(FromPort=443, IpProtocol="tcp", IpRanges= !! ["0.0.0.0/0"], ToPort=443)
               IpPermission(FromPort=22, IpProtocol="tcp", IpRanges= !! ["192.0.2.0/24"], ToPort=22)
               IpPermission(FromPort=3389, IpProtocol="tcp", IpRanges= !! ["192.0.2.0/24"], ToPort=3389) ] 
    ec2Client.AuthorizeSecurityGroupIngress(AuthorizeSecurityGroupIngressRequest(GroupId=securityGroupId, IpPermissions= !! ps))
    
let ``Step 3: Launch an Instance into Your VPC``() =                       
        if (ec2Client.DescribeKeyPairs().KeyPairs |> Seq.exists(fun kp -> kp.KeyName = "VpcGettingStarted") = false) then
           ec2Client.CreateKeyPair(CreateKeyPairRequest(KeyName="VpcGettingStarted")) |> ignore
       
        // Microsoft Windows Server 2012 R2 Base - ami-89a2d5b3              
        let r = ec2Client.RunInstances(RunInstancesRequest(ImageId="ami-89a2d5b3", InstanceType=InstanceType.T1Micro,                                                  
                                                           SubnetId=subnetId, MinCount=1, MaxCount=2, KeyName="VpcGettingStarted", 
                                                           SecurityGroupIds = !! [ securityGroupId ]))

        ec2ReservationId <- r.Reservation.ReservationId
       
        for inst in r.Reservation.Instances do
            let ec2InstanceName = sprintf "VpcGS instance %s" inst.InstanceId 
            ec2Client.CreateTags(CreateTagsRequest(Resources= !! [inst.InstanceId], Tags= !! [ Tag("Name", ec2InstanceName) ])) |> ignore
               
        // TODO: Public IP: Select this check box to request that your instance receives a public IP address.                                                                     
                    
let ``Step 4: Assign an Elastic IP Address to Your Instance``() =         
        let reservation = ec2Client.DescribeInstances().Reservations |> Seq.find(fun r -> r.ReservationId = ec2ReservationId)        
        let r = ec2Client.AllocateAddress(AllocateAddressRequest(Domain=DomainType.Vpc))
        ec2Client.AssociateAddress(AssociateAddressRequest(AllocationId=r.AllocationId, InstanceId=reservation.Instances.[0].InstanceId))

let ``Step 5: Cleanup``() =
        let reservation = ec2Client.DescribeInstances().Reservations |> Seq.find(fun r -> r.ReservationId = ec2ReservationId)
        let instanceIds = reservation.Instances |> Seq.map(fun inst -> inst.InstanceId)                
        ec2Client.TerminateInstances(TerminateInstancesRequest(InstanceIds= !! instanceIds)).TerminatingInstances
        |> Seq.iter(fun inst -> printfn "Terminating ec2 instance %s" inst.InstanceId)
        ec2Client.DeleteVpc(DeleteVpcRequest(VpcId=vpcId))
             
``Step 1: Setup the VPC and Internet Gateway``()
``Step 2: Set Up a Security Group for Your VPC``() 
``Step 3: Launch an Instance into Your VPC``() 
``Step 4: Assign an Elastic IP Address to Your Instance``() 


