using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Playwright;

internal static class BrowserChecks
{
    public static async Task RunAsync()
    {
        var root = Directory.GetCurrentDirectory();
        var www = Path.GetFullPath(Path.Combine(root,"03.EndPoints/CRM.EndPoint.FrontEnd/wwwroot"));
        var views = Path.Combine(root,"03.EndPoints/CRM.EndPoint.FrontEnd/Views");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless=true, Channel="chrome", Timeout=15000 });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width=1440,Height=900 } });
        var page = await context.NewPageAsync();
        var errors = new List<string>(); page.PageError += (_,message) => { errors.Add(message); Console.WriteLine("BROWSER ERROR " + message); };
        page.Console += (_,message) => { if(message.Type=="error") Console.WriteLine("BROWSER CONSOLE " + message.Text); };
        var unseenCalls=0; var ackCalls=0; var ackSuccess=false; var ackFail=true; var session="first"; var admin=false;
        var id=Guid.NewGuid();
        var note = new { id,version="7.0.5",title="تازه‌های سامانه <img src=x onerror=alert(1)>", description="توضیح طولانی برای بررسی خوانایی و چیدمان در صفحه کوچک",isImportant=true,publishDate=DateTime.UtcNow, status=0,audience=0,showPopup=true,rowVersion="AAAAAAAAAAA=",roles=Array.Empty<string>(), items=new[] { new { id=Guid.NewGuid(),title="قابلیت جدید",description=string.Join(" ",Enumerable.Repeat("توضیحات تغییرات",70)),type=1,sortOrder=0,imageUrl="javascript:alert(1)",learnMoreUrl="https://example.com/help" } } };
        string Clean(string html)
        {
            html=Regex.Replace(html,@"^@(?:model|using).*$","",RegexOptions.Multiline);
            html=html.Replace("@Html.AntiForgeryToken()","<input type='hidden' name='__RequestVerificationToken' value='test-token'>");
            html=html.Replace("@Context.Session.GetString(\"ReleaseNotesSession\")",session).Replace("@User.GetUserId()","test-user");
            html=Regex.Replace(html,@"@foreach \(var role in Model.Roles\) \{.*?\}","<option value='مدیر ارشد'>مدیر ارشد</option>",RegexOptions.Singleline);
            html=Regex.Replace(html,@"@section Scripts.*$","",RegexOptions.Singleline);
            return html;
        }
        string Html()
        {
            var libs=new[]{"lib/Bootstrap/bootstrap5.3.bundle.min.js","lib/axios/axios.min.js","lib/toastify/toastify-js.js","lib/SweetAlert/sweetalert2.all.min.js","lib/TomSelect/tom-select.complete.min.js","js/site.js"};
            var body=admin?Clean(File.ReadAllText(Path.Combine(views,"ReleaseNotesAdmin/Index.cshtml"))):"<h1>فضای کاری آزمایشی</h1>";
            return "<!doctype html><html lang='fa' dir='rtl'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>"+
                "<link rel='stylesheet' href='/lib/Bootstrap/bootstrap5.3.rtl.min.css'><link rel='stylesheet' href='/lib/TomSelect/tom-select.min.css'><link rel='stylesheet' href='/css/site.css'><link rel='stylesheet' href='/css/releaseNotes.css'></head><body>"+
                "<main id='mainContent'>"+body+"</main>"+Clean(File.ReadAllText(Path.Combine(views,"Shared/Modals/_ReleaseNotesModal.cshtml")))+
                string.Join("",libs.Select(src=>"<script src='/"+src+"'></script>"))+
                "<script src='/js/releaseNotes.js'></script><script src='/js/pageRuntime.js'></script></body></html>";
        }
        await page.RouteAsync("https://release-notes.test/**",async route=> {
            var path=new Uri(route.Request.Url).AbsolutePath;
            if (path=="/ReleaseNotes/Unseen") { unseenCalls++; await route.FulfillAsync(new(){ContentType="application/json",Body=JsonSerializer.Serialize(ackSuccess?Array.Empty<object>():new object[]{note})}); return; }
            if (path=="/ReleaseNotes/Acknowledge") {
                ackCalls++;
                var sent=JsonDocument.Parse(route.Request.PostData!);
                if (sent.RootElement.GetProperty("ids")[0].GetGuid()!=id) throw new Exception("Wrong acknowledgement ID");
                ackSuccess=!ackFail;
                await route.FulfillAsync(new(){Status=ackFail?500:200,ContentType="application/json",Body=ackFail?"{}":"{\"status\":200}"}); return;
            }
            if (path=="/ReleaseNotesAdmin/List") { await route.FulfillAsync(new(){ContentType="application/json",Body=JsonSerializer.Serialize(new[]{note})});return; }
            if (path=="/ReleaseNotesAdmin/Create") { await route.FulfillAsync(new(){ContentType="application/json",Body="{\"status\":200}"});return; }
            if (path=="/test") { await route.FulfillAsync(new(){ContentType="text/html",Body=Html()});return; }
            var file=Path.GetFullPath(Path.Combine(www,path.TrimStart('/')));
            if (!file.StartsWith(www+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || !File.Exists(file)) { await route.AbortAsync();return; }
            var type=Path.GetExtension(file) switch { ".js"=>"application/javascript", ".css"=>"text/css", ".woff2"=>"font/woff2", _=>"application/octet-stream" };
            await route.FulfillAsync(new(){ContentType=type,Path=file});
        });
        void Check(bool condition,string name) { if(!condition) throw new Exception(name); Console.WriteLine("PASS browser: "+name); }
        await page.GotoAsync("https://release-notes.test/test");
        try { await page.Locator("#releaseNotesModal.show").WaitForAsync(); } catch { Console.WriteLine(await page.EvaluateAsync<string>("() => JSON.stringify({ release:typeof ReleaseNotes, visibility:document.visibilityState, modal:document.querySelector('#releaseNotesModal')?.outerHTML.slice(0,300), busy:typeof activeApiLoadingCount })")); throw; }
        Check(unseenCalls==1 && ackCalls==0,"Opening never acknowledges");
        Check(await page.Locator("[data-rn-content] img").CountAsync()==0,"Unsafe image URL omitted");
        Check((await page.Locator("[data-rn-content]").InnerTextAsync()).Contains("<img src=x"),"HTML title remains plain text");
        foreach (var width in new[]{390,768,1440}) {
            await page.SetViewportSizeAsync(width,900);
            var box=await page.Locator("#releaseNotesModal .modal-content").BoundingBoxAsync();
            Check(box!=null && box.X>=0 && box.X+box.Width<=width && box.Y+box.Height<=900,"Modal fits width "+width);
        }
        await page.Locator("#releaseNotesModal .rn-close").ClickAsync();
        await page.Locator("#releaseNotesModal").WaitForAsync(new(){State=WaitForSelectorState.Hidden});
        await page.ReloadAsync(); await page.WaitForTimeoutAsync(3500);
        Check(unseenCalls==1 && ackCalls==0,"Dismissed popup suppressed in same session");
        session="second"; await page.ReloadAsync(); await page.Locator("#releaseNotesModal.show").WaitForAsync();
        Check(unseenCalls==2,"New login checks unacknowledged releases");
        await page.Locator("[data-rn-ack]").ClickAsync();
        await page.Locator("[data-rn-error]:visible").WaitForAsync();
        Check(ackCalls==1 && await page.Locator("#releaseNotesModal.show").CountAsync()==1,"Failed acknowledgement remains open for retry");
        ackFail=false; await page.Locator("[data-rn-ack]").ClickAsync();
        await page.Locator("#releaseNotesModal").WaitForAsync(new(){State=WaitForSelectorState.Hidden});
        Check(ackCalls==2 && ackSuccess,"Successful acknowledgement closes modal");
        admin=true; await page.ReloadAsync();
        await page.EvaluateAsync("async () => { const script=document.createElement('script'); script.src='/js/releaseNotesAdmin.js'; const modules=await PageRuntime.prepare([script],location.href); await PageRuntime.mount(document.querySelector('main'),modules); }");
        await page.Locator("[data-list] .rn-list-row").WaitForAsync();
        await page.Locator("[data-new]").ClickAsync();
        await page.Locator("#releaseNoteEditor.show").WaitForAsync();
        await page.Locator("[name=version]").FillAsync("7.0.6");
        await page.Locator("[name=title]").FillAsync("پیش‌نمایش آزمایشی");
        await page.Locator("[name=itemTitle]").FillAsync("تغییر آزمایشی");
        await page.Locator("[data-preview]").ClickAsync();
        await page.Locator("#releaseNotesModal.show").WaitForAsync();
        Check(await page.Locator("[data-rn-ack]:visible").CountAsync()==0 && ackCalls==2,"Admin preview has no acknowledgement");
        await page.Locator("#releaseNotesModal .rn-close").ClickAsync();
        await page.Locator("#releaseNoteEditor.show").WaitForAsync();
        Check(await page.Locator("[name=title]").InputValueAsync()=="پیش‌نمایش آزمایشی","Editor restored after preview");
        await page.Locator("[data-editor] [type=submit]").ClickAsync();
        await page.Locator("#releaseNoteEditor").WaitForAsync(new(){State=WaitForSelectorState.Hidden});
        await page.EvaluateAsync("() => PageRuntime.unmount()");
        session="blocked"; ackSuccess=false; 
        var before=unseenCalls; await page.ReloadAsync();
        await page.EvaluateAsync("() => { const el=document.createElement('div'); el.id='blocking'; el.className='modal'; el.innerHTML='<div class=modal-dialog><div class=modal-content><p>فرم فعال</p></div></div>'; document.body.append(el); bootstrap.Modal.getOrCreateInstance(el).show(); }");
        await page.WaitForTimeoutAsync(3500);
        Check(unseenCalls==before,"Existing modal defers popup request");
        await page.EvaluateAsync("() => bootstrap.Modal.getInstance(document.getElementById('blocking')).hide()");
        await page.Locator("#releaseNotesModal.show").WaitForAsync();
        Check(unseenCalls==before+1,"Deferred popup resumes once");
        Check(errors.Count==0,"No JavaScript errors: "+string.Join("; ",errors));
    }
}
