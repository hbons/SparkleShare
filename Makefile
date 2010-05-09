SparkleShare.exe : SparkleShare.sln
	mdtool build --f --buildfile:SparkleShare.sln

install:
	mkdir -p /usr/local/share/sparkleshare
	cp SparkleShare/bin/Debug/SparkleShare.exe /usr/local/share/sparkleshare/
	cp SparkleShare/bin/Debug/SparkleShare.exe.mdb /usr/local/share/sparkleshare/
	chmod 755 /usr/local/share/sparkleshare/SparkleShare.exe
	cp sparkleshare /usr/local/bin/
	chmod 755 /usr/local/bin/sparkleshare
	cp data/icons /usr/share/ -R
	mkdir -p ~/.config/autostart
	cp data/sparkleshare.desktop.in ~/.config/autostart/sparkleshare.desktop
	chmod 775 ~/.config/autostart/sparkleshare.desktop
	gtk-update-icon-cache /usr/share/icons/hicolor -f

uninstall:
	rm /usr/local/bin/sparkleshare
	rm /usr/local/share/sparkleshare/SparkleShare.exe
	rm /usr/local/share/sparkleshare/SparkleShare.exe.mdb
	rmdir /usr/local/share/sparkleshare
	rm /usr/share/icons/hicolor/*x*/places/folder-sparkleshare.png
	rm /usr/share/icons/hicolor/*x*/places/folder-sync*.png
	rm /usr/share/icons/hicolor/*x*/status/document-*ed.png
	rm /usr/share/icons/hicolor/*x*/status/avatar-default.png
	rm /usr/share/icons/hicolor/*x*/emblems/emblem-sync*.png
	rm /usr/share/icons/hicolor/*x*/animations/process-working.png
	rm ~/.config/autostart/sparkleshare.desktop

clean:
	rm SparkleShare/bin/Debug/SparkleShare.exe
	rm SparkleShare/bin/Debug/SparkleShare.exe.mdb
