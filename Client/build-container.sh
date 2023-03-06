#!/bin/bash

myDir=`pwd`
cd ../ && dotnet build -c release

cd ${myDir}

docker build -f ./Docker/Dockerfile \
	--tag podulator/performance-test-client:0.1 \
	.
