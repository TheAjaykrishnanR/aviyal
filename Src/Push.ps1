$commit_msg = $args[0]
if($commit_msg -eq $null) {
	Write-Host "Provide a commit message"
	Exit
}

csharpier format .
git_all $commit_msg ".."
