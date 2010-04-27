SparklePony.exe : src/SparklePony.cs
	gmcs -pkg:gtk-sharp-2.0 -pkg:notify-sharp -pkg:dbus-sharp src/SparklePony.cs

install:
	mkdir /usr/share/sparklepony
	cp src/SparklePony.exe /usr/share/sparklepony/
	cp src/sparklepony /usr/bin/
	cp data/icons /usr/share/ -R

uninstall:
	rm /usr/bin/sparklepony
	rm /usr/share/sparklepony/SparklePony.exe
	rmdir /usr/share/sparklepony
	rm /usr/share/icons/hicolor/*x*/places/folder-publicshare.png

clean:
	rm src/SparklePony.exe
