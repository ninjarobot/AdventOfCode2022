Advent of Code 2022
========

_Farmer Edition_

I may regret this, but for this year's Advent of Code, I'm attempting to solve the problems using Farmer and deploying various cloud resources to solve the problem each day. This may go downhill really fast.

### Day 1
I'll need to process each row from a file, which sounds like a job for quick little container instances (each is an elf). But I don't want to run a ton of them or I'll hit my quota and cost a lot of money. Since the file is static, I'm going to embed it in the template to be passed to each container so they don't need to deal with any centralized storage.

I'm thinking recursion here because it's Advent of Code and that's what you do. Each container will accept some startup parameters:

* The line of the file they should work with
* The max value so far

The container (representing an elf) will read the line in the file. If it's the end of the file, the container will write to a file in the storage account Day1.solution.txt. If it's any other line in the file, it will try to parse the line into a number and compare it with the max value so far that it was passed and pass the maximum of those values to start another container, then run an ARM deployment to deploy a container replacing itself with new values for the line and max value.

This is going to make a lot of ARM deployments. I'm expecting a phone call.
