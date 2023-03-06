#!/bin/bash

function help() {
    echo "$0 [start|stop]"
    echo "starts one server and 2 clients"
    exit 1
}
function run() {
    echo "starting everything..."
    ./Server/bin/Debug/netcoreapp3.1/Server -p 8080 -n server-us-central-1 -v &
    sleep 3
    ./Client/bin/Debug/netcoreapp3.1/Client -s localhost -p 8080 -n client-eu-west-1 -f performance-test -v &
    echo "started everything"
    echo "to connect manually, run: netcat localhost 8080"
}
function kill() {
    echo "killing everything..."
    killall Server
    killall Client
    killall dotnet
}

if [[ $# -ne 1 ]]; then
    help
fi

echo "mode: ${1}"
if [ ${1} == "start" ]; then
    run
elif [ ${1} == "stop" ]; then
    kill
else
    help
fi

