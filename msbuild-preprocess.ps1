$branch = ""
$revision = ""

$hasGit = $false

if ($env:path.ToLower().Contains("git") -and (Test-Path .git))
{
    $hasGit = $true
	$branch = git symbolic-ref --short HEAD # @git_branch
	$revision = git rev-parse HEAD # @git_commit_hash
}

# Task count
$tCount = 3

# Defines.cs

Write-Output "[1/$tCount] Generating Defines.cs file..."

$prefix = "." # @prefix@
$dist_version = "git" # @dist_version@

$twitter_key_content1 = Switch -regex (Get-Content -Path configure.ac) { "with_twitter_api_key=" { echo $switch.current } }
$twitter_key_content2 = $twitter_key_content1 -replace "^[^=]*=", ""
$twitter_api_key = $twitter_key_content2 -replace "[""]", ""  # @twitter_api_key

$defines_in = Get-Content "src\Common\Defines.cs.in" 
$defines = $defines_in -replace "@git_branch@", $branch -replace "@git_commit_hash@", $revision -replace "@prefix@", "." -replace "@dist_version@", "git" -replace "@twitter_api_key@", $twitter_api_key

Set-Content src\Common\Defines.cs $defines
Write-Output "  -> Defines.cs file generated."

# Adapted configs for Win32 smuxi-frontend-gnome.exe Dev and Deploy

$fGnome = "src\Frontend-GNOME\smuxi-frontend-gnome.exe.config"

Write-Output "[2/$tCount] Fixing '$fGnome' for Win32"
Copy-Item -Force "lib\win32\smuxi-frontend-gnome.exe.config" $fGnome
Write-Warning "If you plan to do some hacking please don't commit '$fGnome'"
Write-Output "  -> $fGnome fixed."

# Adapted configs for Win32 smuxi-server.exe Dev and Deploy

$fServer = "src\Server\smuxi-server.exe.config"

Write-Output "[3/$tCount] Fixing '$fServer' for Win32"
Copy-Item -Force "lib\win32\smuxi-server.exe.config" $fServer
Write-Warning "If you plan to do some hacking please don't commit '$fServer'"
Write-Output "  -> $fServer fixed."
