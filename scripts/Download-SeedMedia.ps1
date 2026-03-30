$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$wwwroot = Join-Path $root "API\wwwroot\demo-media"

function Ensure-Directory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Download-File {
    param(
        [string]$Url,
        [string]$Destination
    )

    $directory = Split-Path -Parent $Destination
    Ensure-Directory -Path $directory

    Write-Host "Downloading $Url"
    Invoke-WebRequest -Uri $Url -OutFile $Destination -Headers @{ "User-Agent" = "Mozilla/5.0" }
}

$products = @(
    @{
        Slug = "dag-cilegi-receli"
        ImageQueries = @("strawberry,jam,jar", "berry,spread,jar", "homemade,jam,breakfast")
        VideoUrl = "https://samplelib.com/lib/preview/mp4/sample-5s.mp4"
    },
    @{
        Slug = "yayla-bali"
        ImageQueries = @("honey,jar", "beekeeper,honey", "golden,honey,food", "organic,honey")
        VideoUrl = "https://samplelib.com/lib/preview/mp4/sample-10s.mp4"
    },
    @{
        Slug = "koy-yumurtasi"
        ImageQueries = @("eggs,basket", "brown,eggs", "free-range,eggs", "farm,eggs")
        VideoUrl = "https://samplelib.com/lib/preview/mp4/sample-15s.mp4"
    },
    @{
        Slug = "organik-karsari-peyniri"
        ImageQueries = @("cheese,wheel", "artisan,cheese", "cheese,board", "dairy,cheese")
        VideoUrl = "https://samplelib.com/lib/preview/mp4/sample-20s.mp4"
    },
    @{
        Slug = "taze-kivircik-marul"
        ImageQueries = @("lettuce,green", "leafy,greens", "organic,lettuce")
        VideoUrl = "https://samplelib.com/lib/preview/mp4/sample-5s.mp4"
    },
    @{
        Slug = "ata-tohumu-domates"
        ImageQueries = @("heirloom,tomatoes", "organic,tomatoes", "fresh,tomatoes", "tomatoes,harvest")
        VideoUrl = "https://samplelib.com/lib/preview/mp4/sample-10s.mp4"
    },
    @{
        Slug = "kurutulmus-elma-dilimi"
        ImageQueries = @("dried,apple", "apple,slices", "fruit,snack")
        VideoUrl = "https://samplelib.com/lib/preview/mp4/sample-15s.mp4"
    },
    @{
        Slug = "tas-degirmen-kirmizi-mercimek"
        ImageQueries = @("red,lentils", "lentils,bowl", "lentils,grain")
        VideoUrl = "https://samplelib.com/lib/preview/mp4/sample-20s.mp4"
    }
)

foreach ($product in $products) {
    $productRoot = Join-Path $wwwroot $product.Slug
    if (Test-Path -LiteralPath $productRoot) {
        Remove-Item -LiteralPath $productRoot -Recurse -Force
    }

    for ($i = 0; $i -lt $product.ImageQueries.Count; $i++) {
        $query = $product.ImageQueries[$i]
        $url = "https://loremflickr.com/1280/960/$query/all?lock=$($i + 1)"
        $destination = Join-Path $wwwroot ("{0}\\resimler\\{1}.jpg" -f $product.Slug, ($i + 1))
        Download-File -Url $url -Destination $destination
    }

    $videoDestination = Join-Path $wwwroot ("{0}\\videolar\\1.mp4" -f $product.Slug)
    Download-File -Url $product.VideoUrl -Destination $videoDestination
}

Write-Host "Anahtar kelime bazli demo medya indirme tamamlandi."
