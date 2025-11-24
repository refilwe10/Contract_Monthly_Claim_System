using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contract_Monthly_Claim_System.Models
{
    public enum ClaimStatus { Draft, Pending, Verified, Approved, Rejected, Settled }

    public class Claim
    {
        private string name;
        private string testUserEmail;

        public Claim(string name, string lecturerName)
        {
            this.name = name;
            LecturerName = lecturerName;
        }

        public Claim(string name, string testUserEmail, string lecturerName)
        {
            this.name = name;
            this.testUserEmail = testUserEmail;
            LecturerName = lecturerName;
        }

        [Key]
        public int ClaimId { get; set; }

        [Required, MaxLength(200)]
        public string LecturerName { get; set; } = string.Empty;

        [Required]
        public DateTime ClaimPeriod { get; set; }

        [Required]
        [Range(0, 1000)]
        public decimal HoursWorked { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [NotMapped]
        public decimal Amount => HoursWorked * HourlyRate;

        public string? Notes { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}