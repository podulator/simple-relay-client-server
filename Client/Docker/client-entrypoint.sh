#!/bin/bash

# entry point boot strapper

argCount=$#
echo "invoked with: '${@}'"

defaultName="client"
defaultServer="localhost"
defaultPort="8080"

name="${CLIENT_NAME:-$defaultName}"
server="${SERVER_NAME:-$defaultServer}"
port="${CLIENT_PORT:-$defaultPort}"
firehose="${CLIENT_FIREHOSE}"

region="any"
regionToken="%region%"
verbose="${CLIENT_VERBOSE}"
args=""

function log () {
	echo $1
}

function showHelp () {
    echo "Usage: ${0}" \
	"    -s x               Server ip or DNS address. Defaults to localhost.\n" \
    "    -p x               Server port. Defaults to 8080.\n" \
	"    -n x               Name. Optional. Defaults to 'client'.\n" \
	"                       Multiple clients with the same name will aggregate their statistics.\n" \
	"                       The %region% token will be substituted for the real AWS region where possible.\n" \
	"                       eg. client-%region%-accelerated could become client-eu-west-1-accelerated.\n" \
	"    -f x               Kinesis Firehose name. Optional. Can be in format region:name\n" \
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

while getopts ":s:p:n:f:v" opt; do

  case ${opt} in
	s )	
        server="${OPTARG}"
        ;;
	p )	
        port="${OPTARG}"
        ;;
	f )	
        firehose="\"${OPTARG}\""
        ;;
    n )
        name="${OPTARG}"
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
    # set it to a nice value 
    verbose="-v"
    # we are logging the everything
    pingStats=`ping -c 4 ${server}`
    echo "ping results: ${pingStats}"
    traceStats=`traceroute -4 ${server}`
    echo "traceroute ${server}: ${traceStats}"
fi

# check for region action
if [[ ${name} =~ ${regionToken} ]]; then
    getRegion 
    name="${name//$regionToken/$region}"
    log "Name resolved to: ${name}"
fi

# handle optional firehose
if [[ "x" != "x${firehose}" ]]; then
    echo "setting firehose to: -f ${firehose}"
    firehose="-f ${firehose}"
fi

# let's make the args
args="-s ${server} -p ${port} -n ${name} ${firehose} ${verbose}"
echo "Running: '/tmp/performance/Client ${args}'"
/tmp/performance/Client ${args}
