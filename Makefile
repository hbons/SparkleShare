SparklePony.exe : src/SparklePony.cs
	gmcs -pkg:gtk-sharp-2.0 -pkg:notify-sharp -pkg:dbus-sharp src/SparklePony.cs

install:
	mkdir /usr/share/sparklepony
	cp src/SparklePony.exe /usr/share/sparklepony

clean:
	rm src/SparklePony.exe
