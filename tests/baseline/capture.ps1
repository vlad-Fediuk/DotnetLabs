param(
    [string]$BaseUrl = "http://localhost:5100",
    [string]$AuthUrl = "http://localhost:5001",
    [string]$OutDir = $PSScriptRoot,
    [ValidateSet("all", "catalog-down")]
    [string]$Mode = "all"
)

Add-Type -AssemblyName System.Net.Http

$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(30)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$validItemId = "11111111-2222-3333-4444-555555555553"
$missingItemId = "ffffffff-ffff-ffff-ffff-ffffffffffff"
$password = "Test123!"

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

function Send-Request {
    param([string]$Method, [string]$Url, [string]$Body, [string]$Token, [hashtable]$Headers = @{})

    $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::new($Method.ToUpper()), $Url)
    if ($Body) {
        $request.Content = New-Object System.Net.Http.StringContent($Body, [Text.Encoding]::UTF8, "application/json")
    }
    if ($Token) {
        $request.Headers.Add("Authorization", "Bearer $Token")
    }
    foreach ($key in $Headers.Keys) {
        $request.Headers.Add($key, $Headers[$key])
    }

    $response = $client.SendAsync($request).GetAwaiter().GetResult()
    $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    $headerLines = @()
    foreach ($name in "Location", "Retry-After", "Idempotency-Replayed") {
        $values = $null
        if ($response.Headers.TryGetValues($name, [ref]$values)) {
            $headerLines += "${name}: $($values -join ',')"
        }
    }
    $headerLines += "Content-Type: $($response.Content.Headers.ContentType)"

    [pscustomobject]@{ Status = [int]$response.StatusCode; Body = $content; Headers = $headerLines }
}

function Save-Capture {
    param([string]$Name, $Result)

    [IO.File]::WriteAllText((Join-Path $OutDir "$Name.json"), $Result.Body, $utf8NoBom)
    [IO.File]::WriteAllText((Join-Path $OutDir "$Name.status"), "$($Result.Status)`n", $utf8NoBom)
    [IO.File]::WriteAllText((Join-Path $OutDir "$Name.headers"), (($Result.Headers -join "`n") + "`n"), $utf8NoBom)
    Write-Host ("{0,-40} {1}" -f $Name, $Result.Status)
}

function Get-AccessToken {
    param([string]$Email, [string]$Pass, [string]$DisplayName)

    $credentials = @{ email = $Email; password = $Pass } | ConvertTo-Json -Compress
    $login = Send-Request -Method Post -Url "$AuthUrl/api/v1/auth/login" -Body $credentials
    if ($login.Status -ne 200) {
        $registration = @{ email = $Email; password = $Pass; displayName = $DisplayName } | ConvertTo-Json -Compress
        $null = Send-Request -Method Post -Url "$AuthUrl/api/v1/auth/register" -Body $registration
        $login = Send-Request -Method Post -Url "$AuthUrl/api/v1/auth/login" -Body $credentials
    }
    ($login.Body | ConvertFrom-Json).accessToken
}

function New-OrderBody {
    param([string]$ItemId, [int]$Quantity)
    @{ items = @(@{ productId = $ItemId; quantity = $Quantity }) } | ConvertTo-Json -Depth 5 -Compress
}

$ownerToken = Get-AccessToken -Email "baseline.owner@test.com" -Pass $password -DisplayName "Baseline Owner"
$otherToken = Get-AccessToken -Email "baseline.other@test.com" -Pass $password -DisplayName "Baseline Other"
$adminToken = Get-AccessToken -Email "admin@ecommerce.com" -Pass "Admin123!" -DisplayName "Admin"

if ($Mode -eq "catalog-down") {
    $result = Send-Request -Method Post -Url "$BaseUrl/api/orders" -Body (New-OrderBody $validItemId 1) -Token $ownerToken
    Save-Capture "09-catalog-unavailable" $result
    return
}

$validBody = New-OrderBody $validItemId 2

$created = Send-Request -Method Post -Url "$BaseUrl/api/orders" -Body $validBody -Token $ownerToken
Save-Capture "01-create-valid" $created
$orderId = ($created.Body | ConvertFrom-Json).id

Save-Capture "02-create-unauthorized" (Send-Request -Method Post -Url "$BaseUrl/api/orders" -Body $validBody)

Save-Capture "03-create-notfound-item" (Send-Request -Method Post -Url "$BaseUrl/api/orders" -Body (New-OrderBody $missingItemId 1) -Token $ownerToken)

Save-Capture "04-create-invalid-quantity" (Send-Request -Method Post -Url "$BaseUrl/api/orders" -Body (New-OrderBody $validItemId 0) -Token $ownerToken)

$idempotencyHeaders = @{ "Idempotency-Key" = [guid]::NewGuid().ToString() }
$firstAttempt = Send-Request -Method Post -Url "$BaseUrl/api/orders" -Body $validBody -Token $ownerToken -Headers $idempotencyHeaders
$secondAttempt = Send-Request -Method Post -Url "$BaseUrl/api/orders" -Body $validBody -Token $ownerToken -Headers $idempotencyHeaders
Save-Capture "05a-create-idempotent-first" $firstAttempt
Save-Capture "05-create-idempotent-repeat" $secondAttempt
$idempotencyCheck = [ordered]@{
    firstStatus  = $firstAttempt.Status
    secondStatus = $secondAttempt.Status
    sameOrder    = (($firstAttempt.Body | ConvertFrom-Json).id -eq ($secondAttempt.Body | ConvertFrom-Json).id)
} | ConvertTo-Json -Compress
[IO.File]::WriteAllText((Join-Path $OutDir "05-idempotency-check.json"), $idempotencyCheck, $utf8NoBom)
Write-Host "05-idempotency-check                     $idempotencyCheck"

Save-Capture "06-get-own-order" (Send-Request -Method Get -Url "$BaseUrl/api/orders/$orderId" -Token $ownerToken)

Save-Capture "07-get-foreign-order" (Send-Request -Method Get -Url "$BaseUrl/api/orders/$orderId" -Token $otherToken)

$cancelTarget = Send-Request -Method Post -Url "$BaseUrl/api/orders" -Body $validBody -Token $ownerToken
$cancelTargetId = ($cancelTarget.Body | ConvertFrom-Json).id
$cancelledStatusBody = '{"status":5}'
Save-Capture "08-cancel-order" (Send-Request -Method Patch -Url "$BaseUrl/api/orders/$cancelTargetId/status" -Body $cancelledStatusBody -Token $adminToken)
Save-Capture "08b-cancel-repeat" (Send-Request -Method Patch -Url "$BaseUrl/api/orders/$cancelTargetId/status" -Body $cancelledStatusBody -Token $adminToken)
Save-Capture "08c-cancel-owner-route" (Send-Request -Method Post -Url "$BaseUrl/api/orders/$cancelTargetId/cancel" -Token $ownerToken)

Save-Capture "10-get-user-orders-list" (Send-Request -Method Get -Url "$BaseUrl/api/orders" -Token $ownerToken)
