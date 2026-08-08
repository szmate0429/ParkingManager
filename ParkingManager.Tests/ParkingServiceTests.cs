using Moq;
using ParkingManager.Data;
using ParkingManager.Model;

namespace ParkingManager.Tests
{
    public class ParkingServiceTests
    {
        private readonly Mock<IParkingRepository> _mockRepo;
        private readonly ParkingService _service;

        public ParkingServiceTests()
        {
            _mockRepo = new Mock<IParkingRepository>();
            _service = new ParkingService(_mockRepo.Object);
        }

        [Fact]
        public async Task TryCreate_Fails_WhenEmailIsEmpty()
        {
            //create res
            var result = await _service.TryCreateReservationAsync(1, "", DateTime.Now.AddDays(1), DateTime.Now.AddDays(1).AddHours(1));

            //should return false
            Assert.False(result.Success);
            Assert.Equal("Az email cím nem lehet üres!", result.Message);
        }

        [Fact]
        public async Task TryCreate_Fails_WhenEndTimeIsBeforeStartTime()
        {
            //create a reservation where the end is earlier than the start
            var start = DateTime.Now.AddDays(1);
            var end = start.AddHours(-1); 

            var result = await _service.TryCreateReservationAsync(1, "test@test.hu", start, end);
            //should return false
            Assert.False(result.Success);
            Assert.Equal("A záró időpontnak a kezdő időpont után kell lennie!", result.Message);
        }

        [Fact]
        public async Task TryCreate_Fails_WhenStartTimeIsInThePast()
        {
            //create a reservation that started yesterday
            var start = DateTime.Now.AddDays(-1);
            var end = DateTime.Now.AddDays(1);

            var result = await _service.TryCreateReservationAsync(1, "test@test.hu", start, end);
            //should return false
            Assert.False(result.Success);
            Assert.Equal("Nem lehet múltbeli időpontra foglalni!", result.Message);
        }

        [Fact]
        public async Task TryCreate_Fails_WhenSpotDoesNotExist()
        {
            //create a reservation for a spot that does not exist
            var start = DateTime.Now.AddDays(1);
            var end = start.AddHours(2);

            _mockRepo.Setup(r => r.GetSpotByIdAsync(99)).ReturnsAsync((ParkingSpot?)null);

            var result = await _service.TryCreateReservationAsync(99, "test@test.hu", start, end);
            //should return false
            Assert.False(result.Success);
            Assert.Equal("A megadott parkolóhely nem létezik!", result.Message);
        }

        [Fact]
        public async Task TryCreate_Fails_WhenDisabledSpot_AndNoBadgeProvided()
        {
            //create reservation for a disabled spot with no badge
            var start = DateTime.Now.AddDays(1);
            var end = start.AddHours(2);
            var disabledSpot = new ParkingSpot { Id = 1, Code = "D-01", SpotType = "DISABLED" };

            _mockRepo.Setup(r => r.GetSpotByIdAsync(1)).ReturnsAsync(disabledSpot);

            var result = await _service.TryCreateReservationAsync(1, "test@test.hu", start, end, hasDisabledBadge: false);
            //should return false
            Assert.False(result.Success);
            Assert.Contains("csak érvényes mozgáskorlátozott igazolvánnyal", result.Message);
        }

        [Fact]
        public async Task TryCreate_Fails_WhenEvSpot_AndDurationExceeds3Hours()
        {
            //create a reservation for a ev spot while exceeding 3 hours
            var start = DateTime.Now.AddDays(1);
            var end = start.AddHours(4); 
            var evSpot = new ParkingSpot { Id = 2, Code = "EV-01", SpotType = "EV_CHARGING" };

            _mockRepo.Setup(r => r.GetSpotByIdAsync(2)).ReturnsAsync(evSpot);

            var result = await _service.TryCreateReservationAsync(2, "test@test.hu", start, end, isElectricVehicle: true);
            //should return false
            Assert.False(result.Success);
            Assert.Contains("maximum 3 órára lehet lefoglalni", result.Message);
        }

