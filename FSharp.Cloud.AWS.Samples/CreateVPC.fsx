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

let ec2Client = FEc2.createEC2ClientFromCsvFile("""c:\AWS\Stuart.Credentials.csv""")

(** Create a new VPC **)
let vpcId = { CreateNewVPCRequest.Name = "My Test VPC";
                                  CidrBlock = "10.20.0.0/24"; 
                                  InstanceTenancy = Tenancy.Default } 
            |> FEc2.createNewVPC ec2Client

(** Enable DNS Support & Hostnames in VPC **)
ec2Client.ModifyVpcAttribute(new ModifyVpcAttributeRequest(EnableDnsSupport=true,VpcId=vpcId))
ec2Client.ModifyVpcAttribute(new ModifyVpcAttributeRequest(EnableDnsHostnames=true,VpcId=vpcId))


(** Create new Internet Gateway **)
let internetGatewayId = ec2Client.CreateInternetGateway(new CreateInternetGatewayRequest()).InternetGateway.InternetGatewayId
printfn "Internet Gateway ID : %s" internetGatewayId

(** Attach Internet Gateway to VPC **)
ec2Client.AttachInternetGateway(new AttachInternetGatewayRequest(InternetGatewayId=internetGatewayId,VpcId=vpcId))

(** Create new Route Table **)
let routeTableId = ec2Client.CreateRouteTable(new CreateRouteTableRequest(VpcId=vpcId)).RouteTable.RouteTableId
printfn "Route Table ID %s" routeTableId

(** Create new Route **)
ec2Client.CreateRoute(new CreateRouteRequest(RouteTableId=routeTableId, GatewayId=internetGatewayId, DestinationCidrBlock="0.0.0.0/0" ))

(** Create Subnet1 & associate route table **)
let sn1Id = ec2Client.CreateSubnet(new CreateSubnetRequest(VpcId=vpcId, CidrBlock="10.20.1.0/24", AvailabilityZone="ap-southeast-2b")).Subnet.SubnetId
printfn "Subnet1 ID : %s" sn1Id
ec2Client.AssociateRouteTable(new AssociateRouteTableRequest(RouteTableId=routeTableId, SubnetId=sn1Id))

(** Create Subnet2 & associate route table **) 
let sn2Id = ec2Client.CreateSubnet(new CreateSubnetRequest(VpcId=vpcId, CidrBlock="10.20.2.0/24",AvailabilityZone="ap-southeast-2a")).Subnet.SubnetId
printfn "Subnet2 ID : %s" sn1Id
ec2Client.AssociateRouteTable(new AssociateRouteTableRequest(RouteTableId=routeTableId, SubnetId=sn1Id))

ec2Client.DeleteSubnet(new DeleteSubnetRequest(SubnetId="subnet-ff15cb9a"))
ec2Client.DeleteSubnet(new DeleteSubnetRequest("10.0.1.0/16"))

printfn "VPC Setup Finished"

(** Delete the VPC instance **) 
printfn "Delete VPC"
ec2Client.DeleteVpc(new DeleteVpcRequest(vpcId))