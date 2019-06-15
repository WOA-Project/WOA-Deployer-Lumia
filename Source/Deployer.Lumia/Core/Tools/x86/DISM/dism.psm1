#
# Script Module file for Dism module.
#
# Copyright (c) Microsoft Corporation
#

#
# Cmdlet aliases
#

Set-Alias Apply-WindowsUnattend Use-WindowsUnattend
Set-Alias Add-ProvisionedAppxPackage Add-AppxProvisionedPackage
Set-Alias Remove-ProvisionedAppxPackage Remove-AppxProvisionedPackage
Set-Alias Get-ProvisionedAppxPackage Get-AppxProvisionedPackage

Export-ModuleMember -Alias * -Function * -Cmdlet *
