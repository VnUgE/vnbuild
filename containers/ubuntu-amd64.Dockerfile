FROM ubuntu:24.04

RUN apt update && apt install -y \
    dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/* \
    && apt autoremove -y \
    && apt autoclean

# Install  Task'
ADD --checksum=sha256:b6725faf62ae793d147b70e203fca047c01920807376f9c54d2e9d4211314b81 \
    https://github.com/go-task/task/releases/download/v3.45.4/task_linux_amd64.deb \
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