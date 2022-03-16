using back.Models;
using back.Models.SrevicesContracts;
using Microsoft.EntityFrameworkCore;

namespace back.Services.ClientService
{
    public class ClientService : IClientService
    {
        private const int ALLOWED_MESSAGES = 5;
        private const int TIME_FRAME = 5;

        private readonly DataContext _dataContext;
        private readonly ILogger<ClientService> _logger;

        public ClientService(DataContext dataContext, ILogger<ClientService> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }

        /*
            IClientService Contract Public Methods -- Starts
        */
        //InitialClients ->
        //          - Make sure Client table is EMPTY.
        //          - Initializing new clients (as many as method's props - numOfClients)
        //                                      with empty records, and Amount of 0 to each of them. 
        public async Task InitialClients(int numOfClients)
        {
            if (!CheckIfClientTableIsEmpty())
            {
                await EmptyClientsTable();
            }

            var arrayIds = Enumerable.Range(1, numOfClients).ToArray();
            foreach (var i in arrayIds)
            {
                var helper = new Client()
                {
                    ClientId = i,
                    Amount = 0,
                    ClientSendingRecords = String.Empty
                };
                await AddSingleClient(helper);
            }
        }

        //CanClientSend ->
        //          - Fetching clients records.
        //          - Cleaning all old records. (records that are expired and no longer relevant)
        //              A record will be considered "expired" if its DateTime is expired...
        //          - If there is a room for another records (i.e number of records < ALLOWED_MESSAGES)
        //              - returning True, and Controller will proceed to HandleDeposit
        //              - Otherwise, returning False, and Controller will NOT proceed to HandleDeposit
        public async Task<bool> CanClientSend(int clientId)
        {
            try
            {
                var client = _dataContext.Clients.Find(clientId);
                var updatedRecordsListString = await CleanExpiredRecords(client);
                return Utils.Utils.GetListOfDatesFromString(updatedRecordsListString).Count < ALLOWED_MESSAGES;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ClientService -- CanClientSend -- {ex.Message}");
                return false;
            }

        }

        //HandleDeposit -> 
        //          - Need to update sending client's records.
        //          - Need to update receiving client's amount.
        public async Task<int> HandleDeposit(DepositRequest depositRequest)
        {
            try
            {
                var newAmount = await UpdateClientReceiving(depositRequest);
                await UpdateClientSending(depositRequest);

                return newAmount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ClientService -- HandleDeposit -- {ex.Message}");
                return -1;
            }
        }
        public async Task<List<Client>> GetClients()
        {
            try { 
                return await _dataContext.Clients.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ClientService -- CanClientSend -- {ex.Message}");
                throw;
            }
        }
        /*
            IClientService Contract Public Methods -- Ends
        */



        /*
            ClientService Class helper inner Methods -- Starts
        */
        //EmptyClientsTable-> 
        //                  Whenever we submit new number to simulate as bank accounts, 
        //                  We EMPTY Clients table.
        private async Task EmptyClientsTable()
        {
            var clients = await GetClients();
            _logger.LogInformation("ClientService -- EmptyClientsTable -- Requests starts. " +
                $"There are {clients.Count} to remove.");

            foreach (var client in clients)
            {
                await RemoveClient(client);
            }
            _logger.LogInformation($"ClientService -- EmptyClientsTable -- Done removing " +
                $"{clients.Count} clients. Clients table is now empty");
        }
        private bool CheckIfClientTableIsEmpty()
        {
            return !_dataContext.Clients.Any();
        }
        private async Task<bool> RemoveClient(Client c)
        {
            _dataContext.Clients.Remove(c);
            await _dataContext.SaveChangesAsync();
            return true;
        }

        // UpdateClientSending ->
        //                  - Upon successful Deposit request, (meaning sending client thread IS actually
        //                    allowed currently to send a deposit request)
        //                    need to update Sending client Records,
        //                    With a NEW record with expiry at -> 
        //                         DateTime.Now.AddSeconds(TIME_FRAME)
        //
        //                  - Records are simply a DateTime string, separated by ';'
        //                    (for example - "DateTime1;DateTime2;DateTime3"...)
        private async Task UpdateClientSending(DepositRequest depositRequest)
        {
            var clientSending = _dataContext.Clients.Find(depositRequest.RequestingClientThread);
            if (clientSending != null)
            {
                var newRecordsListString = clientSending.ClientSendingRecords;
                var newEntryRecord = DateTime.Now.AddSeconds(TIME_FRAME).ToString();

                if (newRecordsListString.Length == 0)
                    newRecordsListString = newEntryRecord;
                else
                    newRecordsListString += ";" + newEntryRecord;

                clientSending.ClientSendingRecords = newRecordsListString;
                await _dataContext.SaveChangesAsync();
            }
            else
            {
                _logger.LogError($"ClientService -- UpdateClientSending -- Couldn't find client");

            }
        }

        // UpdateClientReceiving ->
        //                  Upon successful Deposit request, need to update Receiving client Amount
        private async Task<int> UpdateClientReceiving(DepositRequest depositRequest)
        {
            var clientReceiving = _dataContext.Clients.Find(depositRequest.ClientId);
            if (clientReceiving != null)
            {
                var newAmount = clientReceiving.Amount + depositRequest.Amount;
                clientReceiving.Amount = newAmount;

                await _dataContext.SaveChangesAsync();
                return newAmount;
            }

            _logger.LogError($"ClientService -- UpdateClientReceiving -- Couldn't find client");
            return -1;

        }

        //CleanExpiredRecords ->
        //                  Iterates through all client's records, and removes all expired records.
        //                  An expired record is a record that satisfy ->
        //                                                  record < DateTime.Now
        private async Task<string> CleanExpiredRecords(Client client)
        {
            var newRecords = new List<DateTime>();
            var currentRecords = Utils.Utils.GetListOfDatesFromString(client.ClientSendingRecords);

            foreach (var record in currentRecords)
            {
                if (record > DateTime.Now)
                {
                    newRecords.Add(record);
                }
            }

            var stringFromListOfDates = Utils.Utils.GetStringFromListOfDates(newRecords);
            client.ClientSendingRecords = stringFromListOfDates;
            await _dataContext.SaveChangesAsync();

            return stringFromListOfDates;
        }
        private async Task AddSingleClient(Client c)
        {
            try
            {
                _dataContext.AddAsync(c);
                await _dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ClientService -- AddSingleClient -- Failed to initialize" +
                                 $"client {c.ClientId} -- {ex.Message}");
            }
        }
        /*
            ClientService Class helper inner Methods -- Ends
        */
    }
}
