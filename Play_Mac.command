#!/bin/bash
cd "$(dirname "$0")"
chmod +x ./LastLight.Client.Desktop
xattr -d com.apple.quarantine ./LastLight.Client.Desktop 2>/dev/null
./LastLight.Client.Desktop
