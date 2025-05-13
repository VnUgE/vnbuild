FROM ubuntu:25.04

RUN apt update &&   \
    apt install -y  \
    cmake           \
    curl            \
    git             \
    build-essential \
    dotnet-sdk-8.0

# Ensure 'go-task' is globally accessible as 'task'
ADD --checksum=sha256:717cc03e60bf92fa53015a15b263c750f2452ba17f8f7b7648b2afc19ac4e969 \
    https://github.com/go-task/task/releases/download/v3.43.3/task_linux_amd64.deb \
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