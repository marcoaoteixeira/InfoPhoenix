#Requires -Version 2.0
<#
    .SYNOPSIS
    Build solution

    .DESCRIPTION
    Build solution

    .LINK
	Project home: https://github.com/marcoaoteixeira/infophoenix

	.NOTES
	Author:
	Version: 1.0.0
	
	This script is designed to be called from PowerShell.
#>
[CmdletBinding()]
Param (
	[Parameter(Mandatory = $true, HelpMessage = "The executable project")]
	[Alias("e")]
    [String]$ExeProject = $null,
	
	[Parameter(Mandatory = $true, HelpMessage = "The configuration")]
	[Alias("c")]
    [String]$Configuration = $null,
	
	[Parameter(Mandatory = $false, HelpMessage = "The target platform")]
	[Alias("p")]
    [String]$TargetPlatform = "x64",
	
	[Parameter(Mandatory = $false, HelpMessage = "The target framework")]
	[Alias("f")]
    [String]$TargetFramework = "net8.0-windows",
	
	[Parameter(Mandatory = $false, HelpMessage = "The runtime")]
	[Alias("r")]
    [String]$Runtime = "win-x64",
	
	[Parameter(Mandatory = $false, HelpMessage = "The output path")]
	[Alias("o")]
    [String]$OutputPath = ".\output",
	
	[Parameter(Mandatory = $false, HelpMessage = "Increase version major number")]
    [Switch]$IncreaseMajor,
	
	[Parameter(Mandatory = $false, HelpMessage = "Increase version minor number")]
    [Switch]$IncreaseMinor,
	
	[Parameter(Mandatory = $false, HelpMessage = "Run publish")]
    [Switch]$Publish,
	
    [Parameter(Mandatory = $false, HelpMessage = "Whether should show prompt for errors")]
    [Switch]$PromptOnError = $false
)

# Turn on Strict Mode to help catch syntax-related errors.
#   This must come after a script's/function's param section.
#   Forces a Function to be the first non-comment code to appear in a PowerShell Module.
Set-StrictMode -Version Latest

#==========================================================
# Define any necessary global variables, such as file paths.
#==========================================================

# Gets the script file name, without extension.
$SCRIPT_NAME = [System.IO.Path]::GetFileNameWithoutExtension($MyInvocation.MyCommand.Definition)

# Get the directory that this script is in.
$SCRIPT_DIRECTORY_PATH = Split-Path $script:MyInvocation.MyCommand.Path

#==========================================================
# Define functions used by the script.
#==========================================================

# Catch any exceptions Thrown, display the error message, wait for input if appropriate, and then stop the script.
Trap [Exception] {
    $message = $_
    Write-Host "An error occurred while executing the script:`n$message`n" -Foreground Red
    
    If ($PromptOnError) {
        Write-Host "Press any key to continue ..."
        $userInput = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyUp")
    }

    Break;
}

# PowerShell v2.0 compatible version of [String]::IsNullOrWhitespace.
Function Test-StringIsNullOrWhitespace([String]$value) {
    Return [String]::IsNullOrWhiteSpace($value)
}

#==========================================================
# Perform the script tasks.
#==========================================================

# Display the time that this script started running.
$ScriptStartTime = Get-Date
Write-Verbose "[$($SCRIPT_NAME)] Starting at $($ScriptStartTime.TimeOfDay.ToString())."

# Display the version of PowerShell being used to run the script, as this can help solve some problems that are hard to reproduce on other machines.
Write-Verbose "Using PowerShell Version: $($PSVersionTable.PSVersion.ToString())."

Class Version {
	[int]$Major
	[int]$Minor
	[int]$Build
	
	Static [Version]Fetch([String]$VersionFilePath) {
		return [Version](Get-Content $VersionFilePath | Out-String | ConvertFrom-Json)
	}
	
	Static [void]Save([Version]$version, [String]$VersionFilePath) {
		$version | ConvertTo-Json | Out-File $VersionFilePath
	}
}

Try {
	# Restore solution
	dotnet restore $ExeProject
	
	If ($Publish) {
		$VersionFilePath = "$($SCRIPT_DIRECTORY_PATH)\version.json"
	
		# Get the version from json file.
		$CurrentVersion = [Version]::Fetch($VersionFilePath)
		
		If ($IncreaseMajor) { $CurrentVersion.Major += 1 }
		If ($IncreaseMinor) { $CurrentVersion.Minor += 1 }
		$CurrentVersion.Build += 1
		
		$SemVer = "$($CurrentVersion.Major).$($CurrentVersion.Minor).$($CurrentVersion.Build)"
		
		dotnet publish $ExeProject `
			-p:AssemblyVersion=$SemVer `
			-p:FileVersion=$SemVer `
			-p:Version=$SemVer `
			--configuration $Configuration `
			--runtime $Runtime `
			--framework $TargetFramework `
			--output $OutputPath `
			--verbosity normal
		
		$ArchiveLookup = Join-Path $pwd $OutputPath
		
		If (!(Test-Path -PathType Container "artifacts")) {
			New-Item -Path "artifacts" -ItemType Directory | Out-Null
		}
		
		$ArchiveDestination = Join-Path $pwd "artifacts\v$($SemVer).zip"
		
		Compress-Archive "$($ArchiveLookup)\*" -DestinationPath $ArchiveDestination -CompressionLevel Optimal
		
		[Version]::Save($CurrentVersion, $VersionFilePath)
		
		# Clean
		Remove-Item -Path .\bin\ -Force -Recurse | Out-Null
		Remove-Item -Path $OutputPath -Force -Recurse | Out-Null
		
		exit(0)
	}
	
	# Run Build
	dotnet build $ExeProject `
		--configuration $Configuration `
		--no-restore

	# Run Tests
	dotnet test $ExeProject `
		--no-restore `
		--no-build `
		--logger:"Html;LogFileName=code-coverage-log.html" `
		--collect:"XPlat Code Coverage" `
		--results-directory ./code-coverage/ `
		--filter "Category!=RunsOnDevMachine" `
		--settings .runsettings

	# Install Report-Generator Tool
	dotnet tool install dotnet-reportgenerator-globaltool `
		--version 5.4.1 `
		--global
		
	# Generate Coverage Report
	reportgenerator `
		"-reports:./code-coverage/**/coverage.cobertura.xml" `
		"-targetdir:./code-coverage/report" `
		-reporttypes:Html
		
	# Clean
	Remove-Item -Path .\bin\ -Force -Recurse | Out-Null
	Remove-Item -Path $OutputPath -Force -Recurse | Out-Null
} Finally { Write-Verbose "[$($SCRIPT_NAME)] Performing cleanup..." }

# Display the time that this script finished running, and how long it took to run.
$ScriptFinishTime = Get-Date
$ScriptElapsedTime = ($ScriptFinishTime - $ScriptStartTime).TotalSeconds.ToString()
Write-Verbose "[$($SCRIPT_NAME)] Finished at $($ScriptFinishTime.TimeOfDay.ToString())."
Write-Verbose "Completed in $ScriptElapsedTime seconds."