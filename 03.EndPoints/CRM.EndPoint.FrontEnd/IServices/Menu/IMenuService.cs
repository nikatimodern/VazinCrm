using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Menu;

namespace CRM.EndPoint.FrontEnd.IServices.Menu;

public interface IMenuService {
    Task<List<MenuItem>> GetMenusAsync(ClaimsPrincipal user);
}