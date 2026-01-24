#Requires -Version 2.0
<#
    .SYNOPSIS
    Executes EF Core migration or update routines.

    .DESCRIPTION
    Executes EF Core migration or update routines.

    .LINK
	Project home: https://github.com/marcoaoteixeira/InfoPhoenix

	.NOTES
	Author:
	Version: 1.0.0
	
	This script is designed to be called from PowerShell.
#>
[CmdletBinding()]
Param (
    [Parameter(Mandatory = $true, HelpMessage = "The action to execute.")]
	[Alias("a")]
    [String]$Action = $null,

	[Parameter(Mandatory = $false, HelpMessage = "The migration name.")]
	[Alias("m")]
    [String]$MigrationName = $null,
	
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

# Project Variables
$DBCONTEXT_PROJECT_PATH = [System.IO.Path]::Combine("src", "Nameless.InfoPhoenix.Core", "Nameless.InfoPhoenix.Core.csproj")
$MIGRATION_OUTPUT_PATH = [System.IO.Path]::Combine("Data", "Migrations")

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

Function Execute-Database-Migration([String]$migrationName) {
    dotnet ef migrations add $migrationName -s $DBCONTEXT_PROJECT_PATH -p $DBCONTEXT_PROJECT_PATH -o $MIGRATION_OUTPUT_PATH
}

Function Execute-Database-Update() {
    dotnet ef database update -s $DBCONTEXT_PROJECT_PATH -p $DBCONTEXT_PROJECT_PATH
}

#==========================================================
# Perform the script tasks.
#==========================================================

# Display the time that this script started running.
$ScriptStartTime = Get-Date
Write-Verbose "[$($SCRIPT_NAME)] Starting at $($ScriptStartTime.TimeOfDay.ToString())."

# Display the version of PowerShell being used to run the script, as this can help solve some problems that are hard to reproduce on other machines.
Write-Verbose "Using PowerShell Version: $($PSVersionTable.PSVersion.ToString())."

Try {
    If ($Action -ieq "Migration") {
        Execute-Database-Migration $MigrationName
    }

    If ($Action -ieq "Update") {
        Execute-Database-Update
    }
}
Finally {
    Write-Verbose "[$($SCRIPT_NAME)] Performing cleanup..."
}

# Display the time that this script finished running, and how long it took to run.
$ScriptFinishTime = Get-Date
$ScriptElapsedTime = ($ScriptFinishTime - $ScriptStartTime).TotalSeconds.ToString()
Write-Verbose "[$($SCRIPT_NAME)] Finished at $($ScriptFinishTime.TimeOfDay.ToString())."
Write-Verbose "Completed in $ScriptElapsedTime seconds."