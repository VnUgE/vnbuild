FROM fedora:42

# Combine package installation and cleanup in single layer
RUN dnf group install -y \
    c-development \
    development-tools \
    && dnf install -y \
    cmake \
    curl \
    git \
    dotnet-sdk-8.0 \
    && dnf clean all \
    && dnf autoremove -y

# Install task and clean up in same layer
ADD --checksum=sha256:9c4b02c1923d9392250547cb1e9ed3a5ff862381083612196985192e7b34ad84 \
    https://github.com/go-task/task/releases/download/v3.44.0/task_linux_amd64.rpm \
    task_linux_amd64.rpm

RUN rpm -Uvh task_linux_amd64.rpm && \
    rm task_linux_amd64.rpm

COPY app/ /usr/local/lib/vnbuild

RUN ln -s /usr/local/lib/vnbuild/vnbuild /usr/local/bin/vnbuild && \
    chmod +x /usr/local/bin/vnbuild

# Install dotnet tool and verify in single layer
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install -g gitversion.tool --version 6.3.0 && \
    vnbuild --version && task --version && dotnet-gitversion /?