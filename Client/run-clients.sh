#!/bin/bash

server="127.0.0.1"
region=$(cat ~/.aws/config | grep region | cut -f2 -d'=' | xargs)

./bin/Release/netcoreapp7.0/Client -s ${server} -p 8080 -n ga-client-${region} -f demo- -k ${region}:ga-demo-matrow-${region} &
./bin/Release/netcoreapp7.0/Client -s ${server} -p 8080 -n no-ga-client-${region} -f demo- -k ${region}:ga-demo-matrow-${region} &
