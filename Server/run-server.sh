#!/bin/bash

region=$(cat ~/.aws/config | grep region | cut -f2 -d'=' | xargs)

./bin/Release/netcoreapp7.0/Server  -p 8080 -n server-${region} -f demo- -k ${region}:ga-demo-${region} -v &
