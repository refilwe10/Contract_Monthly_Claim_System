using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.services;
using System.Linq;

namespace ContractClaim.Tests
{
    public class ClaimServiceTests
    {
        // Helper method to create a fresh in-memory database context for each test
        private ApplicationDbContext GetDbContext()
        {
            // Use a unique database name to ensure tests are isolated
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            // Ensure the context is fresh for testing
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return context;
        }

        // Test 1: Verify a new claim is created in Draft status
        [Fact]
        public async Task AddClaimAsync_CreatesClaimInDraftStatus()
        {
            // Arrange
            using var context = GetDbContext();
            var service = new ClaimService(context);
            var newClaim = new Claim
            {
                LecturerName = "test@example.com",
                ClaimPeriod = new DateTime(2025, 10, 1),
                HoursWorked = 50,
                HourlyRate = 250.00m
            };

            // Act
            var createdClaim = await service.AddClaimAsync(newClaim);

            // Assert
            Assert.NotNull(createdClaim);
            Assert.Equal(ClaimStatus.Draft, createdClaim.Status);
            Assert.Equal("test@example.com", createdClaim.LecturerName);

            var savedClaim = await context.Claims.FindAsync(createdClaim.ClaimId);
            Assert.Equal(50 * 250.00m, savedClaim.Amount);
        }

        // Test 2: Verify SubmitForReviewAsync correctly sets status to Pending
        [Fact]
        public async Task SubmitForReviewAsync_SetsStatusToPending()
        {
            // Arrange
            using var context = GetDbContext();
            var service = new ClaimService(context);

            var claim = new Claim
            {
                LecturerName = "submit@example.com",
                Status = ClaimStatus.Draft,
                HoursWorked = 40, // Not excessive, won't trigger flag
                HourlyRate = 200.00m, // Not high, won't trigger flag
                ClaimPeriod = DateTime.Now
            };
            context.Claims.Add(claim);
            await context.SaveChangesAsync();

            // Act
            await service.SubmitForReviewAsync(claim.ClaimId);

            // Assert
            var submittedClaim = await context.Claims.FindAsync(claim.ClaimId);
            Assert.Equal(ClaimStatus.Pending, submittedClaim.Status);
        }

        // Test 3: Verify Automated Rejection Rule (Zero Hours)
        [Fact]
        public async Task SubmitForReviewAsync_AutoRejectsOnZeroHours()
        {
            // Arrange
            using var context = GetDbContext();
            var service = new ClaimService(context);

            var claim = new Claim
            {
                Status = ClaimStatus.Draft,
                HoursWorked = 0, // Zero Hours trigger
                HourlyRate = 200.00m,
                LecturerName = "reject@example.com",
                ClaimPeriod = DateTime.Now
            };
            context.Claims.Add(claim);
            await context.SaveChangesAsync();

            // Act
            await service.SubmitForReviewAsync(claim.ClaimId);

            // Assert
            var rejectedClaim = await context.Claims.FindAsync(claim.ClaimId);
            Assert.Equal(ClaimStatus.Rejected, rejectedClaim.Status);
            Assert.Contains("System Auto-Rejection: Zero hours submitted.", rejectedClaim.Notes);
        }
    }
}