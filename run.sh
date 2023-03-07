#!/bin/bash

function help() {
    echo "$0 [managed|start|stop]"
    echo "starts one server and 2 clients"
    echo "managed starts everything, waits, and kills everything on enter key"
    exit 1
}
function run() {
    pushd Server
    echo "starting server from `pwd`..."
    ./run-server.sh
    popd
    sleep 3
    pushd Client
    echo "starting clients from `pwd`..."
    ./run-clients.sh
    popd
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
elif [ ${1} == "managed" ]; then
    run
    echo "press enter to stop everything"
    read
    kill
else
    help
fi

