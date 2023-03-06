#!/bin/bash

pid=$$
docker run \
	--name client-${pid} \
    --rm \
    --network="host"  \
	-it podulator/performance-test-client:0.1 \
    -s 127.0.0.1 -p 8080 -n client-%region%
