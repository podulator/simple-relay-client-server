FROM amazonlinux:latest

LABEL version="0.1"
LABEL description="Docker file for building the performance test server on an amazon linux base"

RUN rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
RUN yum update -y && yum upgrade -y

RUN yum install -y \
    curl \
    dotnet-sdk-3.1 \
    jq \
    tree

WORKDIR /tmp/performance
COPY Docker/server-entrypoint.sh /tmp/performance/server-entrypoint.sh
RUN chmod +x /tmp/performance/server-entrypoint.sh

COPY ./bin/Release/netcoreapp3.1/* /tmp/performance/
RUN tree /tmp/performance

ENTRYPOINT ["/tmp/performance/server-entrypoint.sh"]
CMD [""]
