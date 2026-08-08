using ParkingManager.Data;
using ParkingManager.Model;

namespace ParkingManager.Tests
{
    [Collection("Database Sequential")]
    public class ParkingRepositoryIntegrationTests
    {
        private readonly IParkingRepository _repository;

        public ParkingRepositoryIntegrationTests()
        {
            string connectionString = "Data Source=localhost:1521/FREEPDB1;User Id=parking_user;Password=ParkingPassword123!;";
            IDbConnectionFactory factory = new OracleConnectionFactory(connectionString);
            _repository = new ParkingRepository(factory);
        }

        [Fact]
        public async Task GetParkingSpotsAsync_ReturnsDataFromDatabase()
        {
            var spots = await _repository.GetParkingSpotsAsync();

            Assert.NotNull(spots);
            Assert.True(spots.Any(), "No parking space from database.");

            var firstSpot = spots.First();
            Assert.True(firstSpot.Id > 0);
            Assert.False(string.IsNullOrWhiteSpace(firstSpot.Code));
        }

        [Fact]
        public async Task CreateReservation_SavesToDatabase_AndCanBeReadBack()
        {
            //create reservation
            string testEmail = "integration@test.hu";
            var newReservation = new Reservation
            {
                Id = Guid.NewGuid().ToString(),
                ParkingSpotId = 1,
                RequesterEmail = testEmail,
                StartTime = new DateTime(2050, 1, 1, 10, 0, 0),
                EndTime = new DateTime(2050, 1, 1, 12, 0, 0),
                IsActive = true
            };

            try
            {
                bool insertSuccess = await _repository.CreateReservationAsync(newReservation);

                Assert.True(insertSuccess, "Save was not successful");

                //read it back from db
                var userReservations = await _repository.GetActiveReservationsByEmailAsync(testEmail);

                Assert.Contains(userReservations, r => r.Id == newReservation.Id);
            }
            finally
            {
                // CLEANUP
                await _repository.CancelReservationAsync(newReservation.Id);
            }
        }

        [Fact]
        public async Task CreateReservation_PreventsOverlapping_RealDatabaseLock()
        {
            //create two overlapping reservations
            var baseReservation = new Reservation
            {
                Id = Guid.NewGuid().ToString(),
                ParkingSpotId = 2,
                RequesterEmail = "overlap1@test.hu",
                StartTime = new DateTime(2060, 1, 1, 10, 0, 0),
                EndTime = new DateTime(2060, 1, 1, 12, 0, 0),
                IsActive = true
            };

            var overlappingReservation = new Reservation
            {
                Id = Guid.NewGuid().ToString(),
                ParkingSpotId = 2,
                RequesterEmail = "overlap2@test.hu",
                StartTime = new DateTime(2060, 1, 1, 11, 0, 0),
                EndTime = new DateTime(2060, 1, 1, 13, 0, 0),
                IsActive = true
            };

            try
            {
                //try to reserve while overlapping
                await _repository.CreateReservationAsync(baseReservation);

                bool success = await _repository.CreateReservationAsync(overlappingReservation);

                Assert.False(success, "Database should have rejected the overlapping reservations");
            }
            finally
            {
                // CLEANUP
                await _repository.CancelReservationAsync(baseReservation.Id);
                await _repository.CancelReservationAsync(overlappingReservation.Id);
            }
        }
        [Fact]
        public async Task CancelReservation_LogicallyDeletes_InDatabase()
        {
            //create a reservation
            var reservationToCancel = new Reservation
            {
                Id = Guid.NewGuid().ToString(),
                ParkingSpotId = 3,
                RequesterEmail = "cancel@test.hu",
                StartTime = new DateTime(2070, 1, 1, 10, 0, 0),
                EndTime = new DateTime(2070, 1, 1, 12, 0, 0),
                IsActive = true
            };
            await _repository.CreateReservationAsync(reservationToCancel);

            //cancel the reservation
            bool cancelSuccess = await _repository.CancelReservationAsync(reservationToCancel.Id);


            Assert.True(cancelSuccess);

            //check if removed from active reservations
            var userReservations = await _repository.GetActiveReservationsByEmailAsync("cancel@test.hu");
            Assert.DoesNotContain(userReservations, r => r.Id == reservationToCancel.Id);
        }
    }
}