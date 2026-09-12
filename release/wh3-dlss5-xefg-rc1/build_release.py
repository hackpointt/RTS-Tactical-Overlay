#!/usr/bin/env python3
from pathlib import Path
import argparse, hashlib, json, re, shutil, zipfile

BASE_OPTISCALER = "5c5e424dd137d69ef36c4231fd45b760b4c65cc8"
V15_RUN = 34704880248
V15_ARTIFACT_ID = 10300728399
V15_ARTIFACT_SHA256 = "81600e9bf9789d85ccea2431bc9ecb86fd8807f877a55f3375f65668012d87a7"
ROOT_NAME = "WH3-Native-DLSS5-XeFG-RC1-v15"

TEXT_FILES = {
    'README.md': '# Total War: WARHAMMER III — Native DLSS5 → XeFG RC1\n\nAn experimental compatibility package that keeps **ShortFuse/native NVIDIA DLSS5** as the game\'s upscaling path while feeding its native D3D12 **Depth + Motion Vectors** into **OptiFG → Intel XeFG** for frame generation.\n\nThis is an **unofficial community experiment**. It is not affiliated with Creative Assembly, SEGA, NVIDIA, Intel, ReShade, ShortFuse, or OptiScaler.\n\n## What has been validated\n\n- Total War: WARHAMMER III running in DX11.\n- Native NVIDIA D3D12 DLSS evaluation remains passthrough; the patch does **not** replace the ShortFuse DLSS evaluator.\n- Native DLSS Depth/MV are bridged through a private metadata-only adapter into OptiFG.\n- XeFG owns the final DX12 interop presenter and successfully dispatches generated frames.\n- Stable full battles were tested at 2560×1440 and 3840×2160 on an RTX 4090.\n- The OptiScaler menu exposes a four-stage live health check:\n\n```text\nDLSS5 feeder:     DETECTED\nNative DLSS NGX:  ACTIVE\nDepth + MV:       READY\nXeFG:             ACTIVE\n```\n\n\n## Before installing\n\n**ShortFuse/DLSS5 is not redistributed in this package.** Install and configure your existing ReShade + ShortFuse DLSS5 setup first. Before adding this RC1, launch WH3 and make sure your desired DLSS5 preset/model is already working.\n\nWhy configure it first? Once XeFG owns the final Present path, the ReShade overlay may no longer be visible even though the ShortFuse addon itself continues to run.\n\nFor the RC1 validation path, also disable:\n\n- NVIDIA Smooth Motion / NVIDIA App AI frame generation for WH3.\n- RTSS overlay/hooking for WH3.\n\n## Quick install\n\nThe safest route is the included PowerShell installer:\n\n```powershell\npowershell -ExecutionPolicy Bypass -File .\\tools\\Install-WH3-DLSS5-XeFG.ps1 -GameDir "D:\\SteamLibrary\\steamapps\\common\\Total War WARHAMMER III"\n```\n\nIt validates the game directory and ShortFuse/ReShade prerequisites, backs up the current `dxgi.dll`, `OptiScaler.ini`, and `OptiScaler` directory, then installs the RC1 payload.\n\nManual installation is documented in `docs/INSTALL.md`.\n\n## First launch\n\n1. Start WH3 normally.\n2. Enter a real 3D battle first. Do **not** enable frame generation from the main menu for the first test.\n3. Open the OptiScaler menu.\n4. Confirm `FG Input = OptiFG (Upscaler)` and `FG Output = XeFG`.\n5. Enable **Frame Generation (XeFG) → Active**.\n6. Wait for the four live status lines to become `DETECTED / ACTIVE / READY / ACTIVE`.\n7. Close the menu and play for several minutes before changing any other settings.\n\nThe packaged `OptiScaler.ini` intentionally starts with frame generation disabled, while preselecting the tested input/output path.\n\n## Uninstall / rollback\n\nRun:\n\n```powershell\npowershell -ExecutionPolicy Bypass -File .\\tools\\Uninstall-WH3-DLSS5-XeFG.ps1 -GameDir "D:\\SteamLibrary\\steamapps\\common\\Total War WARHAMMER III"\n```\n\nThe installer records its backup location and the uninstall script restores the previous files.\n\n## Known limitations\n\n- The ReShade UI may be invisible after XeFG takes over the final Present chain. This does **not** by itself mean DLSS5 is off; use the four live status lines instead.\n- Only the RTX 4090 path has been validated so far. Other GPUs are experimental.\n- Smooth Motion and RTSS were disabled during validation and should remain off for RC1 testing.\n- HDR, unusual overlays, other swapchain injectors and alternate proxy DLL names are not part of the validated matrix.\n- This is an RC1, not a production-supported mod. Keep the rollback backup.\n\n## Technical source\n\nThe package includes `source/WH3-native-DLSS5-XeFG.patch`, based on OptiScaler commit:\n\n`5c5e424dd137d69ef36c4231fd45b760b4c65cc8`\n\nThe patch is the reviewable source of the compatibility changes used by this binary.\n\nSee `docs/TECHNICAL.md` for the architecture and `docs/TROUBLESHOOTING.md` for failure-state interpretation.\n',
    'README.zh-CN.md': '# Total War: WARHAMMER III — Native DLSS5 → XeFG RC1\n\n这是一个实验性兼容包：**保留 ShortFuse / NVIDIA 原生 DLSS5 作为超分辨率链路**，同时从 native D3D12 DLSS 调用中取得 **Depth + Motion Vectors**，桥接给 **OptiFG → Intel XeFG** 做帧生成。\n\n这不是 Creative Assembly、SEGA、NVIDIA、Intel、ReShade、ShortFuse 或 OptiScaler 的官方版本。\n\n## 已验证\n\n- WH3 DX11。\n- NVIDIA native D3D12 DLSS Create/Evaluate 保持原生 passthrough。\n- Depth / MV 通过 private shadow adapter 喂给 OptiFG，不把 shadow 冒充为正常 upscaler。\n- XeFG 成功接管 DX11→DX12 interop 最终 Presenter 并持续 Dispatch。\n- RTX 4090 下 2560×1440 与 3840×2160 均完成整局战斗稳定性测试。\n- OptiScaler 菜单中的成功判据：\n\n```text\nDLSS5 feeder:     DETECTED\nNative DLSS NGX:  ACTIVE\nDepth + MV:       READY\nXeFG:             ACTIVE\n```\n\n\n## 安装前必须做\n\n本包**不重新分发 ShortFuse / DLSS5**。请先安装好你自己的 ReShade + ShortFuse DLSS5，并在安装本 RC1 前启动一次 WH3，确认 DLSS5 已工作且你已经选好想用的 preset/model。\n\n原因：XeFG 接管最终 Present 后，ReShade 面板可能无法显示，但 ShortFuse addon 本身仍可继续正常运行。\n\nRC1 验证环境还要求先关闭：\n\n- NVIDIA App 的 Smooth Motion / AI 插帧。\n- RTSS 对 WH3 的 overlay / hook。\n\n## 快速安装\n\n推荐用包内 PowerShell 安装器：\n\n```powershell\npowershell -ExecutionPolicy Bypass -File .\\tools\\Install-WH3-DLSS5-XeFG.ps1 -GameDir "D:\\SteamLibrary\\steamapps\\common\\Total War WARHAMMER III"\n```\n\n安装器会检查 `Warhammer3.exe`、ReShade / ShortFuse，备份当前 `dxgi.dll`、`OptiScaler.ini` 和 `OptiScaler` 目录，再安装 RC1。\n\n手动安装见 `docs/INSTALL.md`。\n\n## 第一次启动\n\n1. 正常启动 WH3。\n2. 先进入真正的 3D 战斗；第一次测试不要在主菜单开启 FG。\n3. 打开 OptiScaler。\n4. 确认 `FG Input = OptiFG (Upscaler)`、`FG Output = XeFG`。\n5. 勾选 `Frame Generation (XeFG) → Active`。\n6. 等待四行状态变成 `DETECTED / ACTIVE / READY / ACTIVE`。\n7. 关闭面板，正常打一段时间，再考虑改其他选项。\n\n随包配置故意让 FG 默认保持关闭，但已经预选测试通过的 Input/Output。\n\n## 回滚\n\n```powershell\npowershell -ExecutionPolicy Bypass -File .\\tools\\Uninstall-WH3-DLSS5-XeFG.ps1 -GameDir "D:\\SteamLibrary\\steamapps\\common\\Total War WARHAMMER III"\n```\n\n它会读取安装记录并恢复安装前文件。\n\n## 已知限制\n\n- XeFG 接管最终 Present 后，ReShade UI 可能不可见；这不代表 DLSS5 关闭，请看“四绿”。\n- 当前只正式验证 RTX 4090，其他 GPU 仍属于实验范围。\n- 验证时 Smooth Motion 与 RTSS 均关闭。\n- HDR、其他 overlay / swapchain injector、其他 proxy DLL 名称未纳入当前验证矩阵。\n- RC1 仍属于社区实验版本，务必保留回滚备份。\n\n## 源码\n\n`source/WH3-native-DLSS5-XeFG.patch` 是基于以下 OptiScaler commit 的单一 consolidated patch：\n\n`5c5e424dd137d69ef36c4231fd45b760b4c65cc8`\n',
    'VERSION.txt': 'WH3 Native DLSS5 -> XeFG RC1\nImplementation build: V15\nRelease date: 2026-09-13\nBase OptiScaler commit: 5c5e424dd137d69ef36c4231fd45b760b4c65cc8\nValidated game: Total War: WARHAMMER III 8.1.0 (DX11)\nValidated GPU: NVIDIA GeForce RTX 4090\nValidated resolutions: 2560x1440 and 3840x2160\n',
    'BUILD_INFO.json': '{\n  "name": "WH3 Native DLSS5 -> XeFG RC1",\n  "implementation": "V15",\n  "release_date": "2026-09-13",\n  "base_optiscaler_commit": "5c5e424dd137d69ef36c4231fd45b760b4c65cc8",\n  "github_workflow_run": 34704880248,\n  "github_artifact_id": 10300728399,\n  "github_source_artifact_sha256": "81600e9bf9789d85ccea2431bc9ecb86fd8807f877a55f3375f65668012d87a7",\n  "validated": {\n    "game": "Total War: WARHAMMER III 8.1.0",\n    "api": "DX11 with DX11->DX12 interop presenter",\n    "gpu": "NVIDIA GeForce RTX 4090",\n    "resolutions": ["2560x1440", "3840x2160"],\n    "smooth_motion": "off",\n    "rtss": "off"\n  }\n}\n',
    'docs/INSTALL.md': '# Installation\n\n## Prerequisites\n\n1. A working Total War: WARHAMMER III DX11 installation.\n2. ReShade available as `ReShade64.dll` in the WH3 game directory (the tested OptiScaler loading arrangement).\n3. ShortFuse `dlss5-feed.addon64` installed somewhere below the WH3 game directory and already validated in-game.\n4. NVIDIA Smooth Motion off for WH3.\n5. RTSS closed or WH3 excluded from RTSS hooking.\n\n## Automated install\n\nRun from an ordinary PowerShell window:\n\n```powershell\npowershell -ExecutionPolicy Bypass -File .\\tools\\Install-WH3-DLSS5-XeFG.ps1 -GameDir "<WH3 folder>"\n```\n\nThe installer is conservative: it refuses a folder without `Warhammer3.exe`, checks for ReShade and ShortFuse, and creates a timestamped backup before replacing OptiScaler-related files.\n\n## Manual install\n\nIf you prefer full control:\n\n1. Back up these items if they exist in the WH3 executable directory:\n   - `dxgi.dll`\n   - `OptiScaler.ini`\n   - `OptiScaler\\`\n2. Copy `runtime\\OptiScaler\\` into the WH3 executable directory as `OptiScaler\\`.\n3. Copy `runtime\\OptiScaler.dll` into the WH3 executable directory and rename the copy to `dxgi.dll`.\n4. Copy `config\\OptiScaler.ini` into the WH3 executable directory.\n5. Keep your existing `ReShade64.dll` and ShortFuse addon installation in place.\n\nThe RC1 preset contains the tested values:\n\n```ini\n[FrameGen]\nEnabled=false\nFGInput=upscaler\nFGOutput=xefg\n\n[Inputs]\nEnableDlssInputs=true\n\n[Plugins]\nLoadReshade=true\n```\n\n`Enabled=false` is intentional. Enable XeFG manually only after entering a 3D battle on the first run.\n',
    'docs/TROUBLESHOOTING.md': '# Troubleshooting\n\nUse the WH3-specific live status block in the OptiScaler menu as the primary diagnostic.\n\n## `DLSS5 feeder: not detected`\n\nThe `dlss5-feed.addon64` module is not loaded. Confirm ShortFuse is installed and that ReShade is being loaded through `ReShade64.dll`. Configure and validate DLSS5 before troubleshooting XeFG.\n\n## `Native DLSS NGX: inactive`\n\nThe real native D3D12 NVIDIA DLSS Evaluate call has not succeeded recently. This usually means the ShortFuse/native DLSS path is not actually running yet. Enter a 3D scene and verify your ShortFuse installation.\n\n## `Depth + MV: not ready`\n\nThe bridge has not tagged both Depth and Motion Vector inputs. Make sure you are in a 3D battle, `FG Input = OptiFG (Upscaler)`, and frame generation has been enabled.\n\n## `XeFG: inactive`\n\nCheck `FG Output = XeFG` and enable `Frame Generation (XeFG) → Active`. XeFG needs a short warm-up after activation.\n\n## Main menu or battle freezes\n\nFor RC1, first restore the validated environment:\n\n- Smooth Motion off.\n- RTSS off / excluded.\n- Use the supplied `dxgi.dll` (copy of the RC1 `OptiScaler.dll`).\n- Use the supplied RC1 `OptiScaler.ini`.\n- Do not mix older experimental V2–V14 DLLs with the V15 runtime directory.\n\nIf the problem persists, roll back with the uninstall script and verify that vanilla WH3 + your standalone ShortFuse/DLSS5 setup is stable.\n\n## ReShade menu does not appear\n\nThis is a known limitation. XeFG owns the final DX12 interop presenter, and the ReShade overlay path may not be composed into the final output. The ShortFuse addon can still be active. Use the four live OptiScaler status lines to verify DLSS5 and XeFG.\n\nFor now, choose your ShortFuse/DLSS5 preset **before installing/enabling RC1**.\n\n## Need a debug log\n\nOnly for troubleshooting, edit `OptiScaler.ini` and enable file logging / a verbose log level. Restore normal logging afterward; full trace logs can become very large.\n',
    'docs/RELEASE_NOTES.md': '# RC1 Release Notes\n\n**Release:** WH3 Native DLSS5 → XeFG RC1  \n**Implementation:** V15 convergence build  \n**Date:** 2026-09-13  \n**Base OptiScaler commit:** `5c5e424dd137d69ef36c4231fd45b760b4c65cc8`\n\n## Included\n\n- V15 OptiScaler compatibility binary.\n- Tested WH3 RC1 configuration preset.\n- Consolidated source patch.\n- Conservative install / rollback scripts.\n- Four-state runtime health panel.\n- Bilingual release documentation.\n\n## Validation matrix\n\n| Item | Result |\n|---|---|\n| WH3 DX11 | Passed |\n| RTX 4090 | Passed |\n| 2560×1440 battle | Passed |\n| 3840×2160 battle | Passed |\n| Native DLSS passthrough | Passed |\n| Depth + MV bridge | Passed |\n| XeFG activation / dispatch | Passed |\n| Full battle stability | Passed |\n| ReShade overlay visibility after XeFG | Known limitation |\n| Smooth Motion coexistence | Not supported in RC1 validation |\n| RTSS coexistence | Not supported in RC1 validation |\n',
    'docs/THIRD_PARTY.md': '# Third-party components\n\nThis release candidate is built around OptiScaler and its runtime dependencies. Their license files are preserved under `docs/Licenses/`.\n\nShortFuse / `dlss5-feed.addon64` and ReShade are **not redistributed** by this package. Users must obtain and install those components separately from their respective sources and comply with their licenses.\n\nTotal War: WARHAMMER III, Creative Assembly, SEGA, NVIDIA, Intel, ReShade, ShortFuse and OptiScaler trademarks belong to their respective owners.\n',
    'docs/TECHNICAL.md': '# Technical Architecture\n\n## Problem\n\nWH3 is a DX11 title without a native frame-generation path. The working DLSS5 setup reaches NVIDIA NGX through a ShortFuse-created D3D12 path. A naive OptiScaler DLSS interception caused the visual pipeline to freeze, while disabling DLSS interception preserved DLSS5 but left OptiFG without an upscaler input.\n\n## RC1 architecture\n\n```text\nWH3 DX11\n   |\n   +--> ReShade / ShortFuse DLSS5\n   |       |\n   |       +--> native NVIDIA D3D12 NGX SuperSampling Create/Evaluate\n   |                 |\n   |                 +--> NVIDIA DLSS5 output (unchanged passthrough)\n   |                 |\n   |                 +--> private metadata-only shadow adapter\n   |                           |\n   |                           +--> Depth + Motion Vectors\n   |                                      |\n   |                                   OptiFG\n   |                                      |\n   +---------------- DX11 -> DX12 interop -+--> XeFG presenter\n                                                   |\n                                             generated frames\n```\n\nThe private shadow is deliberately **not** assigned to `State::currentFeature`. Earlier experiments proved that publishing the metadata-only object as a normal upscaler could push unrelated Present/GPU-timing code down an invalid lifecycle path and freeze the frame.\n\n## XeFG swapchain compatibility\n\nTwo relevant conditions were discovered during isolation:\n\n1. WH3\'s converted interop swapchain included `DXGI_USAGE_UNORDERED_ACCESS`; XeFG\'s `CreateSwapChainForHwnd` rejected the resulting descriptor with `DXGI_ERROR_INVALID_CALL`. RC1 removes only the UAV usage for the WH3 XeFG path.\n2. The windowed XeFG path omits the fullscreen descriptor when initializing the XeFG swapchain.\n\n`DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING` remains preserved. Removing it caused a later mismatch when WH3 presented with `DXGI_PRESENT_ALLOW_TEARING`.\n\n## Runtime observability\n\nRC1 exposes four live states:\n\n- `DLSS5 feeder: DETECTED`: `dlss5-feed.addon64` is loaded.\n- `Native DLSS NGX: ACTIVE`: a real native D3D12 DLSS Evaluate succeeded within the recent activity window.\n- `Depth + MV: READY`: both FG resources are currently tagged.\n- `XeFG: ACTIVE`: the actual XeFG instance is active and not paused.\n\n## Source scope\n\nThe consolidated patch modifies exactly the compatibility-related OptiScaler source areas used by the experiment and is based on commit:\n\n`5c5e424dd137d69ef36c4231fd45b760b4c65cc8`\n',
    'tools/Install-WH3-DLSS5-XeFG.ps1': 'param(\n    [Parameter(Mandatory = $true)]\n    [string]$GameDir\n)\n\n$ErrorActionPreference = \'Stop\'\n$GameDir = (Resolve-Path $GameDir).Path\n$PackageRoot = (Resolve-Path (Join-Path $PSScriptRoot \'..\')).Path\n$RuntimeDir = Join-Path $PackageRoot \'runtime\'\n$ConfigPath = Join-Path $PackageRoot \'config\\OptiScaler.ini\'\n\nfunction Fail([string]$Message) {\n    Write-Host "ERROR: $Message" -ForegroundColor Red\n    exit 1\n}\n\nif (-not (Test-Path (Join-Path $GameDir \'Warhammer3.exe\'))) {\n    Fail "Warhammer3.exe was not found in: $GameDir"\n}\nif (-not (Test-Path (Join-Path $RuntimeDir \'OptiScaler.dll\'))) {\n    Fail "Package runtime is incomplete: runtime\\OptiScaler.dll is missing."\n}\nif (-not (Test-Path $ConfigPath)) {\n    Fail "Package config is incomplete: config\\OptiScaler.ini is missing."\n}\n\n$reshade = Join-Path $GameDir \'ReShade64.dll\'\nif (-not (Test-Path $reshade)) {\n    Fail "ReShade64.dll was not found in the WH3 game directory. Install/configure ReShade + ShortFuse first."\n}\n\n$shortFuse = Get-ChildItem -Path $GameDir -Filter \'dlss5-feed.addon64\' -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1\nif (-not $shortFuse) {\n    Fail "dlss5-feed.addon64 was not found under the WH3 game directory. Install/configure ShortFuse first."\n}\n\nWrite-Host "WH3 directory: $GameDir"\nWrite-Host "ReShade:       $reshade"\nWrite-Host "ShortFuse:     $($shortFuse.FullName)"\nWrite-Host ""\nWrite-Host "IMPORTANT: NVIDIA Smooth Motion and RTSS should be disabled for RC1 validation." -ForegroundColor Yellow\nWrite-Host ""\n\n$stamp = Get-Date -Format \'yyyyMMdd-HHmmss\'\n$backupRoot = Join-Path $GameDir "_WH3-DLSS5-XeFG-RC1-backup\\$stamp"\nNew-Item -ItemType Directory -Force -Path $backupRoot | Out-Null\n\n$targets = @(\n    @{ Rel = \'dxgi.dll\'; Kind = \'File\' },\n    @{ Rel = \'OptiScaler.ini\'; Kind = \'File\' },\n    @{ Rel = \'OptiScaler\'; Kind = \'Directory\' }\n)\n\n$records = @()\nforeach ($target in $targets) {\n    $dst = Join-Path $GameDir $target.Rel\n    $exists = Test-Path $dst\n    $records += [pscustomobject]@{ path = $target.Rel; existed = $exists }\n    if ($exists) {\n        $backupDst = Join-Path $backupRoot $target.Rel\n        if ($target.Kind -eq \'Directory\') {\n            New-Item -ItemType Directory -Force -Path (Split-Path $backupDst -Parent) | Out-Null\n            Copy-Item -Path $dst -Destination $backupDst -Recurse -Force\n        }\n        else {\n            New-Item -ItemType Directory -Force -Path (Split-Path $backupDst -Parent) | Out-Null\n            Copy-Item -Path $dst -Destination $backupDst -Force\n        }\n    }\n}\n\n# Install dependency directory first.\n$dstOpti = Join-Path $GameDir \'OptiScaler\'\nif (Test-Path $dstOpti) { Remove-Item $dstOpti -Recurse -Force }\nCopy-Item -Path (Join-Path $RuntimeDir \'OptiScaler\') -Destination $dstOpti -Recurse -Force\n\n# OptiScaler is used as the DXGI proxy in the validated WH3 path.\nCopy-Item -Path (Join-Path $RuntimeDir \'OptiScaler.dll\') -Destination (Join-Path $GameDir \'dxgi.dll\') -Force\nCopy-Item -Path $ConfigPath -Destination (Join-Path $GameDir \'OptiScaler.ini\') -Force\n\n$installRecord = [pscustomobject]@{\n    package = \'WH3 Native DLSS5 -> XeFG RC1\'\n    implementation = \'V15\'\n    installed_at = (Get-Date).ToString(\'o\')\n    backup_dir = $backupRoot\n    targets = $records\n}\n$recordPath = Join-Path $GameDir \'.wh3-dlss5-xefg-rc1-install.json\'\n$installRecord | ConvertTo-Json -Depth 5 | Set-Content -Path $recordPath -Encoding UTF8\n\nWrite-Host ""\nWrite-Host "Installed WH3 Native DLSS5 -> XeFG RC1." -ForegroundColor Green\nWrite-Host "Backup: $backupRoot"\nWrite-Host ""\nWrite-Host "First run:" -ForegroundColor Cyan\nWrite-Host "  1. Enter a 3D battle before enabling FG."\nWrite-Host "  2. Open OptiScaler."\nWrite-Host "  3. Enable Frame Generation (XeFG) -> Active."\nWrite-Host "  4. Verify DETECTED / ACTIVE / READY / ACTIVE."\n',
    'tools/Uninstall-WH3-DLSS5-XeFG.ps1': 'param(\n    [Parameter(Mandatory = $true)]\n    [string]$GameDir\n)\n\n$ErrorActionPreference = \'Stop\'\n$GameDir = (Resolve-Path $GameDir).Path\n$recordPath = Join-Path $GameDir \'.wh3-dlss5-xefg-rc1-install.json\'\n\nif (-not (Test-Path $recordPath)) {\n    Write-Host "No RC1 install record was found at $recordPath" -ForegroundColor Yellow\n    Write-Host "Nothing was changed. For a manual install, restore your own backups manually."\n    exit 1\n}\n\n$record = Get-Content $recordPath -Raw | ConvertFrom-Json\n$backupRoot = $record.backup_dir\nif (-not (Test-Path $backupRoot)) {\n    Write-Host "Backup directory is missing: $backupRoot" -ForegroundColor Red\n    exit 1\n}\n\nforeach ($target in $record.targets) {\n    $dst = Join-Path $GameDir $target.path\n    if (Test-Path $dst) {\n        Remove-Item $dst -Recurse -Force\n    }\n\n    if ($target.existed) {\n        $backupSrc = Join-Path $backupRoot $target.path\n        if (-not (Test-Path $backupSrc)) {\n            Write-Host "Missing backup item: $backupSrc" -ForegroundColor Red\n            exit 1\n        }\n        Copy-Item -Path $backupSrc -Destination $dst -Recurse -Force\n    }\n}\n\nRemove-Item $recordPath -Force\nWrite-Host "RC1 removed and previous OptiScaler/DXGI files restored." -ForegroundColor Green\nWrite-Host "Backup retained at: $backupRoot"\n',
    'tools/Verify-WH3-DLSS5-XeFG.ps1': 'param(\n    [Parameter(Mandatory = $true)]\n    [string]$GameDir\n)\n\n$ErrorActionPreference = \'Stop\'\n$GameDir = (Resolve-Path $GameDir).Path\n\n$checks = @(\n    @{ Name = \'Warhammer3.exe\'; Path = (Join-Path $GameDir \'Warhammer3.exe\') },\n    @{ Name = \'dxgi.dll (RC1 proxy expected)\'; Path = (Join-Path $GameDir \'dxgi.dll\') },\n    @{ Name = \'OptiScaler.ini\'; Path = (Join-Path $GameDir \'OptiScaler.ini\') },\n    @{ Name = \'OptiScaler runtime dir\'; Path = (Join-Path $GameDir \'OptiScaler\') },\n    @{ Name = \'ReShade64.dll\'; Path = (Join-Path $GameDir \'ReShade64.dll\') }\n)\n\nforeach ($c in $checks) {\n    $ok = Test-Path $c.Path\n    $status = if ($ok) { \'OK\' } else { \'MISSING\' }\n    $color = if ($ok) { \'Green\' } else { \'Red\' }\n    Write-Host ("{0,-34} {1}" -f $c.Name, $status) -ForegroundColor $color\n}\n\n$shortFuse = Get-ChildItem -Path $GameDir -Filter \'dlss5-feed.addon64\' -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1\nif ($shortFuse) {\n    Write-Host ("{0,-34} {1}" -f \'dlss5-feed.addon64\', \'FOUND\') -ForegroundColor Green\n    Write-Host "  $($shortFuse.FullName)"\n} else {\n    Write-Host ("{0,-34} {1}" -f \'dlss5-feed.addon64\', \'MISSING\') -ForegroundColor Red\n}\n\n$ini = Join-Path $GameDir \'OptiScaler.ini\'\nif (Test-Path $ini) {\n    $text = Get-Content $ini -Raw\n    Write-Host ""\n    foreach ($needle in @(\'FGInput=upscaler\',\'FGOutput=xefg\',\'EnableDlssInputs=true\',\'LoadReshade=true\')) {\n        $ok = $text -match [regex]::Escape($needle)\n        $color = if ($ok) { \'Green\' } else { \'Yellow\' }\n        $state = if ($ok) { \'OK\' } else { \'CHECK\' }\n        Write-Host ("{0,-34} {1}" -f $needle, $state) -ForegroundColor $color\n    }\n}\n\nWrite-Host ""\nWrite-Host "Runtime truth still comes from the in-game four-state panel:" -ForegroundColor Cyan\nWrite-Host "  DLSS5 feeder:     DETECTED"\nWrite-Host "  Native DLSS NGX:  ACTIVE"\nWrite-Host "  Depth + MV:       READY"\nWrite-Host "  XeFG:             ACTIVE"\n',
}

