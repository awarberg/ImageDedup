# Introduction
This is an image de-duplication tool. It uses the [ImageHash](https://github.com/coenm/ImageHash) library
to find identical or similar images.

# Prerequisites
.NET 6 or later is required

# Usage
To launch the tool, run the following command in a PowerShell:  
`dotnet run --project ImageDedup.UI`

# Known issues
1. Delete using recycle bin-option only works for local drives. Files on network shares will be permanently deleted.