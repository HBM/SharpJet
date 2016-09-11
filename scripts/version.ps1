param(
	[string]$git = "git",
	[string]$solutiondir = "solutiondir",
	[string]$assembly_info_template = "assembly_info_template",
	[string]$assembly_info = "assembly_info",
	[string]$name = "name",
	[string]$guid = "guid"
)

[System.Environment]::CurrentDirectory = $solutiondir

$rev_count = & $git rev-list HEAD --count
$dirty_string = & $git diff --shortstat
$is_dirty = $FALSE
if ($dirty_string) { $is_dirty = $TRUE }
$git_hash = & $git rev-parse --verify HEAD
$git_short_hash = & $git rev-parse --verify --short HEAD
& $git describe --exact-match --tags HEAD
$git_tag = $LastExitCode
$is_tag = ($git_tag -eq 0) -and !($is_dirty)

$template_content = Get-Content -Path $assembly_info_template
if ($is_tag) {
	$template_content = $template_content -replace "%revcount%", ""
	$template_content = $template_content -replace "%shorthash%", ""
	$template_content = $template_content -replace "%dirty%", ""
} else {
	$template_content = $template_content -replace "%revcount%", -$rev_count
	$template_content = $template_content -replace "%shorthash%", "+$git_short_hash"
	if ($is_dirty) {
		$template_content = $template_content -replace "%dirty%", '.dirty'
	} else {
		$template_content = $template_content -replace "%dirty%", ""
	}
}
$template_content = $template_content -replace "%name%", $name
$template_content = $template_content -replace "%guid%", $guid

$template_content | Out-File $assembly_info
