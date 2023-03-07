FROM ubuntu:22.04

LABEL version="0.1"
LABEL description="Docker file for building the performance test server on an amazon linux base"

RUN apt update -y && apt upgrade -y
RUN apt install -y \
    apt-transport-https \
    curl \
    gzip \
    jq \
    libicu70 \
    tar \
    traceroute \
    tree \
    wget

RUN wget https://dot.net/v1/dotnet-install.sh && chmod +x ./dotnet-install.sh
RUN ./dotnet-install.sh --channel 7.0

RUN echo "export DOTNET_ROOT=$HOME/.dotnet" >> $HOME/.bashrc
RUN echo "export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools" >> ~/.bashrc

WORKDIR /tmp/performance

COPY Docker/server-entrypoint.sh /tmp/performance/server-entrypoint.sh
RUN chmod +x /tmp/performance/server-entrypoint.sh

COPY ./bin/Release/netcoreapp7.0/* /tmp/performance/
RUN tree /tmp/performance

ENTRYPOINT ["/tmp/performance/server-entrypoint.sh"]
CMD [""]
