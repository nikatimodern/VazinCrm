using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Services.SignalR
{
    public class ChatHub:Hub
    {
        public async Task SendMessage(string user,string Message,string id)
        {
            try
            {
                await Clients.All.SendAsync("ReceiveMessage", user, Message, id);
                await Clients.All.SendAsync("mo", user, Message, id);
            }
            catch
            {

            }



        }


    }
}
