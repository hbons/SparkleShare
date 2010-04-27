SparklePony.exe : SparklePony.cs
	gmcs -pkg:gtk-sharp-2.0 -pkg:notify-sharp -pkg:dbus-sharp SparklePony.cs

clean:
	rm SparklePony.exe
