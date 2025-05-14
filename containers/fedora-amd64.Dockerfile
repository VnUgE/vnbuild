FROM fedora:42

RUN dnf group install -y  \
    c-development         \
    development-tools

RUN dnf install -y  \
    cmake           \
    curl            \
    git             \
    dotnet-sdk-8.0  \
    && dnf clean all

# Ensure 'go-task' is globally accessible as 'task'
ADD --checksum=sha256:6086e8a1feca2c6a1fdd5042f7752cc41ed5d72bf023456d9fc63cb1bff4fefc \
    https://github.com/go-task/task/releases/download/v3.43.3/task_linux_amd64.rpm \
    task_linux.rpm

#install the task package
RUN rpm -Uvh task_linux.rpm

COPY app/ /usr/local/lib/vnbuild

RUN ln -s /usr/local/lib/vnbuild/vnbuild /usr/local/bin/vnbuild  && \
    chmod +x /usr/local/bin/vnbuild

#dotnet gitversion is required as a global tool
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install -g gitversion.tool --version 5.12.0

RUN vnbuild --version && task --version && dotnet-gitversion /?