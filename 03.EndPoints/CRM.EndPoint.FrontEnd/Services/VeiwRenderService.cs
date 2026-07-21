using CRM.EndPoint.FrontEnd.ISerives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Services
{
    public class VeiwRenderService : IVeiwRenderService
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VeiwRenderService(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> RenderToStringAsync(string viewName, object model, Dictionary<string, object> ViewData = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

                using (var sw = new StringWriter())
                {
                    var viewResult = _viewEngine.FindView(actionContext, viewName, false);

                    if (!viewResult.Success)
                    {
                        throw new ArgumentNullException($"View {viewName} not found.");
                    }

                    var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) {
                        Model = model

                    };
                    if (ViewData != null)
                    {
                        foreach (var item in ViewData)
                        {
                            viewDictionary[item.Key] = item.Value;
                        }
                    }

                    var tempData = new TempDataDictionary(httpContext, _tempDataProvider);

                    var viewContext = new ViewContext(
                        actionContext,
                        viewResult.View,
                        viewDictionary,
                        tempData,
                        sw,
                        new HtmlHelperOptions()
                    );

                    await viewResult.View.RenderAsync(viewContext);
                    return sw.ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
