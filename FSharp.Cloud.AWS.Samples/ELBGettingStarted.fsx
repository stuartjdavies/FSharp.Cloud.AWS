(*** hide ***)
#load "packages/FsLab/FsLab.fsx"

#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\AWSSDK.dll"
#r @"..\FSharp.Cloud.AWS\bin\Debug\FSharp.Cloud.AWS.dll"
#load "packages/FsLab/FsLab.fsx"

open FSharp.Cloud.AWS
open Amazon.EC2
open Amazon.EC2.Model
open FSharp.Cloud.AWS.AwsUtils
open System.Collections.Generic
open Amazon
open Amazon.AutoScaling
open Amazon.AutoScaling.Model
open Amazon.ElasticLoadBalancing
open Amazon.ElasticLoadBalancing.Model
open FSharp.Cloud.AWS.DSL
open System

let ec2 = FEc2.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2
let elb = FElb.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2   
                                         
(** Create a new VPC **)
let vpcId = CreateVpcRequest(CidrBlock="10.0.0.0/16", InstanceTenancy=Tenancy.Default) 
            |> SendTo ec2 :?> CreateVpcResponse |> (fun r -> r.Vpc.VpcId)   
CreateTagsRequest(Resources= !~ vpcId, Tags= !~ Amazon.EC2.Model.Tag("Name", "Gettings Started ELB VPC")) |> SendTo ec2

(** Create Subnets & associate to route table **)
let subnetId1  = CreateSubnetRequest(VpcId=vpcId, CidrBlock="10.0.0.0/24", AvailabilityZone="ap-southeast-2a") 
                 |> SendTo ec2 :?> CreateSubnetResponse |> (fun r -> r.Subnet.SubnetId)                  
let subnetId2  = CreateSubnetRequest(VpcId=vpcId, CidrBlock="10.0.2.0/27", AvailabilityZone="ap-southeast-2b") 
                 |> SendTo ec2 :?> CreateSubnetResponse |> (fun r -> r.Subnet.SubnetId) 
                
CreateTagsRequest(Resources= !~ subnetId2, Tags= !~ Amazon.EC2.Model.Tag("Name", "Public subnet 2")) |> SendTo ec2

(** Create Security group **)
let securityGroupId = CreateSecurityGroupRequest(VpcId=vpcId,GroupName="GettingStarted_ELB_SG",Description="ELB Getting started Security Group") 
                      |> SendTo ec2 :?> CreateSecurityGroupResponse |> (fun r -> r.GroupId)
         
(** Create Load Balancer **)
CreateLoadBalancerRequest(LoadBalancerName="GettingStartedELB", Listeners= !~ Listener("HTTP", 80, 80 ), 
                          Subnets= !! [subnetId1; subnetId2], SecurityGroups= !~ securityGroupId, Scheme="internal") |> SendTo elb
ConfigureHealthCheckRequest("GettingStartedELB", HealthCheck("HTTP:80/index.html", 30, 5, 2, 10)) |> SendTo elb


