$branch = ""
$revision = ""

if ($env:path.ToLower().Contains("git") -and (Test-Path .git))
{
	$branch = git symbolic-ref --short HEAD # @git_branch
	$revision = git rev-parse HEAD # @git_commit_hash
}

$prefix = "." # @prefix@
$dist_version = "git" # @dist_version@

$twitter_key_content1 = Switch -regex (Get-Content -Path configure.ac) { "with_twitter_api_key=" { echo $switch.current } }
$twitter_key_content2 = $twitter_key_content1 -replace "^[^=]*=", ""
$twitter_api_key = $twitter_key_content2 -replace "[""]", ""  # @twitter_api_key

$defines_in = Get-Content src\Common\Defines.cs.in 
$defines = $defines_in -replace "@git_branch@", $branch -replace "@git_commit_hash@", $revision -replace "@prefix@", "." -replace "@dist_version@", "git" -replace "@twitter_api_key@", $twitter_api_key

Set-Content src\Common\Defines.cs $defines