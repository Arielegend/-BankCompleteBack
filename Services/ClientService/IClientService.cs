using back.Models;
using back.Models.SrevicesContracts;

namespace back.Services.ClientService
{
    public interface IClientService
    {
        public Task InitialClients(int numberOfClients);
        public Task <List<Client>> GetClients();
        public Task<bool> CanClientSend(int clientId);
        public Task<int> HandleDeposit(DepositRequest depositRequest);
    }
}