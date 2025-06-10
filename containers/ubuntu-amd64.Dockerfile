FROM ubuntu:24.04

RUN apt update && \
    apt install -y \
    cmake \
    curl \
    git \
    build-essential \
    dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/* \
    && apt autoremove -y \
    && apt autoclean

# Install  Task'
ADD --checksum=sha256:cdd55b9908d3ef0889bb2270132f7bdb90e50d85b645c57434385cb8ea80cc42 \
    https://github.com/go-task/task/releases/download/v3.44.0/task_linux_amd64.deb \
    task_linux_amd64.deb

RUN dpkg --install task_linux_amd64.deb && \
    rm task_linux_amd64.deb && \
    apt autoremove -y

COPY app/ /usr/local/lib/vnbuild

RUN ln -s /usr/local/lib/vnbuild/vnbuild /usr/local/bin/vnbuild && \
    chmod +x /usr/local/bin/vnbuild

#dotnet gitversion is required as a global tool
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install -g gitversion.tool --version 6.3.0

RUN vnbuild --version && task --version && dotnet-gitversion /?