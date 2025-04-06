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
ADD --checksum=sha256:d02d4657de0d5454a61bd433c52a9b23b42419bc5dd1da4aaadd0951b57b2cd2 \
    https://github.com/go-task/task/releases/download/v3.42.1/task_linux_amd64.rpm \
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