using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.services.interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Contract_Monthly_Claim_System.services
{
    public class ClaimService : IClaimService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        public ClaimService(ApplicationDbContext context)
        {
            _context = context;
            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        public async Task<Claim> AddClaimAsync(Claim claim)
        {
            claim.Status = ClaimStatus.Draft;
            claim.CreatedAt = DateTime.UtcNow;
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
            return claim;
        }

        public async Task<Attachment> AddAttachmentAsync(int claimId, IFormFile file, string uploadedBy)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file upload.");

            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!Array.Exists(allowedExtensions, e => e == extension))
                throw new InvalidOperationException("Unsupported file type. Allowed: PDF, DOCX, XLSX");

            if (file.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("File too large. Max size is 5MB.");

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                ClaimId = claimId,
                FileName = file.FileName,
                FileType = extension.Trim('.'),
                FileSize = file.Length,
                FilePath = $"/uploads/{fileName}",
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.Now
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            return attachment;
        }

        // === AUTOMATED VERIFICATION LOGIC ADDED HERE ===
        public async Task SubmitForReviewAsync(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
                throw new KeyNotFoundException($"Claim with ID {id} not found.");

            if (claim.Status == ClaimStatus.Draft)
            {
                var automationNotes = new List<string>();

                // Rule 1: Auto-Reject if hours are zero or negative
                if (claim.HoursWorked <= 0)
                {
                    claim.Status = ClaimStatus.Rejected;
                    claim.Notes = (claim.Notes ?? string.Empty) + "\n❌ System Auto-Rejection: Zero hours submitted.";
                    await _context.SaveChangesAsync();
                    return;
                }

                // Rule 2: Flag excessive hours (> 100)
                if (claim.HoursWorked > 100)
                {
                    automationNotes.Add("⚠️ System Flag: Hours exceed typical limit (100h). Review required.");
                }

                // Rule 3: Flag high hourly rates (> R300)
                if (claim.HourlyRate > 300)
                {
                    automationNotes.Add("⚠️ System Flag: Hourly rate is above standard threshold (R300).");
                }

                // Append flags to notes
                if (automationNotes.Any())
                {
                    claim.Notes = (claim.Notes ?? string.Empty) + "\n-- Automation Report --\n" + string.Join("\n", automationNotes);
                }

                // Move to Pending status
                claim.Status = ClaimStatus.Pending;
                await _context.SaveChangesAsync();
            }
        }
        // ===============================================

        public async Task ApproveAsync(int id, string approvedBy)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
                throw new KeyNotFoundException($"Claim with ID {id} not found.");

            if (claim.Status == ClaimStatus.Pending)
            {
                claim.Status = ClaimStatus.Approved;
                claim.Notes = (claim.Notes ?? string.Empty) + $"\nApproved by {approvedBy} on {DateTime.UtcNow}";
                await _context.SaveChangesAsync();
            }
        }

        public async Task RejectAsync(int id, string rejectedBy, string reason)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
                throw new KeyNotFoundException($"Claim with ID {id} not found.");

            if (claim.Status == ClaimStatus.Pending)
            {
                claim.Status = ClaimStatus.Rejected;
                claim.Notes = (claim.Notes ?? string.Empty) + $"\nRejected by {rejectedBy} with reason: '{reason}' on {DateTime.UtcNow}";
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Claim?> GetByIdAsync(int id)
        {
            return await _context.Claims
                                 .Include(c => c.Attachments)
                                 .FirstOrDefaultAsync(c => c.ClaimId == id);
        }

        public async Task<IEnumerable<Claim>> GetAllForLecturerAsync(string lecturer)
        {
            return await _context.Claims
                                 .Where(c => c.LecturerName == lecturer)
                                 .OrderByDescending(c => c.CreatedAt)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Claim>> GetAllPendingAsync()
        {
            return await _context.Claims
                                 .Where(c => c.Status == ClaimStatus.Pending)
                                 .OrderBy(c => c.ClaimPeriod)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsByClaimIdAsync(int claimId)
        {
            return await _context.Attachments.Where(a => a.ClaimId == claimId).ToListAsync();
        }

        public async Task<IEnumerable<Claim>> GetAllClaimsAsync()
        {
            return await _context.Claims
                                 .OrderByDescending(c => c.CreatedAt)
                                 .ToListAsync();
        }
    }
}