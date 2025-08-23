•	Install the required MAUI workloads for the active SDK:
•	From an elevated Developer Command Prompt:
•	dotnet --info  (to confirm SDK in use: 9.0.3xx per your log)
•	dotnet workload restore
•	dotnet workload install maui-android maui-windows maui-ios maui-maccatalyst