FROM ubuntu:24.04

RUN apt update &&   \
    apt install -y  \
    cmake           \
    curl            \
    git             \
    build-essential \
    dotnet-sdk-8.0

# Ensure 'go-task' is globally accessible as 'task'
ADD --checksum=sha256:bb660b4197bc6e5728e32d50a6eeea0c1fb095e5574dd4e7212c0ff0503ff81c \
    https://github.com/go-task/task/releases/download/v3.42.1/task_linux_amd64.deb \
    task_linux.deb

#install the task package
RUN dpkg --install task_linux.deb

COPY app/ /usr/local/lib/vnbuild

RUN ln -s /usr/local/lib/vnbuild/vnbuild /usr/local/bin/vnbuild   && \
    chmod +x /usr/local/bin/vnbuild

#dotnet gitversion is required as a global tool
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install -g gitversion.tool --version 5.12.0

RUN vnbuild --version && task --version && dotnet-gitversion /?