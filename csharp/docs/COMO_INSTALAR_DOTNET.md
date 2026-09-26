<!-- Passo a passo para instalar o .NET SDK -->
# Como instalar o .NET SDK

## Linux — Debian / Kali / Ubuntu (via repositório da Microsoft)
```bash
# adiciona o repositório da Microsoft
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O /tmp/ms.deb
sudo dpkg -i /tmp/ms.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0
```
> Também funciona com o SDK 6/7. Alternativa oficial: o script `dotnet-install.sh`
> (https://dotnet.microsoft.com/download).

## Windows
Baixe em https://dotnet.microsoft.com/download (o instalador do **.NET SDK**) ou:
```
winget install Microsoft.DotNet.SDK.8
```

## Verificar
```
dotnet --version
```

## Rodar o projeto
A partir da raiz `csharp/` (mesmo comando nos dois sistemas):
```
dotnet run --project back-end
```
