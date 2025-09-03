using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NewVivaApi.Data;
using NewVivaApi.Models;
using System.Security.Claims;
using System;

namespace NewVivaApi.Services
{
    public class PayAppPaymentService
    {
        private readonly AppDbContext _context = new AppDbContext();
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PayAppPaymentService> _logger;
        private readonly int _payAppId;
        private readonly int _subContractorId;
        private readonly PayApp _payApp;
        private readonly Subcontractor _subcontractor;
        private PayAppPayment _subContractorPayAppPayment;

        public PayAppPaymentService(int payAppId, IHttpContextAccessor httpContextAccessor)
        {
            _logger = httpContextAccessor.HttpContext.RequestServices.GetService<ILogger<PayAppPaymentService>>();
            _httpContextAccessor = httpContextAccessor;
            _payAppId = payAppId;

            _payApp = _context.PayApps
                .Include(pa => pa.SubcontractorProject)
                .ThenInclude(sp => sp.Subcontractor)
                .FirstOrDefault(pa => pa.PayAppId == _payAppId);

            if (_payApp == null)
            {
                Console.WriteLine($"PayApp with ID {_payAppId} not found.");
                throw new Exception($"Exception: There is no defined PayApp for ID {_payAppId}");
            }

            _subContractorId = _payApp.SubcontractorProject?.SubcontractorId ?? 0;
            _subcontractor = _context.Subcontractors.FirstOrDefault(sc => sc.SubcontractorId == _subContractorId);
            
            if (_subcontractor == null)
            {
                Console.WriteLine($"Subcontractor with ID {_subContractorId} not found.");
                throw new Exception($"Exception: There is no defined Subcontractor for ID {_subContractorId}");
            }

            UpdateExistingOrCreateDefaultSubContractorPayAppPayment();
        }

        private void UpdateExistingOrCreateDefaultSubContractorPayAppPayment()
        {
            var defaultSubContractorPayment = _context.PayAppPayments
                .FirstOrDefault(pap => pap.PayAppId == _payAppId &&
                                     pap.DeleteDt == null &&
                                     pap.SubcontractorId != null);

            if (defaultSubContractorPayment != null)
            {
                defaultSubContractorPayment.PaymentTypeId = 1; // TODO: Extract from JsonAttributes
                //defaultSubContractorPayment.LastUpdateUser = GetCurrentUserName();
                defaultSubContractorPayment.LastUpdateDt = DateTimeOffset.UtcNow;
                _context.SaveChanges();

                _subContractorPayAppPayment = defaultSubContractorPayment;
                return;
            }

            Console.WriteLine($"No existing PayAppPayment found for PayApp ID {_payAppId}, creating a new one.");
            // Create new PayAppPayment
            defaultSubContractorPayment = new PayAppPayment
            {
                PayAppId = _payAppId,
                PaymentTypeId = 1, // TODO: Extract from JsonAttributes
                SubcontractorId = _payApp.SubcontractorProject?.SubcontractorId,
                DollarAmount = _payApp.ApprovedAmount ?? 0,
                JsonAttributes = GetSubContractorPayAppPaymentJsonAttributes(),
                CreateDt = DateTimeOffset.UtcNow,
                LastUpdateUser = GetCurrentUserName(),
                LastUpdateDt = DateTimeOffset.UtcNow
            };
            _context.PayAppPayments.Add(defaultSubContractorPayment);
            _context.SaveChanges();
            _subContractorPayAppPayment = defaultSubContractorPayment;
        }

        private string GetSubContractorPayAppPaymentJsonAttributes()
        {
            try
            {
                dynamic jsonAttributes = new JObject();
                
                // Parse PayApp JsonAttributes
                dynamic payAppJsonAttributes = string.IsNullOrEmpty(_payApp.JsonAttributes) 
                    ? new JObject() 
                    : JObject.Parse(_payApp.JsonAttributes);
                
                // Parse Subcontractor JsonAttributes
                dynamic subContractorJsonAttributes = string.IsNullOrEmpty(_subcontractor.JsonAttributes) 
                    ? new JObject() 
                    : JObject.Parse(_subcontractor.JsonAttributes);

                // Combine attributes
                jsonAttributes.PaymentAddress = payAppJsonAttributes.PaymentAddress;
                jsonAttributes.OrderOf = payAppJsonAttributes.PaymentOrderOf;
                jsonAttributes.PaidDate = payAppJsonAttributes.PaymentPaidDate;
                jsonAttributes.WireNumber = payAppJsonAttributes.PaymentWireNumber;
                jsonAttributes.WireNotes = payAppJsonAttributes.PaymentWireNotes;
                jsonAttributes.BankName = subContractorJsonAttributes.BankName;
                jsonAttributes.AccountNumber = subContractorJsonAttributes.AccountNumber;
                jsonAttributes.RoutingNumber = subContractorJsonAttributes.RoutingNumber;

                return jsonAttributes.ToString(Formatting.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayAppPayment JsonAttributes for PayApp {PayAppId}", _payAppId);
                return "{}"; // Return empty JSON object on error
            }
        }

        public async Task ReconcileTotalDollarAmount()
        {
            try
            {
                decimal totalPADollarAmount = GetTotalDollarAmountBasedOnPayAppStatus();

                var jointChecks = await _context.PayAppPayments
                    .Where(pap => pap.PayAppId == _payAppId && 
                                 pap.SubcontractorId == null && 
                                 pap.DeleteDt == null)
                    .ToListAsync();
                foreach (var jointCheck in jointChecks)
                {
                    totalPADollarAmount -= jointCheck.DollarAmount;
                }
                _subContractorPayAppPayment.DollarAmount = totalPADollarAmount;
                _subContractorPayAppPayment.LastUpdateUser = GetCurrentUserName();
                _subContractorPayAppPayment.LastUpdateDt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully reconciled PayApp {PayAppId} total amount to {Amount}", 
                    _payAppId, totalPADollarAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling total dollar amount for PayApp {PayAppId}", _payAppId);
                throw;
            }
        }

        private decimal GetTotalDollarAmountBasedOnPayAppStatus()
        {
            // Status 2 = Approved (based on original comment)
            if (_payApp.StatusId == 2)
            {
                return _payApp.ApprovedAmount ?? 0;
            }
            else
            {
                return _payApp.RequestedAmount;
            }
        }

        private string GetCurrentUserName()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.Identity.Name ?? "Unknown";
            }
            return "System";
        }
    }
}