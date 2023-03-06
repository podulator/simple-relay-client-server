#!/bin/bash

myDir=`pwd`
cd ../ && dotnet build -c release

cd ${myDir}

docker build -f ./Docker/Dockerfile \
	--tag podulator/performance-test-server:0.1 \
	.
