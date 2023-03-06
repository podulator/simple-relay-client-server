#!/bin/bash

docker run \
	--name server \
    --rm \
    --network="host"  \
    -p 8080:8080 \
	-it podulator/performance-test-server:0.1 \
    -v -p 8080 -n server-%region%
