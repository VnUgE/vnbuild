FROM ubuntu:24.04

RUN apt update && apt install -y \
    dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/* \
    && apt autoremove -y \
    && apt autoclean

# Install  Task'
ADD --checksum=sha256:9ba2f9f5f11f8429f82cf4ceaa90e6187e02e5a0c161ba0975ba621942ce20bc \
    https://github.com/go-task/task/releases/download/v3.44.1/task_linux_amd64.deb \
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