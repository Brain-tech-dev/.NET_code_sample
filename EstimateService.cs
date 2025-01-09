using ShutterManagement.Business.Interfaces;
using ShutterManagement.Core.SPModel.SP_Responses;
using ShutterManagement.Core.SPModel;
using ShutterManagement.Core.ViewModels.Request;
using ShutterManagement.Core.ViewModels.Response;
using ShutterManagement.Core.ViewModels;
using ShutterManagement.Core;
using ShutterManagement.DAL.Context;
using ShutterManagement.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShutterManagement.Core.Common;
using static Azure.Core.HttpHeader;
using Azure;
using System.Data.Entity;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace ShutterManagement.Business.Services
{
    public class EstimateService : BaseRepository<BtprojecShuttermanagementDevContext, TblEstimate>, IEstimateService
    {
        private readonly BtprojecShuttermanagementDevContext _context;
        private readonly IComponentPriceService componentPriceService;
        private readonly IExpressionService expressionService;
        private readonly IProductComponentService productComponentService;
        private readonly IComponentService componentService;
        private readonly IJobService jobService;
        private readonly IContactService contactService;
        private readonly IComponentMaterial componentMaterialService;
        private readonly IMaterial materialService;
        private readonly IWindowService windowService;
        private readonly IWindowProductService windowProductService;
        private readonly IWindowOptionService windowOptionService;
        private readonly IOptionsTypeService optionsTypeService;
        private readonly IEstimateWindowService estimateWindowService;
        private readonly IEstimateWindowProductService estimateWindowProductService;
        private readonly IEstimateWindowProductComponentService estimateWindowProductComponentService;
        private readonly IEstimateWindowProductComponentMaterialService estimateWindowProductComponentMaterialService;
        private readonly IProductService productService;
        private readonly ITenantService tenantService;
        private readonly ClaimsProperty claimsProperty;
        private readonly IMstProductType productType;
        private readonly EmailHelper emailHelper;
        private string Message = "";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOption optionService;
        private readonly ITaxRate taxRate;
        private readonly IRegion region;
        List<Guid> expressionList = new List<Guid>();

        public EstimateService(BtprojecShuttermanagementDevContext context,
            IComponentPriceService componentPriceService,
            IExpressionService expressionService,
            IProductComponentService productComponentService,
            IComponentService componentService,
            IJobService jobService,
            IContactService contactService,
            IComponentMaterial componentMaterialService,
            IMaterial materialService,
            IWindowService windowService,
            IWindowProductService windowProductService,
            IWindowOptionService windowOptionService,
            IOptionsTypeService optionsTypeService,
            IEstimateWindowService estimateWindowService,
            IEstimateWindowProductService estimateWindowProductService,
            IEstimateWindowProductComponentService estimateWindowProductComponentService,
            IEstimateWindowProductComponentMaterialService estimateWindowProductComponentMaterialService,
            IProductService productService,
            ITenantService tenantService, ClaimsProperty claimsProperty,
            IMstProductType productType,
            EmailHelper emailHelper,
            IHttpContextAccessor _httpContextAccessor,
            IOption optionService,
            ITaxRate taxRate,
            IRegion region
            )
        {
            _context = context;
            this.componentPriceService = componentPriceService;
            this.expressionService = expressionService;
            this.productComponentService = productComponentService;
            this.componentService = componentService;
            this.jobService = jobService;
            this.contactService = contactService;
            this.componentMaterialService = componentMaterialService;
            this.materialService = materialService;
            this.windowService = windowService;
            this.windowProductService = windowProductService;
            this.windowOptionService = windowOptionService;
            this.optionsTypeService = optionsTypeService;
            this.estimateWindowService = estimateWindowService;
            this.estimateWindowProductService = estimateWindowProductService;
            this.estimateWindowProductComponentService = estimateWindowProductComponentService;
            this.estimateWindowProductComponentMaterialService = estimateWindowProductComponentMaterialService;
            this.productService = productService;
            this.tenantService = tenantService;
            this.claimsProperty = claimsProperty;
            this.productType = productType;
            this.emailHelper = emailHelper;
            this._httpContextAccessor = _httpContextAccessor;
            this.optionService = optionService;
            this.taxRate = taxRate;
            this.region = region;
        }

        public async Task<IResponseVm<string>> ActiveInActiveEstimate(Guid EstimateId)
        {
            IResponseVm<string> responseVm = new ResponseVm<string>();
            try
            {
                // Log the start of the operation
                FileLogger.Log($"Start: Toggling Active/Inactive status for Estimate with ID: {EstimateId}");

                var estimate = GetAllQuerable()
                    .Where(c => c.EstimateId == EstimateId
                    && c.TenantId == AppConstant.LoggedInTenantId && c.IsDelete == false)
                    .FirstOrDefault();

                if (estimate == null)
                {
                    // Log failure when estimate is not found
                    FileLogger.Log($"Estimate with ID {EstimateId} not found.");
                    responseVm.Message = "Estimate " + AppConstant.NoDataFound;
                    return responseVm;
                }

                // Toggle the active status of the estimate
                estimate.IsActive = !estimate.IsActive;
                estimate.UpdatedAt = DateTime.Now;
                estimate.ModifiedBy = AppConstant.LoggedInUserID;

                // Log the status change
                FileLogger.Log($"Estimate with ID {EstimateId} status changed to: {(estimate.IsActive ?? false ? "Active" : "Inactive")}.");

                await Update(estimate);

                responseVm.IsSuccess = true;
                responseVm.Message = AppConstant.UpdateSuccessMsg;

                // Log the successful completion
                FileLogger.Log($"Estimate with ID {EstimateId} status update completed successfully.");
            }
            catch (Exception ex)
            {
                // Log exception if an error occurs
                FileLogger.Log($"Error occurred while updating Estimate with ID {EstimateId}: {ex.Message}");
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
            }
            return responseVm;
        }

        public async Task<IResponseVm<string>> DeleteEstimate(Guid EstimateId)
        {
            IResponseVm<string> responseVm = new ResponseVm<string>();
            try
            {
                // Log the start of the operation
                FileLogger.Log($"Start: Deleting Estimate with ID: {EstimateId}");

                var estimate = GetAllQuerable()
                    .Where(c => c.EstimateId == EstimateId
                    && c.TenantId == AppConstant.LoggedInTenantId && c.IsDelete == false)
                    .FirstOrDefault();

                if (estimate == null)
                {
                    // Log failure when estimate is not found
                    FileLogger.Log($"Estimate with ID {EstimateId} not found.");
                    responseVm.Message = "Estimate " + AppConstant.NoDataFound;
                    return responseVm;
                }

                // Mark the estimate as deleted
                estimate.IsActive = false;
                estimate.IsDelete = true;
                estimate.UpdatedAt = DateTime.Now;
                estimate.ModifiedBy = AppConstant.LoggedInUserID;

                // Log the deletion action
                FileLogger.Log($"Estimate with ID {EstimateId} marked as deleted.");

                await Update(estimate);

                responseVm.IsSuccess = true;
                responseVm.Message = AppConstant.UpdateSuccessMsg;

                // Log the successful completion
                FileLogger.Log($"Estimate with ID {EstimateId} deletion completed successfully.");
            }
            catch (Exception ex)
            {
                // Log exception if an error occurs
                FileLogger.Log($"Error occurred while deleting Estimate with ID {EstimateId}: {ex.Message}");
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
            }
            return responseVm;
        }

        public async Task<IResponseVm<List<EstimateList>>> GetAll_Estimate(Pagination filter)
        {
            IResponseVm<List<EstimateList>> responseVm = new ResponseVm<List<EstimateList>>();
            try
            {
                FileLogger.Log($"Start: Fetching estimates with filter parameters: TenantId = {filter.TenantId}, PageNo = {filter.PageNo}, RecordsPerPage = {filter.RecordPerPage}");

                // Validate Tenant if provided, else use the logged-in tenant
                var estimates = await FetchEstimates(filter);
                if (estimates == null)
                {
                    responseVm.Message = AppConstant.TenantNotFound;
                    return responseVm;
                }

                // Log the number of estimates fetched
                FileLogger.Log($"Fetched {estimates.Count} estimates.");

                // Get product types and filter records based on search and status
                var filteredEstimates = ApplyFilters(estimates, filter);

                // Paginate the filtered results
                var paginatedEstimates = Paginate.Pagination(filteredEstimates, filter);

                // Prepare final response
                responseVm.IsSuccess = true;
                if (paginatedEstimates.Any())
                {
                    responseVm.Data = paginatedEstimates;
                    responseVm.Message = AppConstant.FetchSuccessMsg;
                    responseVm.TotalRecords = filteredEstimates.Count;
                    FileLogger.Log($"Successfully fetched {paginatedEstimates.Count} estimates.");
                }
                else
                {
                    responseVm.Message = AppConstant.NoDataFound;
                    FileLogger.Log("No data found after applying filters.");
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error occurred while fetching estimates: {ex.Message}");
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
            }
            return responseVm;
        }

        public async Task<IResponseVm<string>> SendForApproval(Guid EstimateId)
        {
            IResponseVm<string> responseVm = new ResponseVm<string>();
            try
            {


                var IsEstimateExists = (await GetAll()).Where(x => x.IsDelete == false && x.TenantId == AppConstant.LoggedInTenantId && x.EstimateId == EstimateId).
                    OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                if (IsEstimateExists == null)
                {
                    responseVm.Message = "Estimate " + AppConstant.NoDataFound;
                    return responseVm;
                }
                IsEstimateExists.Status = EnumValue.GetEnumValue(Estimation.PendingForApproval);
                IsEstimateExists.UpdatedAt = DateTime.Now;
                IsEstimateExists.ModifiedBy = AppConstant.LoggedInUserID;
                await Update(IsEstimateExists);

                var jobData = jobService.GetAllQuerable().Where(x => x.JobId == IsEstimateExists.JobId && x.IsDelete == false).FirstOrDefault();
                if (jobData != null)
                {
                    var contactData = contactService.GetAllQuerable().Where(x => x.ContactId == jobData.ContactId && x.IsDelete == false).FirstOrDefault();
                    if (contactData != null)
                    {
                        var contactEmail = contactData.Email;
                        SendEmail sendEmail = new SendEmail
                        {
                            FromEmail = contactEmail,
                        };
                        //emailHelper.SendMail(sendEmail);
                    }
                }

                responseVm.IsSuccess = true;
                responseVm.Message = AppConstant.UpdateSuccessMsg;
            }
            catch (Exception ex)
            {
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
            }
            return responseVm;
        }

        public async Task<IResponseVm<string>> Create_Estimate(CreateEstimate model)
        {
            IResponseVm<string> responseVm = new ResponseVm<string>();
            FileLogger.Log("Start: Create Estimate...");

            try
            {
                // Validate tenant existence
                var isTenantExists = await ValidateTenantExists();
                if (!isTenantExists)
                {
                    responseVm.Message = AppConstant.TenantNotFound;
                    return responseVm; // Return early if tenant is not found
                }

                // Validate if the estimate already exists
                var isEstimateExists = await ValidateEstimateExists(model.EstimateName, (Guid)model.JobId, Guid.Empty);
                if (isEstimateExists)
                {
                    responseVm.Message = "Estimates Name " + AppConstant.AlreadyExists;
                    return responseVm; // Return early if estimate exists
                }

                if (model.WindowProductId == null || model.WindowProductId.Length == 0)
                {
                    responseVm.Message = "Atleast one window is required.";
                    return responseVm; // Return early if window not exists
                }

                // Create and add the estimate
                TblEstimate createEstimate = await CreateEstimateRecord(model);
                if (createEstimate.EstimateId != Guid.Empty)
                {
                    // Process window products and components
                    await ProcessWindowProducts(model.WindowProductId.ToList(), createEstimate.EstimateId);
                    if (!string.IsNullOrEmpty(Message))
                    {
                        responseVm.Message = Message;
                        return responseVm;
                    }
                    //var TotalPrice = await CalculateEstimate(createEstimate.EstimateId);
                    //createEstimate.TotalPrice = TotalPrice;
                    //await Update(createEstimate);
                    responseVm.IsSuccess = true;
                    responseVm.Message = AppConstant.InsertSuccessMsg;
                    FileLogger.Log("Estimate created and all related data added successfully.");
                }
                else
                {
                    responseVm.Message = AppConstant.NoDataFound;
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error occurred: {ex.Message}");
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
            }

            // Always return the response after completing the operation
            return responseVm;
        }

        public async Task<IResponseVm<List<WindowProdcutDetailsForEstimate>>> BuildEstimate(Guid[] windowProductIds, bool IsBreakdown)
        {
            IResponseVm<List<WindowProdcutDetailsForEstimate>> responseVm = new ResponseVm<List<WindowProdcutDetailsForEstimate>>();
            FileLogger.Log("Start: Calculate Estimate price...");

            try
            {
                var WindowProductData = new List<WindowProdcutDetailsForEstimate>();
                // Validate tenant existence
                var isTenantExists = await ValidateTenantExists();
                if (!isTenantExists)
                {
                    responseVm.Message = AppConstant.TenantNotFound;
                    return responseVm; // Return early if tenant is not found
                }
                if (windowProductIds == null || windowProductIds.Length == 0)
                {
                    responseVm.Message = "Incorrect data.";
                    return responseVm;
                }
                foreach (var windowProductId in windowProductIds)
                {
                    var data = await componentPriceService.GetProductData(windowProductId);
                    if (data.IsSuccess)
                    {
                        //calculate price
                        var price = await CalculateEstimate(data.Data);
                        data.Data.ForEach(x => x.TotalPrice = Convert.ToDecimal(price.ToString("0.00")));
                        WindowProductData.AddRange(data.Data);
                        responseVm.IsSuccess = true;
                    }
                }
                if (!IsBreakdown)
                {
                    var forProduct = WindowProductData.Select(x =>
                                        new WindowProdcutDetailsForEstimate
                                        {
                                            // Window Properties
                                            WindowId = x.WindowId,
                                            AddressId = x.AddressId,
                                            Label = x.Label,
                                            Width = x.Width,
                                            Height = x.Height,
                                            ArchHeight = x.ArchHeight,
                                            Quantity = x.Quantity,
                                            JobId = x.JobId,
                                            Options = x.Options,
                                            WindowProdcutId = x.WindowProdcutId,
                                            ProductId = x.ProductId,
                                            BuildWidth = x.BuildWidth,
                                            BuildHeight = x.BuildHeight,
                                            ProductName = x.ProductName,
                                            ProductWidth = x.Width,
                                            ProductHeight = x.Height,
                                            ProductArchHeight = x.ArchHeight,
                                            Area = x.Area,
                                            Length = x.Length,
                                            AdditionalWidth = x.AdditionalWidth,
                                            Markup = x.Markup,
                                            ProductTypeName = x.ProductName,
                                            TotalPrice = x.TotalPrice
                                        }).DistinctBy(x => x.WindowProdcutId).ToList();
                    responseVm.Data =  forProduct;
                }
                else
                {
                    responseVm.Data =  WindowProductData;
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error occurred: {ex.Message}");
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
            }

            // Always return the response after completing the operation
            return responseVm;
        }

        private async Task<decimal> CalculateEstimate(List<WindowProdcutDetailsForEstimate> data)
        {
            decimal finalSubTotal = 0.00M;

            foreach (var item in data)
            {
                var formula = item.ExpressionFormulae;

                // Define an expression with a nudget and a custom function

                // check placeholders
                string pattern = @"{{.*?}}";
                var matches = Regex.Matches(formula ?? "", pattern);
                if (matches != null && matches.Count() > 0)
                {
                    foreach (Match match in matches)
                    {
                        var property = match.Value.Replace("{{", "").Replace("}}", "");
                        var val = GetProperty.GetColumnValue(item, property);
                        if (val == null)
                        {
                            string[] info = property.Split("__");
                            if (info != null && info.Length > 0 && info.Length == 3)
                            {
                                if (info[0] == "Component")
                                {
                                    val = componentPriceService.GetAllQuerable().Where(x => x.ProductType == info[1] && x.ComponentName == info[2]).Select(x => (item.IsRated == true ? x.PriceRated : x.PriceNonRated)).FirstOrDefault();
                                }
                                else if (info[0] == "Options")
                                {
                                    //var option = 
                                    //val = windowOptionService.GetAllQuerable().Where(x => x. == info[1] && x.ComponentName == info[2]).Select(x => (item.IsRated == true ? x.PriceRated : x.PriceNonRated)).FirstOrDefault();
                                }
                                else if (info[0] == "Material")
                                {
                                    val = materialService.GetAllQuerable().Where(x => x.ProductType.ToString() == info[1] && x.MaterialName == info[2]).Select(x=>x.UnitCost).FirstOrDefault();
                                }
                            }
                        }
                        // Set parameters
                        formula = formula?.Replace(match.Value, Convert.ToDecimal(val).ToString());
                        //expression.Parameters[match.Value] = Convert.ToDecimal(val);
                    }
                }
                var expression = new NCalc.Expression(formula);
                var res = expression.Evaluate();
                item.Price = Convert.ToDecimal(res);
                finalSubTotal += Convert.ToDecimal(res);
            }
            return finalSubTotal;
        }

        public async Task<IResponseVm<string>> Update_Estimate(UpdatetEstimate model)
        {
            IResponseVm<string> responseVm = new ResponseVm<string>();
            FileLogger.Log("Start: Update Estimate...");

            try
            {
                // Validate tenant existence
                var isTenantExists = await ValidateTenantExists();
                if (!isTenantExists)
                {
                    responseVm.Message = AppConstant.TenantNotFound;
                    return responseVm;
                }

                // Fetch the estimate
                var estimate = GetAllQuerable()
                    .Where(c => c.EstimateId == model.EstimateId && c.TenantId == AppConstant.LoggedInTenantId && c.IsDelete == false)
                    .FirstOrDefault();

                if (estimate == null)
                {
                    responseVm.Message = "Estimate " + AppConstant.NoDataFound;
                    return responseVm;
                }

                if (model.WindowProductId == null || model.WindowProductId.Length == 0)
                {
                    responseVm.Message = "Atleast one window is required.";
                    return responseVm; // Return early if window not exists
                }
                // Check if the estimate name already exists
                var isEstimateExists = await ValidateEstimateExists(model.EstimateName, (Guid)model.JobId, model.EstimateId);
                if (isEstimateExists)
                {
                    responseVm.Message = "Estimate Name " + AppConstant.AlreadyExists;
                    return responseVm;
                }

                // Check if the estimate is pending approval
                if (estimate.Status == EnumValue.GetEnumValue(Estimation.PendingForApproval))
                {
                    responseVm.Message = "Waiting for approval, can't edit this estimate";
                    return responseVm;
                }

                // Update the estimate details
                estimate.EstimateName = model.EstimateName;
                estimate.Notes = model.Notes;
                estimate.DiscountAmount = model.Discount;
                estimate.DiscountCoupon = model.DiscountCoupon;
                estimate.GrandTotal = model.GrandTotal;
                estimate.JobId = model.JobId;
                estimate.TaxId = model.taxRateId;
                estimate.TaxAmount = model.Tax;
                estimate.TotalPrice = model.SubTotal;
                estimate.TermsCondition = model.TermsCondition;
                estimate.ModifiedBy = AppConstant.LoggedInUserID;
                estimate.UpdatedAt = DateTime.Now;

                await Update(estimate);
                FileLogger.Log($"Estimate {estimate.EstimateName} updated successfully.");

                // Deactivate old related records
                await DeactivateOldEstimateRecords(estimate.EstimateId);
                FileLogger.Log("Old related records deactivated successfully.");

                // Process new window products and components
                await ProcessWindowProducts(model.WindowProductId.ToList(), estimate.EstimateId);
                if (!string.IsNullOrEmpty(Message))
                {
                    responseVm.Message = Message;
                    return responseVm;
                }

                responseVm.IsSuccess = true;
                responseVm.Message = AppConstant.UpdateSuccessMsg;
                FileLogger.Log("Estimate update process completed successfully.");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error occurred: {ex.Message}");
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
            }

            return responseVm;
        }


        public async Task<IResponseVm<List<EstimateList>>> ViewEachEstimation(Guid EstimateId)
        {
            IResponseVm<List<EstimateList>> responseVm = new ResponseVm<List<EstimateList>>();
            try
            {
                if (EstimateId == Guid.Empty)
                {
                    responseVm.Message = "Invalid Estimate ID.";
                    return responseVm;
                }

                var estimate = GetAllQuerable()
                                       .Where(e => e.EstimateId == EstimateId && e.IsDelete == false)
                                       .FirstOrDefault();

                if (estimate == null)
                {
                    responseVm.Message = "Estimate not Found.";
                    return responseVm;
                }

                AppConstant.LoggedInTenantId = estimate.TenantId;

                Pagination filterValue = new Pagination
                {
                    SearchValue = EstimateId.ToString()
                };

                var estimateListFiltered = await GetAll_Estimate(filterValue);

                if (estimateListFiltered != null && estimateListFiltered.TotalRecords > 0 && estimateListFiltered.Data.Any())
                {
                    responseVm.Data = estimateListFiltered.Data;
                    responseVm.IsSuccess = true;
                    responseVm.TotalRecords = responseVm.TotalRecords;
                }
                else
                {
                    responseVm.IsSuccess = false;
                    responseVm.Message = "No estimates found after filtering.";
                }
            }
            catch (Exception ex)
            {
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
            }

            return responseVm;
        }

        public async Task<IResponseVm<string>> ChangeStatus(Guid EstimationId, Estimation NewEstimationStatus)
        {
            // Initial log indicating that the method is processing the request
            FileLogger.Log($"Processing status change for EstimationId: {EstimationId}...");

            IResponseVm<string> responseVm = new ResponseVm<string>();

            try
            {
                // Retrieve the EstimationStatus
                var EstimationStatus = GetEstimationStatusById(EstimationId);

                if (EstimationStatus == null)
                {
                    responseVm.Message = "Estimation not found.";
                    FileLogger.Log($"Estimation with ID {EstimationId} not found.");
                    return responseVm;
                }

                // Safely parse the current status to Estimation enum
                if (!Enum.TryParse(EstimationStatus.Status, out Estimation currentStatus))
                {
                    responseVm.Message = "Invalid status found in the system.";
                    FileLogger.Log($"Estimation with ID {EstimationId} has an invalid status.");
                    return responseVm;
                }

                // Check the current status of the Estimation and handle accordingly
                if (CanChangeStatus(currentStatus, NewEstimationStatus))
                {
                    EstimationStatus.Status = EnumValue.GetEnumValue(NewEstimationStatus);
                    await Update(EstimationStatus);
                    responseVm.IsSuccess = true;
                    responseVm.Message = AppConstant.UpdateSuccessMsg;
                    responseVm.Data = EnumValue.GetEnumValue(NewEstimationStatus);

                    // Log and record the status change in history
                    await EstimationChangeHistory(EstimationId, NewEstimationStatus);
                    FileLogger.Log($"Estimation status changed successfully to {NewEstimationStatus} for EstimationId: {EstimationId}");
                }
                else
                {
                    responseVm.Message = $"You can't change status from {currentStatus} to {NewEstimationStatus}.";
                    responseVm.Data = currentStatus.ToString();
                    FileLogger.Log($"Status change from {currentStatus} to {NewEstimationStatus} is not allowed.");
                }
            }
            catch (Exception ex)
            {
                // Catch any errors and log them
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
                FileLogger.Log($"Error processing status change for EstimationId: {EstimationId}. Error: {ex.Message}");
            }

            return responseVm;
        }


        #region private methods


        private async Task<List<TblEstimate>> FetchEstimates(Pagination filter)
        {
            if (string.IsNullOrEmpty(filter.TenantId))
            {
                // Log and fetch estimates for the logged-in tenant
                FileLogger.Log("No TenantId provided, fetching for the logged-in tenant.");
                return (await GetAll()).Where(x => x.IsDelete == false && x.TenantId == AppConstant.LoggedInTenantId)
                                       .OrderByDescending(x => x.CreatedAt)
                                       .ToList();
            }

            // Validate TenantId if provided
            var isRelatedTenant = tenantService.GetAllQuerable()
                .Where(x => x.TenantId.ToString() == filter.TenantId && x.ParentTenantId == AppConstant.LoggedInTenantId && x.IsDelete == false)
                .FirstOrDefault();

            if (isRelatedTenant == null)
            {
                FileLogger.Log($"Tenant with ID {filter.TenantId} is not related or not found.");
                return null; // Return null to indicate tenant validation failure
            }

            // Fetch estimates for the provided tenant
            FileLogger.Log($"Tenant with ID {filter.TenantId} validated successfully.");
            return (await GetAll()).Where(x => x.IsDelete == false && x.TenantId.ToString() == filter.TenantId)
                                   .OrderByDescending(x => x.CreatedAt)
                                   .ToList();
        }

        private List<EstimateList> ApplyFilters(List<TblEstimate> estimates, Pagination filter)
        {
            var estimateList = new List<EstimateList>();


            foreach (var estimate in estimates)
            {
                var e = new EstimateList
                {
                    EstimateId = estimate.EstimateId,
                    CreatedDate = estimate.CreatedAt,
                    JobId = estimate.JobId,
                    Status = estimate.Status,
                    IsActive = estimate.IsActive,
                    Discount = estimate.DiscountAmount,
                    DiscountCoupon = estimate.DiscountCoupon,
                    taxRateId = estimate.TaxId,
                    EstimateName = estimate.EstimateName,
                    EstimateCode = estimate.EstimateCode,
                    SubTotal = estimate.TotalPrice,
                    GrandTotal = estimate.GrandTotal,
                    Notes = estimate.Notes,
                    Tax = estimate.TaxAmount,
                    TermsCondition = estimate.TermsCondition,
                };
                var Userdetails = dbContext.TblUsers
                  .Where(x => x.Id == estimate.CreatedBy && x.IsDelete == false)
                  .FirstOrDefault();
                e.CreatedBy = Userdetails.FirstName + " " + Userdetails.LastName;
                var Tenant = dbContext.TblTenants.Where(e => e.TenantId == estimate.TenantId && e.IsDelete == false).FirstOrDefault();
                var tenantdetails = dbContext.TblTenantBrandingInfos
                 .Where(tb => tb.Id == Tenant.BrandingInfo)
                    .FirstOrDefault();
                e.primarycolor = tenantdetails.PrimaryColor;
                e.CompanyName = tenantdetails.CompanyName;
                var taxdetails = taxRate.GetAllQuerable()
                     .Where(x => x.TaxRateId == estimate.TaxId && x.IsDelete == false)
                    .FirstOrDefault();

                if (taxdetails != null)
                {
                    var region1 = region.GetAllQuerable()
                        .Where(x => x.Id == taxdetails.Region && x.IsDelete == false)
                        .Select(pt => pt.RegionName)
                        .FirstOrDefault();

                    e.TaxRate = taxdetails.Rate;
                    e.Region = region1 ?? "";
                }
                else
                {
                    e.TaxRate = 0;
                    e.Region = "";
                }


                // Fetch and add related products and options
                AddEstimateProducts(estimate, e);
                estimateList.Add(e);
            }

            // Filter records based on search value and status
            var filteredRecords = estimateList
                .Where(x =>
                    (filter.Status == 0 ? x.IsActive == false : (filter.Status == 1 ? x.IsActive == true : true)) &&
                    (string.IsNullOrWhiteSpace(filter.SearchValue) ||
                    ((x.Notes ?? "").Contains(filter.SearchValue)
                     || (x.JobId.ToString() ?? "").Contains(filter.SearchValue)
                     || (x.Status ?? "").Contains(filter.SearchValue)
                     || (x.EstimateId.ToString()).Contains(filter.SearchValue)
                     || (x.EstimateCode ?? "").Contains(filter.SearchValue)
                     || (x.EstimateName ?? "").Contains(filter.SearchValue)
                     || (x.TermsCondition ?? "").Contains(filter.SearchValue)
                     || (x.DiscountCoupon ?? "").Contains(filter.SearchValue)
                     || (x.GrandTotal.ToString() ?? "").Contains(filter.SearchValue)))
                ).ToList();

            return filteredRecords;
        }

        private void AddJobDetails(EstimateList e)
        {

            var jobdetails1 = (from j1 in dbContext.TblJobs
                               join j2 in dbContext.TblEstimates on j1.JobId equals j2.JobId
                               join j3 in dbContext.TblAddresses on j1.AddressId equals j3.AddressId
                               join j4 in dbContext.MstJobTypes on j1.JobType equals j4.Id
                               join j5 in dbContext.TblUsers on j1.AssignedPersonnel equals j5.Id
                               where j1.IsDelete == false
                               select new JobDetails
                               {
                                   JobName = j1.JobName,
                                   JobNotes = j1.Notes,
                                   JobType = j4.JobTypeName,
                                   AssignedPersonnel = j5.FirstName + " " + j5.LastName,
                                   Address = j3.StreetAddress + " " + j3.City + " " + j3.State + " " + j3.ZipCode,
                                   AddressLabel = j3.Label,
                                   ScheduleTime = j1.ScheduledDate
                               }).FirstOrDefault();

            e.jobDetails = jobdetails1;
        }


        private void AddEstimateProducts(TblEstimate estimate, EstimateList e)
        {
            var productList = estimateWindowProductService.GetAllQuerable()
                .Where(x => x.EstimateId == estimate.EstimateId && x.IsDelete == false)
                .Select(x => new
                {
                    x.EstimateWindowProductId,
                    x.ProductId,
                    x.ProductName,
                    x.ProductType,
                    x.Width,
                    x.Height,
                    x.ArchHeight,
                    x.Color,
                    x.Notes,
                    x.WindowId,
                    x.WindowProductId,
                    x.Price
                }).ToList();

            e.WindowProductId = productList.Select(p => (Guid)p.WindowProductId).ToArray();
            foreach (var p in productList)
            {
               
                var Options = (from windowOption in dbContext.WindowOptions
                               join optionType in dbContext.TblOptionsTypes on windowOption.OptionTypeId equals optionType.OptionTypeId
                               join options in dbContext.TblOptions on optionType.OptionId equals options.OptionId
                               where windowOption.WindowProductId == p.WindowProductId
                               select new
                               {
                                   OptionName = Convert.ToString(options.OptionName + " - " + optionType.OptionType)
                               }).ToList();
                var WindowDetails = estimateWindowService.GetAllQuerable()
                    .Where(x => x.EstimateId == estimate.EstimateId && x.IsDelete == false)
                    .ToList();
                var WindowQty_Label = windowService.GetAllQuerable()
                        .Where(w => w.WindowId == p.WindowId && w.IsDelete == false)
                        .FirstOrDefault();
                EstimateProductList estimateProduct = new EstimateProductList
                {
                    EstimateWindowProductId = p.EstimateWindowProductId,
                    Label = WindowQty_Label.Label,
                    WindowQty = WindowQty_Label.Quantity,
                    ProductType = p.ProductType,
                    ProductTypeName = productType.GetAllQuerable()
                        .Where(pt => pt.ProductId.ToString() == p.ProductType && pt.IsDelete == false)
                        .Select(pt => pt.ProductName)
                        .FirstOrDefault(),
                    Width = p.Width,
                    Height = p.Height,
                    ArchHeight = p.ArchHeight,
                    ProductName = p.ProductName,
                    WindowProductId = p.WindowProductId,
                    TotalPrice = p.Price,
                    Options = Options.Select(x=>x.OptionName).ToArray(),
                    EstimateWindowDetails = WindowDetails.Where(x => x.WindowId == p.WindowId).Select(x => new EstimateWindowList
                    {
                        ArchHeight = x.ArchHeight,
                        Height = x.Height,
                        Width = x.Width
                    }).FirstOrDefault()

                };
                e.EstimateProductList.Add(estimateProduct);
            }
            AddJobDetails(e);
        }

        private async Task<bool> ValidateTenantExists()
        {
            FileLogger.Log($"Checking if tenant with ID {AppConstant.LoggedInTenantId} exists...");
            var isTenantExists = await tenantService.AnyAsync(x => x.TenantId == AppConstant.LoggedInTenantId && x.IsActive == true && x.IsDelete == false);
            if (!isTenantExists)
            {
                FileLogger.Log("Tenant not found.");
            }
            return isTenantExists;
        }

        private async Task DeactivateOldEstimateRecords(Guid estimateId)
        {
            FileLogger.Log($"Deactivating old records for Estimate ID: {estimateId}");

            var oldWinEstList = (await estimateWindowService.GetAll()).Where(x => x.EstimateId == estimateId && x.IsDelete == false).ToList();
            foreach (var item in oldWinEstList)
            {
                item.IsActive = false;
                item.IsDelete = true;
                item.UpdatedAt = DateTime.Now;
                item.ModifiedBy = AppConstant.LoggedInUserID;
                await estimateWindowService.Update(item);
            }

            var oldWinEstProdList = (await estimateWindowProductService.GetAll()).Where(x => x.EstimateId == estimateId && x.IsDelete == false).ToList();
            foreach (var item in oldWinEstProdList)
            {
                item.IsActive = false;
                item.IsDelete = true;
                item.UpdatedAt = DateTime.Now;
                item.ModifiedBy = AppConstant.LoggedInUserID;
                await estimateWindowProductService.Update(item);
            }

            var oldWinEstProdComList = (await estimateWindowProductComponentService.GetAll()).Where(x => x.EstimateId == estimateId && x.IsDelete == false).ToList();
            foreach (var item in oldWinEstProdComList)
            {
                item.IsActive = false;
                item.IsDelete = true;
                item.UpdatedAt = DateTime.Now;
                item.ModifiedBy = AppConstant.LoggedInUserID;
                await estimateWindowProductComponentService.Update(item);
            }

            var oldWinEstProdComMatList = (await estimateWindowProductComponentMaterialService.GetAll()).Where(x => x.EstimateId == estimateId && x.IsDelete == false).ToList();
            foreach (var item in oldWinEstProdComMatList)
            {
                item.IsActive = false;
                item.IsDelete = true;
                item.UpdatedAt = DateTime.Now;
                item.ModifiedBy = AppConstant.LoggedInUserID;
                await estimateWindowProductComponentMaterialService.Update(item);
            }
        }

        private async Task<bool> ValidateEstimateExists(string estimateName, Guid jobId, Guid estimateId)
        {
            FileLogger.Log($"Checking if estimate with name {estimateName} already exists...");
            var isEstimateExists = (await GetAll())
                .Where(c => (estimateId != Guid.Empty ? c.EstimateId != estimateId : true) && c.EstimateName == estimateName && c.TenantId == AppConstant.LoggedInTenantId && c.IsDelete == false && c.JobId == jobId)
                .ToList();
            if (isEstimateExists.Count > 0)
            {
                FileLogger.Log($"Estimate with name {estimateName} already exists.");
                return true;
            }
            return false;
        }

        private async Task<TblEstimate> CreateEstimateRecord(CreateEstimate model)
        {
            FileLogger.Log("Creating new estimate...");
            TblEstimate createEstimate = new TblEstimate
            {
                EstimateId = Guid.NewGuid(),
                EstimateName = model.EstimateName,
                EstimateCode = GenerateUniqueId.GenerateUniqueIds(ModuleName.Estimation, AppConstant.LoggedInCompanyCode),
                Notes = model.Notes,
                DiscountAmount = model.Discount,
                DiscountCoupon = model.DiscountCoupon,
                GrandTotal = model.GrandTotal,
                JobId = model.JobId,
                TaxId = model.taxRateId,
                TaxAmount = model.Tax,
                TermsCondition = model.TermsCondition,
                CreatedBy = AppConstant.LoggedInUserID,
                CreatedAt = DateTime.Now,
                TenantId = AppConstant.LoggedInTenantId,
                IsActive = true,
                IsDelete = false,
                TotalPrice = model.SubTotal,
                Status = EnumValue.GetEnumValue(Estimation.New),
            };

            var res = await Add(createEstimate);
            return res;
        }

        private async Task ProcessWindowProducts(List<Guid> windowProductIds, Guid estimateId)
        {
            FileLogger.Log("Processing window products...");
            foreach (var product in windowProductIds)
            {
                var windowProductData = (await windowProductService.GetAll()).Where(x => x.WindowProductId == product && x.IsDelete == false).FirstOrDefault();
                if (windowProductData == null)
                {
                    Message = "Window Product " + AppConstant.NoDataFound;
                    return; // Return early if window product not found
                }

                var windowData = (await windowService.GetAll()).Where(x => x.WindowId == windowProductData.WindowId && x.IsDelete == false).FirstOrDefault();
                if (windowData == null)
                {
                    Message = "Window " + AppConstant.NoDataFound;
                    return; // Return early if window not found
                }

                await AddWindowToEstimate(windowData, windowProductData, estimateId);
                await AddProductToEstimate(windowProductData, estimateId);
                if (!string.IsNullOrEmpty(Message))
                {
                    return;
                }
                await AddComponentsToEstimate((Guid)windowProductData.ProductId, estimateId);
                if (!string.IsNullOrEmpty(Message))
                {
                    return;
                }
            }
        }

        private async Task AddWindowToEstimate(TblWindow windowData, TblWindowProduct windowProductData, Guid estimateId)
        {
            FileLogger.Log($"Adding window data for window {windowData.WindowId} to estimate...");
            TblEstimateWindow tblEstimateWindow = new TblEstimateWindow
            {
                EstWindowId = Guid.NewGuid(),
                EstimateId = estimateId,
                WindowId = windowData.WindowId,
                IsActive = true,
                IsDelete = false,
                CreatedAt = DateTime.Now,
                CreatedBy = AppConstant.LoggedInUserID,
                TenantId = AppConstant.LoggedInTenantId,
                Label = windowData.Label,
                AddressId = windowData.AddressId,
                ArchHeight = windowData.ArchHeight,
                Height = windowData.Height,
                Width = windowData.Width,
                Quantity = windowData.Quantity,
                Notes = windowData.Notes,
            };

            await estimateWindowService.Add(tblEstimateWindow);
        }

        private async Task AddProductToEstimate(TblWindowProduct windowProductData, Guid estimateId)
        {
            FileLogger.Log($"Adding product data for product {windowProductData.ProductId} to estimate...");
            var productData = (await productService.GetAll()).Where(x => x.ProductId == windowProductData.ProductId && x.IsDelete == false).FirstOrDefault();
            if (productData == null)
            {
                Message = "Product " + AppConstant.NoDataFound;
                return; // Return early if product data not found
            }
            Guid[] arr = { windowProductData.WindowProductId };
            var buildEstimate = await BuildEstimate(arr, false);
            TblEstimateWindowProduct tblEstimateWindowProduct = new TblEstimateWindowProduct
            {
                WindowId = (Guid)windowProductData.WindowId,
                ProductName = productData.ProductName,
                Height = windowProductData.BuildHeight,
                Width = windowProductData.BuildWidth,
                AdditionalWidth = productData.AdditionalWidth,
                Notes = productData.Notes,
                IsDelete = false,
                IsActive = true,
                Price = (buildEstimate.Data.FirstOrDefault() != null ? buildEstimate.Data.FirstOrDefault().TotalPrice : 0.00M ) ,
                CreatedAt = DateTime.Now,
                CreatedBy = AppConstant.LoggedInUserID,
                ArchHeight = productData.ArchHeight,
                Area = productData.Area,
                Color = productData.Color,
                EstimateId = estimateId,
                Length = productData.Length,
                Markup = productData.Markup,
                ProductType = productData.ProductType,
                TenantId = AppConstant.LoggedInTenantId,
                ProductId = productData.ProductId,
                EstimateWindowProductId = Guid.NewGuid(),
                WindowProductId = windowProductData.WindowProductId
            };

            await estimateWindowProductService.Add(tblEstimateWindowProduct);
        }

        private async Task AddComponentsToEstimate(Guid productId, Guid estimateId)
        {
            FileLogger.Log($"Adding components for product {productId} to estimate...");
            var productComponentData = (await productComponentService.GetAll()).Where(x => x.ProductId == productId && x.IsDelete == false).ToList();
            if (productComponentData == null || productComponentData.Count == 0)
            {
                Message = "No Components " + AppConstant.NoDataFound;
                return; // Return early if no components found
            }
            foreach (var component in productComponentData)
            {
                var componentData = (await componentService.GetAll()).Where(x => x.ComponentId == component.ComponentId && x.IsDelete == false).FirstOrDefault();
                if (componentData == null)
                {
                    Message = "Components " + AppConstant.NoDataFound;
                    return; // Return early if component data not found
                }
                //expressionList.Add(Guid.Parse(componentData.PricingExpression ?? ""));
                await AddComponentToEstimateWindow(componentData, (Guid)component.ProductId, estimateId);
                await AddComponentMaterialsToEstimate(componentData.ComponentId, estimateId);
                if (!string.IsNullOrEmpty(Message))
                {
                    return;
                }
            }
        }

        private async Task AddComponentToEstimateWindow(TblComponent componentData, Guid ProductId, Guid estimateId)
        {
            FileLogger.Log($"Adding component {componentData.ComponentName} to estimate...");
            TblEstimateWindowProductComponent tblEstimateWindowProductComponent = new TblEstimateWindowProductComponent
            {
                EstimateWindowProductComponentId = Guid.NewGuid(),
                ProductId = ProductId,
                ComponentId = componentData.ComponentId,
                EstimateId = estimateId,
                ComponentName = componentData.ComponentName,
                IsDelete = false,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = AppConstant.LoggedInUserID,
                MarkupPercentage = componentData.MarkupPercentage,
                Notes = componentData.Notes,
                ProductType = componentData.ProductType,
                PricingExpression = componentData.PricingExpression,
                TenantId = AppConstant.LoggedInTenantId,
            };

            await estimateWindowProductComponentService.Add(tblEstimateWindowProductComponent);
        }

        private async Task AddComponentMaterialsToEstimate(Guid componentId, Guid estimateId)
        {
            FileLogger.Log($"Adding materials for component {componentId} to estimate...");
            var componentMaterialData = (await componentMaterialService.GetAll()).Where(x => x.ComponentId == componentId && x.IsDelete == false).ToList();
            if (componentMaterialData == null || componentMaterialData.Count == 0)
            {
                Message = "No material " + AppConstant.NoDataFound;
                return; // Return early if no materials found
            }

            foreach (var material in componentMaterialData)
            {
                var materialData = (await materialService.GetAll()).Where(x => x.RawMaterialId == material.RawMaterialId && x.IsDelete == false).FirstOrDefault();
                if (materialData == null)
                {
                    Message = "Material " + AppConstant.NoDataFound;
                    return; // Return early if material data not found
                }

                await AddMaterialToEstimate(materialData, (Guid)material.ComponentId, estimateId);

            }
        }

        private async Task AddMaterialToEstimate(TblRawMaterial materialData, Guid ComponentId, Guid estimateId)
        {
            FileLogger.Log($"Adding material {materialData.MaterialName} to estimate...");
            TblEstimateWindowProductComponentMaterial tblEstimateWindowProductComponentMaterial = new TblEstimateWindowProductComponentMaterial
            {
                EstimateWindowProductComponentMaterialId = Guid.NewGuid(),
                ComponentId = ComponentId,
                IsDelete = false,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = AppConstant.LoggedInUserID,
                TenantId = AppConstant.LoggedInTenantId,
                EstimateId = estimateId,
                IsCutSheet = materialData.IsCutSheet,
                MaterialName = materialData.MaterialName,
                Notes = materialData.Notes,
                ProductType = materialData.ProductType,
                ReorderLevel = materialData.ReorderLevel,
                Sku = materialData.Sku,
                StockQuantity = materialData.StockQuantity,
                UnitCost = materialData.UnitCost,
                UnitOfMeasurement = materialData.UnitOfMeasurement,
                VendorId = materialData.VendorId,
                RawMaterialId = materialData.RawMaterialId,
            };

            await estimateWindowProductComponentMaterialService.Add(tblEstimateWindowProductComponentMaterial);
        }



        public bool CanChangeStatus(Estimation currentStatus, Estimation newStatus)
        {
            if (currentStatus == Estimation.New)
            {
                return newStatus == Estimation.PendingForApproval || newStatus == Estimation.Approved || newStatus == Estimation.Rejected;
            }
            else if (currentStatus == Estimation.PendingForApproval)
            {
                return newStatus == Estimation.Approved || newStatus == Estimation.Rejected;
            }
            else if (currentStatus == Estimation.Approved)
            {
                return newStatus == Estimation.GenerateOrder;
            }
            else if (currentStatus == Estimation.Rejected)
            {
                return newStatus == Estimation.New;
            }
            else if (currentStatus == Estimation.GenerateOrder)
            {
                return false;
            }

            return false;
        }


        private async Task<IResponseVm<string>> EstimationChangeHistory(Guid EstimationId, Estimation Status)
        {
            IResponseVm<string> responseVm = new ResponseVm<string>();
            try
            {
                TblEstimateApproved approved = new TblEstimateApproved
                {
                    EstimationId = EstimationId,
                    CurrentDatetime = DateTime.Now,
                    Ipaddress = GetClientIpAddress(),
                    Status = EnumValue.GetEnumValue(Status),
                };

                await dbContext.TblEstimateApproveds.AddAsync(approved);
                await dbContext.SaveChangesAsync();

                FileLogger.Log($"Estimation change history added for EstimationId: {EstimationId}, Status: {Status}");
            }
            catch (Exception ex)
            {
                responseVm.Message = AppConstant.ExceptionError + ex.Message;
                FileLogger.Log($"Error adding change history for EstimationId: {EstimationId}. Error: {ex.Message}");
            }
            return responseVm;
        }

        public string GetClientIpAddress()
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            return ipAddress;
        }

        private TblEstimate GetEstimationStatusById(Guid EstimationId)
        {
            // Fetch the Estimation status based on the EstimationId, ensuring we check IsDelete and IsActive flags
            return GetAllQuerable()
                    .Where(es => es.EstimateId == EstimationId && es.IsDelete == false && es.IsActive == true)
                    .FirstOrDefault();
        }


        #endregion




    }
}