GITHUB_RELEASE_NOTES = '# WH3 Native DLSS5 → XeFG RC1\n\nThis is the first public release candidate of the Warhammer III native-DLSS5-to-XeFG compatibility path.\n\n## What it does\n\nIt keeps ShortFuse/native NVIDIA DLSS5 as the real upscaler, captures the native D3D12 DLSS Depth + Motion Vector inputs through a private metadata-only bridge, and feeds them to OptiFG → Intel XeFG for frame generation.\n\n## Validated\n\n- Total War: WARHAMMER III DX11\n- NVIDIA GeForce RTX 4090\n- 2560×1440 full battle\n- 3840×2160 full battle\n- Native NVIDIA D3D12 DLSS passthrough\n- Depth + Motion Vector bridge\n- XeFG activation and sustained dispatch\n\nExpected in-game health panel:\n\n```text\nDLSS5 feeder:     DETECTED\nNative DLSS NGX:  ACTIVE\nDepth + MV:       READY\nXeFG:             ACTIVE\n```\n\n## Important prerequisites\n\nShortFuse / `dlss5-feed.addon64` and ReShade are **not redistributed**. Install and configure your own ShortFuse DLSS5 setup first.\n\nFor the validated RC1 path:\n- NVIDIA Smooth Motion: OFF\n- RTSS: OFF / excluded for WH3\n- Enable XeFG only after entering a real 3D battle on the first test\n\n## Known limitation\n\nAfter XeFG owns the final Present path, the ReShade UI may no longer be visible. This does not by itself mean DLSS5 is disabled. Use the four-state OptiScaler panel above.\n\n## Source\n\nThe release archive contains `source/WH3-native-DLSS5-XeFG.patch`, based on OptiScaler commit:\n\n`5c5e424dd137d69ef36c4231fd45b760b4c65cc8`\n\nThis is an unofficial community experiment and is not affiliated with Creative Assembly, SEGA, NVIDIA, Intel, ReShade, ShortFuse, or OptiScaler.\n'


