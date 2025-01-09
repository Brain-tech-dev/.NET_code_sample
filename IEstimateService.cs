using ShutterManagement.Core.ViewModels.Request;
using ShutterManagement.Core.ViewModels;
using ShutterManagement.DAL.Context;
using ShutterManagement.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShutterManagement.Core.ViewModels.Response;
using ShutterManagement.Core.Common;

namespace ShutterManagement.Business.Interfaces
{
    public interface IEstimateService :IGenericRepository<TblEstimate>
    {
        Task<IResponseVm<string>> Create_Estimate(CreateEstimate model);
        Task<IResponseVm<string>> Update_Estimate(UpdatetEstimate model);
        Task<IResponseVm<string>> ActiveInActiveEstimate(Guid EstimateId);
        Task<IResponseVm<string>> DeleteEstimate(Guid EstimateId);
        Task<IResponseVm<List<WindowProdcutDetailsForEstimate>>> BuildEstimate(Guid[] windowProductIds, bool IsBreakDown);
        Task<IResponseVm<List<EstimateList>>> GetAll_Estimate(Pagination filter);
        Task<IResponseVm<string>> SendForApproval(Guid EstimateId);
        Task<IResponseVm<List<EstimateList>>> ViewEachEstimation(Guid EstimateId);
        Task<IResponseVm<string>> ChangeStatus(Guid EstimationId, Estimation NewEstimationStatus);

        //Task<IResponseVm<List<EstimationBreakdown>>> EstimationBreakdown(Guid windowProductId);
    }
}
