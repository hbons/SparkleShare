FROM ubuntu:16.04

RUN apt-get update
RUN apt-get install -y \
  desktop-file-utils \
  git \
  gtk-sharp3-gapi \
  intltool \
  libappindicator3-0.1-cil-dev \
  libdbus-glib2.0-cil-dev \
  libgtk3.0-cil-dev \
  libsoup2.4-dev \
  libtool-bin \
  libwebkit2gtk-4.0 \
  mono-devel \
  mono-mcs \
  nant \
  xsltproc

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
RUN ./autogen.sh && \
  make && make install 

