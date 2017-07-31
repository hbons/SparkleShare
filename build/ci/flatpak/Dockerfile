FROM fedora:latest

RUN dnf install -y flatpak-builder

RUN git clone https://github.com/hbons/org.sparkleshare.SparkleShare 
WORKDIR /org.sparkleshare.SparkleShare

RUN flatpak remote-add --from gnome https://sdk.gnome.org/gnome.flatpakrepo
RUN flatpak install gnome org.gnome.Platform 3.24
RUN flatpak install gnome org.gnome.Sdk 3.24

RUN flatpak-builder --repo=repo app org.sparkleshare.SparkleShare.json

