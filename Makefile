SparklePony.exe : src/SparklePony.cs
	gmcs -pkg:gtk-sharp-2.0 -pkg:notify-sharp -pkg:dbus-sharp src/SparklePony.cs

install:
	mkdir -p /usr/local/share/sparklepony
	cp src/SparklePony.exe /usr/local/share/sparklepony/
	chmod 755 /usr/local/share/sparklepony/SparklePony.exe
	cp src/sparklepony /usr/local/bin/
	chmod 755 /usr/local/bin/sparklepony
	cp data/icons /usr/share/ -R
	mkdir -p ~/.config/autostart
	cp sparklepony.desktop.in ~/.config/autostart/sparklepony.desktop

uninstall:
	rm /usr/local/bin/sparklepony
	rm /usr/local/share/sparklepony/SparklePony.exe
	rmdir /usr/local/share/sparklepony
	rm /usr/share/icons/hicolor/*x*/places/folder-publicshare.png
	rm /usr/share/icons/hicolor/*x*/status/document-*ed.png
	rm /usr/share/icons/hicolor/*x*/status/avatar-default.png
	rm ~/.config/autostart/sparklepony.desktop

clean:
	rm src/SparklePony.exe
