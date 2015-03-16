(*** hide ***)
#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"
(** Creating Amazon Virtual Private Cloud
    =====================================

    Amazon Virtual Private Cloud (Amazon VPC) lets you provision a logically isolated section of the 
    Amazon Web Services (AWS) Cloud where you can launch AWS resources in a virtual network that you define. 
    You have complete control over your virtual networking environment, 
    including selection of your own IP address range, creation of subnets, 
    and configuration of route tables and network gateways. 
**)
open FSharp.Cloud.AWS
open Amazon.EC2
open Amazon.EC2.Model
open FSharp.Cloud.AWS.AwsUtils
open System.Collections.Generic
      
(** Create ec2 client **)
let ec2Client = FEc2.createEC2ClientFromCsvFile("""c:\AWS\Stuart.Credentials.csv""")

(** Create a new VPC **)
let vpcId = ec2Client.CreateVpc(CreateVpcRequest(CidrBlock="10.20.0.0/24", InstanceTenancy=Tenancy.Default))
                     .Vpc.VpcId                     
ec2Client.CreateTags(CreateTagsRequest(Resources=List<string>([vpcId]), Tags=List<Tag>([ Tag("Name", "My Test VPC") ])))

(** Update vpc attributes **)                             
ec2Client.ModifyVpcAttribute(ModifyVpcAttributeRequest(EnableDnsSupport=true,VpcId=vpcId))
ec2Client.ModifyVpcAttribute(ModifyVpcAttributeRequest(EnableDnsHostnames=true,VpcId=vpcId))           
           
(** Create new Internet Gateway **)
let internetGatewayId = ec2Client.CreateInternetGateway(CreateInternetGatewayRequest())
                                 .InternetGateway.InternetGatewayId                         
printfn "Internet Gateway ID : %s" internetGatewayId

(** Attach Internet Gateway to VPC **)
ec2Client.AttachInternetGateway(AttachInternetGatewayRequest(VpcId=vpcId, InternetGatewayId=internetGatewayId))

(** Create new Route Table **)
let routeTableId = ec2Client.CreateRouteTable(CreateRouteTableRequest(VpcId=vpcId)) 
                            .RouteTable.RouteTableId
printfn "Route Table ID %s" routeTableId        
                                   
(** Create new Route **)
ec2Client.CreateRoute(CreateRouteRequest(RouteTableId=routeTableId, GatewayId=internetGatewayId, 
                                         DestinationCidrBlock="0.0.0.0/0")) 

(** Create Subnet1 & associate route table **)
let sn1Id = ec2Client.CreateSubnet(CreateSubnetRequest(VpcId=vpcId, CidrBlock="10.20.1.0/24",
                                                       AvailabilityZone="ap-southeast-2b")).Subnet.SubnetId            
printfn "Subnet1 ID : %s" sn1Id
ec2Client.AssociateRouteTable(AssociateRouteTableRequest(RouteTableId=routeTableId, SubnetId=sn1Id))

(** Create Subnet2 & associate route table **) 
let sn2Id = ec2Client.CreateSubnet(CreateSubnetRequest(VpcId=vpcId, CidrBlock="10.20.1.0/24", 
                                                       AvailabilityZone="ap-southeast-2b"))
                     .Subnet.SubnetId   
printfn "Subnet2 ID : %s" sn1Id
ec2Client.AssociateRouteTable(AssociateRouteTableRequest(RouteTableId=routeTableId, SubnetId=sn2Id))
 
(** Delete Subnet **)
ec2Client.DeleteSubnet(DeleteSubnetRequest(sn1Id)) 
ec2Client.DeleteSubnet(DeleteSubnetRequest(sn2Id)) 

printfn "VPC Setup Finished"

(** Delete the VPC instance **) 
printfn "Delete VPC" 
ec2Client.DeleteVpc(DeleteVpcRequest(VpcId=vpcId))



