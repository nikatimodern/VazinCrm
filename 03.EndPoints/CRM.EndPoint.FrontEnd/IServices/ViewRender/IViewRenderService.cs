using System.Collections.Generic;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.IServices.ViewRender
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string ViewName, object model,Dictionary<string,object> ViewData=null );
    }
}
