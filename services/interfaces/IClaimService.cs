using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Contract_Monthly_Claim_System.Models;
using System.Collections.Generic;

namespace Contract_Monthly_Claim_System.services.interfaces
{
    public interface IClaimService
    {
        Task<Claim> AddClaimAsync(Claim claim);
        Task<Attachment> AddAttachmentAsync(int claimId, IFormFile file, string uploadedBy);
        Task<IEnumerable<Attachment>> GetAttachmentsByClaimIdAsync(int claimId);

        // Workflow methods
        Task SubmitForReviewAsync(int id);
        Task ApproveAsync(int id, string approvedBy);
        Task RejectAsync(int id, string rejectedBy, string reason);

        // Retrieval methods
        Task<IEnumerable<Claim>> GetAllPendingAsync();
        Task<Claim?> GetByIdAsync(int id);
        Task<IEnumerable<Claim>> GetAllForLecturerAsync(string lecturer);
        Task<IEnumerable<Claim>> GetAllClaimsAsync();
    }
}