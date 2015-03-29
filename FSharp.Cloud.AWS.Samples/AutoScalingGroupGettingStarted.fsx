(*** hide ***)
(*** hide ***)
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
open FSharp.Cloud.AWS.DSL

(** Create ec2 client **)
let ec2Client = FEc2.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2
let asClient = FAutoScaling.createClientFromCsvFile """c:\AWS\Stuart.Credentials.csv""" RegionEndpoint.APSoutheast2

(** Step 1: Create a Launch Configuration **)
asClient.CreateLaunchConfiguration(CreateLaunchConfigurationRequest(LaunchConfigurationName="Getting Started Auto Scaling config", 
                                                                    ImageId="ami-89a2d5b3",
                                                                    InstanceType=InstanceType.T1Micro.Value))
                                                                                                              
(** Step 2: Create an Auto Scaling Group **)  
asClient.CreateAutoScalingGroup(CreateAutoScalingGroupRequest(AutoScalingGroupName="Getting Started Auto Scaling group Name",
                                                              LaunchConfigurationName="Getting Started Auto Scaling config",
                                                              AvailabilityZones= !! ["ap-southeast-2a"; "ap-southeast-2b"],
                                                              // LoadBalancerNames=List<string>([""]),
                                                              MaxSize=1,
                                                              MinSize=1,
                                                              DesiredCapacity=1))

(** Step 3: Verify Your Auto Scaling Group **)         
let r = asClient.DescribeAutoScalingGroups(DescribeAutoScalingGroupsRequest(AutoScalingGroupNames= !! ["Getting Started Auto Scaling group Name"]))
r.AutoScalingGroups |> Seq.iter(fun g -> printfn "Group - %s" g.AutoScalingGroupName)
printfn "%d" r.AutoScalingGroups.Count

(** Step 4: (Optional) Delete Your Auto Scaling Infrastructure **) 
asClient.DeleteAutoScalingGroup(DeleteAutoScalingGroupRequest(AutoScalingGroupName="Getting Started Auto Scaling group Name",ForceDelete=true))
asClient.DeleteLaunchConfiguration(DeleteLaunchConfigurationRequest(LaunchConfigurationName="Getting Started Auto Scaling config"))  