        [Fact]
        public async Task TryCreate_Fails_WhenSpotIsAlreadyBooked()
        {
            //create overlapping reservations
            var start = DateTime.Now.AddDays(1);
            var end = start.AddHours(2);
            var standardSpot = new ParkingSpot { Id = 3, Code = "A-01", SpotType = "STANDARD" };

            _mockRepo.Setup(r => r.GetSpotByIdAsync(3)).ReturnsAsync(standardSpot);

            //using moq simulate false return
            _mockRepo.Setup(r => r.CreateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(false);

            var result = await _service.TryCreateReservationAsync(3, "test@test.hu", start, end);
            //should return false
            Assert.False(result.Success);
            Assert.Equal("A választott parkolóhely ebben az idősávban már foglalt!", result.Message);
        }

        [Fact]
        public async Task TryCreate_Succeeds_WhenAllRulesPass()
        {
            //create a normal reservation
            var start = DateTime.Now.AddDays(1);
            var end = start.AddHours(2);
            var standardSpot = new ParkingSpot { Id = 4, Code = "A-02", SpotType = "STANDARD" };

            _mockRepo.Setup(r => r.GetSpotByIdAsync(4)).ReturnsAsync(standardSpot);

            _mockRepo.Setup(r => r.CreateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(true);

            var result = await _service.TryCreateReservationAsync(4, "test@test.hu", start, end);
            //should return true
            Assert.True(result.Success);
            Assert.Equal("A foglalás sikeresen rögzítve!", result.Message);
            Assert.NotNull(result.Reservation);
            Assert.Equal(4, result.Reservation.ParkingSpotId);
            Assert.Equal("test@test.hu", result.Reservation.RequesterEmail);

            _mockRepo.Verify(r => r.CreateReservationAsync(It.IsAny<Reservation>()), Times.Once);
        }

        [Fact]
        public async Task CancelReservation_ReturnsTrue_WhenRepoSucceeds()
        {
            //cancel reservation
            _mockRepo.Setup(r => r.CancelReservationAsync("res-uuid")).ReturnsAsync(true);

            var result = await _service.CancelReservationAsync("res-uuid");

            Assert.True(result);
            _mockRepo.Verify(r => r.CancelReservationAsync("res-uuid"), Times.Once);
        }

        [Fact]
        public async Task TryCreate_Fails_WhenEvSpot_AndVehicleIsNotElectric()
        {
            //create a reservation in ev spot with not an ev
            var start = DateTime.Now.AddDays(1);
            var end = start.AddHours(2);
            var evSpot = new ParkingSpot { Id = 5, Code = "EV-02", SpotType = "EV_CHARGING" };

            _mockRepo.Setup(r => r.GetSpotByIdAsync(5)).ReturnsAsync(evSpot);

            var result = await _service.TryCreateReservationAsync(5, "test@test.hu", start, end, isElectricVehicle: false);
            //should return false
            Assert.False(result.Success);
            Assert.Contains("csak zöld rendszámmal", result.Message);
        }

        [Fact]
        public async Task TryCreate_Succeeds_WhenEvSpot_AndElectricVehicle()
        {
            //create reservation in ev spot with ev and not exceeding 3 hours
            var start = DateTime.Now.AddDays(1);
            var end = start.AddHours(2); 
            var evSpot = new ParkingSpot { Id = 6, Code = "EV-03", SpotType = "EV_CHARGING" };

            _mockRepo.Setup(r => r.GetSpotByIdAsync(6)).ReturnsAsync(evSpot);
            _mockRepo.Setup(r => r.CreateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(true);

            var result = await _service.TryCreateReservationAsync(6, "test@test.hu", start, end, isElectricVehicle: true);
            //should return true
            Assert.True(result.Success);
            Assert.Equal("A foglalás sikeresen rögzítve!", result.Message);
        }

        [Fact]
        public async Task TryCreate_Succeeds_WhenDisabledSpot_AndBadgeProvided()
        {
            //create reservation in disabled spot with disabled badge provided
            var start = DateTime.Now.AddDays(1);
            var end = start.AddHours(2);
            var disabledSpot = new ParkingSpot { Id = 8, Code = "D-03", SpotType = "DISABLED" };

            _mockRepo.Setup(r => r.GetSpotByIdAsync(8)).ReturnsAsync(disabledSpot);
            _mockRepo.Setup(r => r.CreateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(true);

            var result = await _service.TryCreateReservationAsync(8, "test@test.hu", start, end, hasDisabledBadge: true);
            //should return true
            Assert.True(result.Success);
            Assert.Equal("A foglalás sikeresen rögzítve!", result.Message);
        }

        [Fact]
        public async Task CancelReservation_ReturnsFalse_WhenRepoFails()
        {
            //cancelling reservation should fail when given invalid uuid(Reservation Id)
            _mockRepo.Setup(r => r.CancelReservationAsync("invalid-uuid")).ReturnsAsync(false);

            var result = await _service.CancelReservationAsync("invalid-uuid");

            Assert.False(result);
            _mockRepo.Verify(r => r.CancelReservationAsync("invalid-uuid"), Times.Once);
        }
    }
}

