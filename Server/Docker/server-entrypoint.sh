#!/bin/bash

# entry point boot strapper

argCount=$#
echo "invoked with: '${@}'"

defaultName="server"
defaultPort="8080"
name="-n ${SERVER_NAME:-$defaultName}"
port="-p ${SERVER_PORT:-$defaultPort}"
region="any"
regionToken="%region%"
verbose="${SERVER_VERBOSE}"

function log () {
	if [ "x" != "x${verbose}" ]; then
		echo $1
	fi
}

function showHelp () {
    echo "Usage: ${0} " \
    "    -p x               Server port. Defaults to 8080.\n" \
	"    -n x               Name. Optional. Defaults to 'server'.\n" \
	"                       A %region% token will be substituted for the real AWS region where possible.\n" \
	"                       eg. server-%region%-accelerated could become server-eu-west-1-accelerated.\n" \
	"    -v                 Enable verbose output"
}

function getRegion() {
    log "We have a region to patch"
    if [[ "x" == "x${AWS_REGION}" ]]; then
        echo "no environment var set, trying the metadata endpoint"
        temp=`curl --connect-timeout 5 --silent http://169.254.169.254/latest/dynamic/instance-identity/document | jq -r .region`
        if [[ "x${temp}" == "x" ]]; then
            log "Couldn't determine the region"
        else
            region=${temp}
        fi    
    else
        region=${AWS_REGION}
    fi
    log "Region set to: '${region}'"
}

while getopts ":p:n:v" opt; do

  case ${opt} in
    p )
        port="-p ${OPTARG}"
        ;;
    n )
        name="-n ${OPTARG}"
        ;;
	v )
		verbose="-v"
	  	;;
    \? )
      	showHelp
      	;;
    : )
      	echo "Invalid option: ${OPTARG} requires an argument"
	  	showHelp
      	;;
  esac
done

# check for verbose 
if [[ "x" != "x${verbose}" ]]; then
	log "Verbose mode enabled"
    verbose="-v"
fi

# check for region action
if [[ ${name} =~ ${regionToken} ]]; then
    getRegion 
    name="${name//$regionToken/$region}"
    log "Name resolved to: ${name}"
fi

# let's make the args 
args="${port} ${name} ${verbose}"
echo "Running: '/tmp/performance/Server ${args}'"
/tmp/performance/Server ${args}
