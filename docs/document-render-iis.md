# Document downloads on IIS

PNG and PDF exports launch Playwright on the server. Installing Chrome on the developer workstation does not install Playwright's browser for the IIS application pool. In production, the renderer requires the Chromium revision matching the published Microsoft.Playwright package; Chrome/Edge fallback is available only in Development.

## Deploy

1. Publish the Windows x64 application and copy the complete output to the IIS site's physical directory, including `.playwright`, `Microsoft.Playwright.dll`, `playwright.ps1`, and `Deployment`.
2. In IIS Manager, find the site's application pool name. On the server, open PowerShell as Administrator and run the setup script from the deployed directory:

   ```powershell
   Set-Location -LiteralPath 'D:\Sites\VazinCrm'
   .\Deployment\Install-DocumentRenderer.ps1 -AppPoolName 'YourAppPool'
   ```

   Replace both example values with the deployed directory and actual pool name. Use `-WhatIf` to preview changes. The script supports Windows PowerShell 5.1 and uses the Node CLI bundled with the publish, so it does not require PowerShell 7 or a separate Node installation.

3. The script installs Chromium headless shell under `%ProgramData%\VazinCrm\Playwright\<pool>`, grants that pool ReadAndExecute access, and sets `PLAYWRIGHT_BROWSERS_PATH` in the existing `aspNetCore/environmentVariables` element in `web.config`. Existing hosting attributes and other environment variables are preserved. The server needs access to Playwright's download servers during setup. Browsers are not downloaded while serving requests.
4. Recycle the application's pool in IIS Manager, sign in to the CRM, and test both image and PDF downloads. This is the required check under the actual application pool identity; successful installation as Administrator alone does not verify IIS execution.
5. Run setup again after replacing `web.config` during deployment or upgrading Microsoft.Playwright. Keep the same browser path configured for installation and runtime.

## Diagnose

The application writes renderer exceptions to `Logs/log-*.txt`. Search for `Failed to render pre-factor images` or `Failed to render pre-factor PDF` and read the exception immediately below it.

- `Executable doesn't exist`: the matching browser is missing from the configured runtime path. Run setup from the deployed version.
- `TargetClosedException` while launching Chrome/Edge, with `Failed to encrypt` or user-profile errors: the branded browser failed under the pool account. Deploy the current renderer and install its headless shell using setup.
- Access denied: confirm the actual pool account can read and execute the published `.playwright` files and shared browser directory, write its temporary files, and write the application's `Logs` directory. The script grants only access to its browser directory.
- Navigation, certificate, DNS, or timeout errors after browser launch: check that the server can reach the application's preview URL and trusts its HTTPS certificate. These errors require separate investigation; installing Chromium alone does not resolve them.

The script does not change the pool identity or TLS validation. Other errors in the same log, such as SMS synchronization failures or SQL schema errors, require their own fixes.

## References

- [Playwright browser installation, headless shell, and shared browser paths](https://playwright.dev/dotnet/docs/browsers)
- [IIS application pool identities and resource access](https://learn.microsoft.com/en-us/iis/manage/configuring-security/application-pool-identities)
- [ASP.NET Core Module environment variables](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/aspnet-core-module?view=aspnetcore-10.0#set-environment-variables)
