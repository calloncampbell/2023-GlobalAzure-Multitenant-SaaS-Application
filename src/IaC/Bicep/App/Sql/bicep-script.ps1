param
(
    [Parameter(Mandatory, HelpMessage="Env")]
    [ValidateNotNullOrEmpty()]
    [ValidateSet("LabA", "LabB", "LabC",
        IgnoreCase = $false)]
    [String]
	$env,

    [Parameter(Mandatory, HelpMessage="Action. create = Modify infrastructure, validate = validate the bicep file. what-if = what will happen if the deploy command is used.")]
    [ValidateNotNullOrEmpty()]
    [ValidateSet("create", "validate", "what-if", IgnoreCase = $false)]
    [String]
    $action
)

$date = Get-Date -Format FileDateTimeUniversal
$subscription = ""
$paramerterFileLocation = ""
$deploymentName = "SaaSSql"
$locationName = ""

if (@("LabA", "LabB", "LabC").Contains($env)) {
    $subscription = "1ebbf115-096c-420f-9cde-42cefb07c19f"
}

#Labs
if ($env -eq "LabA") {
    $paramerterFileLocation = "./parameters.laba.json"
    $locationName = "CanadaCentral"
} elseif ($env -eq "LabB") {
    $paramerterFileLocation = "./parameters.labb.json"
    $locationName = "CanadaCentral"
} elseif ($env -eq "LabC") {
    $paramerterFileLocation = "./parameters.labc.json"
    $locationName = "CanadaCentral"
}

Write-Output "subscription: $subscription"
Write-Output "Location: $locationName"
Write-Output "ParameterFile: $paramerterFileLocation"
Write-Output "Action: $action"

# confirmation before modifying resources
$confirmation = Read-Host "Are you sure you want to proceed? (y/n)"
if ($confirmation -ne 'y') {
    return
}

az account set --subscription $subscription

if ($action -eq "create") {
    az deployment sub $action -c --location $locationName --name "$deploymentName-$date" --parameters $paramerterFileLocation --template-file "./main.bicep"
} else {
    az deployment sub $action --location $locationName --name "$deploymentName-$date" --parameters $paramerterFileLocation --template-file "./main.bicep"
}

