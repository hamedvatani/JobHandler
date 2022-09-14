using System.Threading.Tasks;

namespace JobHandler.Sender
{
    public interface ISender
    {
        void Send<T>(T job);
        Task SendAsync<T>(T job);
    }
}