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
         
(** Create ec2 client **)
let ec2Client = FEc2.createEC2ClientFromCsvFile("""c:\AWS\Stuart.Credentials.csv""")

(** Create a new VPC **)
let vpcId, _ = FEc2.createVpc ec2Client "10.20.0.0/24" Tenancy.Default "My Test VPC"

(** Enable DNS Support & Hostnames in VPC **)
FEc2.enableVpcDnsSupport ec2Client vpcId true
FEc2.enableVpcDnsHostnames ec2Client vpcId true
           
(** Create new Internet Gateway **)
let internetGatewayId, _ = FEc2.createInternetGatway ec2Client
printfn "Internet Gateway ID : %s" internetGatewayId

(** Attach Internet Gateway to VPC **)
FEc2.attachInternetGateway ec2Client vpcId internetGatewayId

(** Create new Route Table **)
let routeTableId, _ = FEc2.createRouteTable ec2Client vpcId
printfn "Route Table ID %s" routeTableId        
        
(** Create new Route **)
let createRouteResponse = FEc2.createRoute ec2Client routeTableId internetGatewayId "0.0.0.0/0"

(** Create Subnet1 & associate route table **)
let sn1Id, _ = FEc2.createSubnet ec2Client vpcId "10.20.1.0/24" (Some("ap-southeast-2b"))
printfn "Subnet1 ID : %s" sn1Id
FEc2.associateSubnetToRouteTable ec2Client routeTableId sn1Id

(** Create Subnet2 & associate route table **) 
let sn2Id, _ = FEc2.createSubnet ec2Client vpcId "10.20.1.0/24" (Some("ap-southeast-2b"))
printfn "Subnet2 ID : %s" sn1Id
FEc2.associateSubnetToRouteTable ec2Client routeTableId sn1Id

(** Delete Subnet **)
FEc2.deleteSubnet ec2Client sn1Id
FEc2.deleteSubnet ec2Client sn2Id

printfn "VPC Setup Finished"

(** Delete the VPC instance **) 
printfn "Delete VPC"
FEc2.deleteVpc ec2Client vpcId          



