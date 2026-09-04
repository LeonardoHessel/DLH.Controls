param([Parameter(Mandatory)][string]$Tag)
$ErrorActionPreference = 'Stop'
# NuGet-compatible release tags; build metadata deliberately excluded.
if ($Tag -notmatch '^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$') {
    throw 'Use a release tag such as v0.1.0-preview.2 or v1.0.0.'
}
$version = $Tag.Substring(1)
if ($version.Contains('-')) {
    foreach ($part in $version.Split('-', 2)[1].Split('.')) {
        if ($part -match '^0[0-9]+$') { throw 'Numeric prerelease identifiers cannot have leading zeros.' }
    }
}
$version
