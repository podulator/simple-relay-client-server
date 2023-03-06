#!/bin/bash

docker rm -vf $(docker ps -a -q)
docker rmi -f $(docker images -a -q)
if [[ -d ./binaries ]]; then
	sudo rm -rf ./binaries
fi
