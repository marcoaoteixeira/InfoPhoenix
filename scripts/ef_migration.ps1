#Requires -Version 2.0
<#
    .SYNOPSIS
    EF Core Migration

    .DESCRIPTION
    Executes EF Core migration routine

    .LINK
	Project home: https://github.com/marcoaoteixeira/infophoenix

	.NOTES
	Author:
	Version: 1.0.0
	
	This script is designed to be called from PowerShell.
#>
[CmdletBinding()]
Param (
	[Parameter(Mandatory = $true, HelpMessage = "The startup project.")]
	[Alias("s")]
    [String]$StartupProject = $null,
	
	[Parameter(Mandatory = $true, HelpMessage = "The project where DbContext resides.")]
	[Alias("p")]
    [String]$DbContextProject = $null,
	
	[Parameter(Mandatory = $true, HelpMessage = "The migration name.")]
	[Alias("m")]
    [String]$MigrationName = $null,
	
	[Parameter(Mandatory = $false, HelpMessage = "The output location.")]
	[Alias("o")]
    [String]$OutputPath = "./Data/Migrations",
	
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

Try { dotnet ef migrations add $MigrationName -s $StartupProject -p $DbContextProject -o $OutputPath }
Finally { Write-Verbose "[$($SCRIPT_NAME)] Performing cleanup..." }

# Display the time that this script finished running, and how long it took to run.
$ScriptFinishTime = Get-Date
$ScriptElapsedTime = ($ScriptFinishTime - $ScriptStartTime).TotalSeconds.ToString()
Write-Verbose "[$($SCRIPT_NAME)] Finished at $($ScriptFinishTime.TimeOfDay.ToString())."
Write-Verbose "Completed in $ScriptElapsedTime seconds."