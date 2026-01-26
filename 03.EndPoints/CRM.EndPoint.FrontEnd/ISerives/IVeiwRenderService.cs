using System.Collections.Generic;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.ISerives
{
    public interface IVeiwRenderService
    {
        Task<string> RenderToStringAsync(string ViewName, object model,Dictionary<string,object> ViewData=null );
    }
}
