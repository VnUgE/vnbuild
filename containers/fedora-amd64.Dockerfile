FROM fedora:42

# Combine package installation and cleanup in single layer
RUN dnf install -y \
    dotnet-sdk-8.0 \
    && dnf clean all \
    && dnf autoremove -y

# Install task and clean up in same layer
ADD --checksum=sha256:7981839f1932ab9de743b0cb46513d9127139d65c9f1ed92ffc6f97cc7b1fe07 \
    https://github.com/go-task/task/releases/download/v3.45.4/task_linux_amd64.rpm \
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