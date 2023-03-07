#!/bin/bash

server="127.0.0.1"
region=$(cat ~/.aws/config | grep region | cut -f2 -d'=' | xargs)
pid=$$

docker run \
    --name client-${pid} \
    --rm \
    --network="host"  \
    -it podulator/performance-test-client:0.1 \
    -s ${server} \
    -p 8080 \
    -n ga-client-${region} \
    -f demo- \
    -k ${region}:ga-demo-matrow-${region}
