Properties {
    $solution = "SparkleShare.sln"
	$gitVersion = "2.9.0"
}

if ($env:APPVEYOR) {
    $base_dir = $env:APPVEYOR_BUILD_FOLDER
	$token = $env:GitHubToken
}

Include "psake-common.ps1"

Task Default -Depends Collect

Task Test -Depends Compile -Description "Run unit and integration tests." {
    # Run-XunitTests "SparkleShare.Tests"
	# Run-XunitTests "SparkleShare.Lib.Tests"
}

Task Collect -Depends Test -Description "Copy all artifacts to the build folder." {
	Create-Directory $temp_dir
	Create-Directory "$base_dir\bin\msysgit"

	Exec { cinst git.commandline -y --version $gitVersion }
	Copy-Item -Recurse -Force "$env:ChocolateyInstall\lib\git.commandline\tools\*" "$base_dir\bin\msysgit"
	Copy-Files "$src_dir\Pixmaps\sparkleshare-app.ico" $build_dir
}

Task Pack -Depends Collect -Description "Create NuGet packages." {
    $version = Get-BuildVersion

	# Create-Package "SparkleShare.Lib" $version
	Create-Package "SparkleShare" $version
}

Task Installer -Depends Pack -Description "Create Squirrel release." {
	$version = Get-BuildVersion
	
	if ($token) {
		Exec { .$syncReleases -releaseDir $release_dir -url "https://github.com/BarryThePenguin/SparkleShare" -token $token }
	} else {
		Exec { .$syncReleases -releaseDir $release_dir -url "https://github.com/BarryThePenguin/SparkleShare" }
	}
	Exec { .$squirrel -releasify "$build_dir\SparkleShare.$version.nupkg" -releaseDir $release_dir -setupIcon "$build_dir\sparkleshare-app.ico" -loadingGif "$src_dir\Pixmaps\install-spinner.gif" } #  -baseUrl "https://github.com/BarryThePenguin/SparkleShare/releases/latest" -n /a /f build/windows/app_signing.p12 /p SECRETPASSWORD

	# Remove synced releases for github
	Get-ChildItem "$release_dir.*" -exclude @('*' + $version + '*') | Remove-Item

	# if(Test-Path $env:WIX) {
	# 	if(Test-Path $build_dir\SparkleShare.msi) {
	# 		del $build_dir\SparkleShare.msi
	# 	}
	# 
	# 	Exec { .$Env:WIX\bin\candle "$base_dir\SparkleShare.wxs" -ext WixUtilExtension | Write-Host }
	# 	Exec { .$Env:WIX\bin\light -ext WixUtilExtension "$base_dir\Sparkleshare.wixobj" "-droot=$base_dir" -out "$build_dir\SparkleShare.msi" | Write-Host }
	# 	if (Test-Path $build_dir\SparkleShare.msi) {
	# 		"SparkleShare.msi created."
	# 	}
	# } else {
	# 	"Not building installer (could not find wix, Windows Installer XML toolset)"
	# 	"wix is available at http://wix.sourceforge.net/"
	# }
}