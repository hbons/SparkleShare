.\packages\OpenCover.4.5.3723\OpenCover.Console.exe -register:user "-filter:+[SparkleLib]* -[*Test]*" "-target:.\packages\xunit.runner.console.2.0.0\tools\xunit.console.exe" "-targetargs:..\..\src\SparkleLib.Tests\bin\debug\SparkleLib.Tests.dll -noshadow"

.\packages\ReportGenerator.2.1.4.0\ReportGenerator.exe "-reports:results.xml" "-targetdir:.\coverage"

pause