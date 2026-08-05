# LeetCode 刷题记录自动更新脚本
# 用法: .\update-leetcode-tracker.ps1

$CppDir = "G:\CPPLearning\VS_CppCode\leet-code\Topic_1"
$CsDir = "G:\CSLearning\VS_CsCode\leet-code"
$TrackerFile = "G:\OpenCode\UnityClientDev\leetcode-tracker.md"

# 获取 C++ 最近提交的新题目
function Get-CppNewTopics {
    param($Dir)
    
    if (-not (Test-Path "$Dir\.git")) {
        return @()
    }
    
    Push-Location $Dir
    $newTopics = @()
    
    # 获取最近提交中新增的文件
    $files = git diff --name-only HEAD~1 HEAD 2>$null
    if ($files) {
        foreach ($file in $files) {
            if ($file -match 'Topic_(\d+)/') {
                $topicNum = $Matches[1]
                if ($topicNum -notin $newTopics) {
                    $newTopics += $topicNum
                }
            }
        }
    }
    
    Pop-Location
    return $newTopics
}

# 获取 C# 最近提交的新题目
function Get-CsNewTopics {
    param($Dir)
    
    if (-not (Test-Path "$Dir\.git")) {
        return @()
    }
    
    Push-Location $Dir
    $newTopics = @()
    
    # 获取最近提交中新增的文件
    $files = git diff --name-only HEAD~1 HEAD 2>$null
    if ($files) {
        foreach ($file in $files) {
            if ($file -match 'Topic_(\d+)/') {
                $topicNum = $Matches[1]
                if ($topicNum -notin $newTopics) {
                    $newTopics += $topicNum
                }
            }
        }
    }
    
    Pop-Location
    return $newTopics
}

# 获取所有已存在的题目
function Get-AllTopics {
    param($Dir)
    
    $topics = @()
    $topicDirs = Get-ChildItem -Path $Dir -Directory -Filter "Topic_*"
    
    foreach ($dir in $topicDirs) {
        if ($dir.Name -match 'Topic_(\d+)') {
            $topics += $Matches[1]
        }
    }
    
    return $topics
}

# 主逻辑
Write-Host "正在检查刷题目录..." -ForegroundColor Cyan

# 检查 C++
$cppAllTopics = Get-AllTopics -Dir $CppDir
$cppNewTopics = Get-CppNewTopics -Dir $CppDir

# 检查 C#
$csAllTopics = Get-AllTopics -Dir $CsDir
$csNewTopics = Get-CsNewTopics -Dir $CsDir

Write-Host "C++ 总题数: $($cppAllTopics.Count)" -ForegroundColor Green
Write-Host "C# 总题数: $($csAllTopics.Count)" -ForegroundColor Green

if ($cppNewTopics.Count -gt 0) {
    Write-Host "C++ 新增题目: $($cppNewTopics -join ', ')" -ForegroundColor Yellow
}

if ($csNewTopics.Count -gt 0) {
    Write-Host "C# 新增题目: $($csNewTopics -join ', ')" -ForegroundColor Yellow
}

# 读取现有 tracker
$trackerContent = Get-Content -Path $TrackerFile -Raw

# 更新 C# 部分
$csTableStart = $trackerContent.IndexOf("## C# 刷题记录")
$csTableEnd = $trackerContent.IndexOf("---", $csTableStart + 10)
$csSection = $trackerContent.Substring($csTableStart, $csTableEnd - $csTableStart)

# 更新 C++ 部分
$cppTableStart = $trackerContent.IndexOf("## C++ 刷题记录")
$cppTableEnd = $trackerContent.IndexOf("---", $cppTableStart + 10)
$cppSection = $trackerContent.Substring($cppTableStart, $cppTableEnd - $cppTableStart)

Write-Host "`n当前 C# 题目数: $(($csSection -split '\|' | Where-Object { $_ -match '^\s*\d+\s*$' }).Count / 4)" -ForegroundColor Cyan
Write-Host "当前 C++ 题目数: $(($cppSection -split '\|' | Where-Object { $_ -match '^\s*\d+\s*$' }).Count / 4)" -ForegroundColor Cyan

Write-Host "`n完成！如需更新 tracker，请运行: .\update-leetcode-tracker.ps1 -Update" -ForegroundColor Green
