
REM ssh-keyscan comes with OpenSSH.Client
powershell -command "Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0"
REM ssh-keygen comes with OpenSSH.Server
powershell -command "Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0"
