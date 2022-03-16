using back.Models;
using back.Models.SrevicesContracts;
using back.Services.ClientService;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace back.Controllers
{
    [EnableCors()]
    [ApiController]
    [Route("[controller]")]
    public class BankController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly ILogger<BankController> _logger;
        private static object Lock = new object();

        public BankController(IClientService clientService, ILogger<BankController> logger)
        {
            _clientService = clientService;
            _logger = logger;
        }

        [HttpGet("IsAllive")]
        public IsAlliveResponse IsAllive()
        {
            var response = new IsAlliveResponse() {ReturnStatus = ReturnStatuses.Allive};
            return response;
        }


        [HttpGet("GetAllClients")]
        public async Task<List<Client>> GetClients()
        {
            _logger.LogInformation("BankController -- GetAllClients -- Request starts");
            var clients = await _clientService.GetClients();
            _logger.LogInformation(
                $"BankController -- GetAllClients -- Request ends, there are {clients.Count} clients in DB");
            return clients;
        }


        [HttpPost("SetNumberOfClients")]
        public async Task<SetNumberOfClientsResponse> SetNumberOfClients([FromBody] SetNumberOfClientsRequest request)
        {
            var response = new SetNumberOfClientsResponse();
            var numOfAccounts = request.NumberOfClients;

            try
            {
                _logger.LogInformation(
                    $"BankController -- SetNumberOfClients -- Request of {request.NumberOfClients} clients");

                await _clientService.InitialClients(numOfAccounts);

                _logger.LogInformation(
                    $"BankController -- SetNumberOfClients -- Done initializing {request.NumberOfClients} clients");

                response.ReturnStatus = ReturnStatuses.Success;
                response.TotalCount = numOfAccounts;

            }
            catch (Exception ex)
            {
                _logger.LogError($"BankController -- SetNumberOfClients -- Failed to set client's table. {ex.Message}");
                response.ReturnStatus = ReturnStatuses.Error;
                response.TotalCount = -1;
            }
            return response;
        }


        [HttpPost("Deposit")]
        public DepositResponse Deposit([FromBody] DepositRequest request)
        {
            var response = new DepositResponse();
            lock (Lock)
            {
                var canClientSend =  _clientService.CanClientSend(request.RequestingClientThread);
                if (canClientSend.Result)
                {
                    // Client can send ->
                    //              Handling deposit
                    //              Returning response with amount to be added
                    var amount =  _clientService.HandleDeposit(request);
                    response.Amount = amount.Result;

                    if (amount.Result == -1)
                    {
                        // Amount will be -1 in case we FAILED to update Sending || Receiving client.
                        response.ReturnStatus = ReturnStatuses.Error;
                        _logger.LogError($"BankController -- Deposit -- {request.RequestingClientThread} " +
                                         $"deposit {request.Amount} to client number {request.ClientId} -- FAILED");
                    }
                    else
                    {
                        // In case amount is no -1, than updating Sending && Receiving client done successfully. 
                        response.ReturnStatus = ReturnStatuses.Success;
                        _logger.LogInformation($"BankController -- Deposit -- {request.RequestingClientThread} " +
                                               $"deposit {request.Amount} to client number {request.ClientId}");
                    }
                }
                else
                {
                    // Client can NOT send ->
                    //              returning ErrorTooManyRequests
                    response.ReturnStatus = ReturnStatuses.ErrorTooManyRequests;
                    _logger.LogError($"BankController -- Deposit -- Too many request by {request.RequestingClientThread}");
                }
            }
            return response;
        }
    }
}