def fail(msg: str):
    raise SystemExit(msg)


def patch_ini(src: Path, dst: Path):
    text = src.read_text(encoding="utf-8-sig")
    lines = text.splitlines()
    section = ""
    out = []
    seen = set()
    for line in lines:
        stripped = line.strip()
        if stripped.startswith("[") and stripped.endswith("]"):
            section = stripped[1:-1]
        key = None
        if "=" in line and not stripped.startswith(";"):
            key = line.split("=", 1)[0].strip()
        if section == "FrameGen" and key == "Enabled":
            line = "Enabled=false"; seen.add(("FrameGen", "Enabled"))
        elif section == "FrameGen" and key == "FGInput":
            line = "FGInput=upscaler"; seen.add(("FrameGen", "FGInput"))
        elif section == "FrameGen" and key == "FGOutput":
            line = "FGOutput=xefg"; seen.add(("FrameGen", "FGOutput"))
        elif section == "Inputs" and key == "EnableDlssInputs":
            line = "EnableDlssInputs=true"; seen.add(("Inputs", "EnableDlssInputs"))
        elif section == "Plugins" and key == "LoadReshade":
            line = "LoadReshade=true"; seen.add(("Plugins", "LoadReshade"))
        out.append(line)
    required = {
        ("FrameGen", "Enabled"), ("FrameGen", "FGInput"), ("FrameGen", "FGOutput"),
        ("Inputs", "EnableDlssInputs"), ("Plugins", "LoadReshade")
    }
    missing = required - seen
    if missing:
        fail(f"Could not patch required OptiScaler.ini keys: {sorted(missing)}")
    dst.write_text("\r\n".join(out) + "\r\n", encoding="utf-8")


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def copy_required(src: Path, dst: Path):
    if not src.exists():
        fail(f"Required artifact item missing: {src}")
    if src.is_dir():
        shutil.copytree(src, dst)
    else:
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dst)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--artifact-dir", required=True)
    ap.add_argument("--dist-dir", required=True)
    args = ap.parse_args()

    artifact = Path(args.artifact_dir).resolve()
    dist = Path(args.dist_dir).resolve()
    if dist.exists():
        shutil.rmtree(dist)
    dist.mkdir(parents=True)

    pkg = dist / ROOT_NAME
    for d in ["runtime", "config", "source", "docs/Licenses", "tools"]:
        (pkg / d).mkdir(parents=True, exist_ok=True)

    copy_required(artifact / "OptiScaler.dll", pkg / "runtime/OptiScaler.dll")
    copy_required(artifact / "OptiScaler", pkg / "runtime/OptiScaler")
    copy_required(artifact / "WH3-native-DLSS5-XeFG.patch", pkg / "source/WH3-native-DLSS5-XeFG.patch")
    copy_required(artifact / "Licenses", pkg / "docs/Licenses")
    copy_required(artifact / "OptiScaler.ini", pkg / "config/OptiScaler.upstream-default.ini")
    patch_ini(artifact / "OptiScaler.ini", pkg / "config/OptiScaler.ini")

    for rel, text in TEXT_FILES.items():
        p = pkg / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding="utf-8")

    build_info = {
        "name": "WH3 Native DLSS5 -> XeFG RC1",
        "implementation": "V15",
        "release_date": "2026-09-13",
        "base_optiscaler_commit": BASE_OPTISCALER,
        "github_workflow_run": V15_RUN,
        "github_artifact_id": V15_ARTIFACT_ID,
        "github_source_artifact_sha256": V15_ARTIFACT_SHA256,
        "validated": {
            "game": "Total War: WARHAMMER III 8.1.0",
            "api": "DX11 with DX11->DX12 interop presenter",
            "gpu": "NVIDIA GeForce RTX 4090",
            "resolutions": ["2560x1440", "3840x2160"],
            "smooth_motion": "off",
            "rtss": "off"
        }
    }
    (pkg / "BUILD_INFO.json").write_text(json.dumps(build_info, indent=2) + "\n", encoding="utf-8")

    rows = []
    for p in sorted(pkg.rglob("*")):
        if p.is_file() and p.name != "CHECKSUMS.sha256":
            rows.append(f"{sha256_file(p)}  {p.relative_to(pkg).as_posix()}")
    (pkg / "CHECKSUMS.sha256").write_text("\n".join(rows) + "\n", encoding="utf-8")

    zip_path = dist / f"{ROOT_NAME}.zip"
    fixed_date = (2026, 9, 13, 0, 0, 0)
    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for p in sorted(pkg.rglob("*")):
            if not p.is_file():
                continue
            arc = f"{ROOT_NAME}/{p.relative_to(pkg).as_posix()}"
            zi = zipfile.ZipInfo(arc, fixed_date)
            zi.compress_type = zipfile.ZIP_DEFLATED
            zi.external_attr = 0o644 << 16
            zf.writestr(zi, p.read_bytes())

    zip_sha = sha256_file(zip_path)
    (dist / f"{zip_path.name}.sha256").write_text(f"{zip_sha}  {zip_path.name}\n", encoding="utf-8")
    (dist / "GITHUB_RELEASE_NOTES.md").write_text(GITHUB_RELEASE_NOTES, encoding="utf-8")
    shutil.copy2(pkg / "source/WH3-native-DLSS5-XeFG.patch", dist / "WH3-native-DLSS5-XeFG.patch")

    print(zip_path)
    print(zip_sha)


if __name__ == "__main__":
    main()
