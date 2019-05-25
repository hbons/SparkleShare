FROM ubuntu:latest

ENV DEBIAN_FRONTEND=noninteractive 

RUN apt-get update
RUN apt-get install -y \
  automake \
  autoconf \
  desktop-file-utils \
  git \
  gtk-sharp3-gapi \
  libappindicator3-0.1-cil-dev \
  libdbus-glib2.0-cil-dev \
  libgtk3.0-cil-dev \
  libmono-system-xml-linq4.0-cil \
  libsoup2.4-dev \
  libtool-bin \
  libwebkit2gtk-4.0 \
  mono-devel \
  mono-mcs \
  ninja-build \
  python3-pip \
  xsltproc

RUN pip3 install meson

RUN git clone https://github.com/hbons/notify-sharp && \
  cd notify-sharp/ && \
  ./autogen.sh --disable-docs && \
  make && make install

RUN cd ../

RUN git clone https://github.com/hbons/soup-sharp && \
  cd soup-sharp/ && \
  ./autogen.sh && \
  make && make install

RUN cd ../

RUN git clone https://github.com/hbons/webkit2gtk-sharp && \
  cd webkit2gtk-sharp/ && \
  ./autogen.sh && \
  make && make install

RUN cd ../

COPY ./ ./
RUN mkdir build/

RUN meson build/ && \
  cd build/ && \
  ninja && ninja install

