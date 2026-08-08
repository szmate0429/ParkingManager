using ParkingManager.Model;

namespace ParkingManager.Data
{
    public class ParkingService
    {
        private readonly IParkingRepository _repository;

        public ParkingService(IParkingRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ParkingSpot>> GetParkingSpotsAsync()
        {
            return await _repository.GetParkingSpotsAsync();
        }

        public async Task<IEnumerable<Reservation>> GetReservationsBySpotAsync(int spotId)
        {
            return await _repository.GetReservationsBySpotAsync(spotId);
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByUserSpotAsync(int spotId, string requesterEmail)
        {
            return await _repository.GetReservationsByUserSpotAsync(spotId, requesterEmail);
        }

        public async Task<IEnumerable<UserReservationDisplay>> GetActiveReservationsByEmailAsync(string email)
        {
            return await _repository.GetActiveReservationsByEmailAsync(email);
        }

        public async Task<(bool Success, string Message, Reservation? Reservation)> TryCreateReservationAsync(
            int spotId, string email, DateTime start, DateTime end,
            bool hasDisabledBadge = false, bool isElectricVehicle = false)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Az email cím nem lehet üres!", null);

            if (end <= start)
                return (false, "A záró időpontnak a kezdő időpont után kell lennie!", null);
            if (start < DateTime.Now)
                return (false, "Nem lehet múltbeli időpontra foglalni!", null);


            // Get spot and check type constraints
            var spot = await _repository.GetSpotByIdAsync(spotId);
            if (spot == null)
                return (false, "A megadott parkolóhely nem létezik!", null);

            if (spot.SpotType == "DISABLED" && !hasDisabledBadge)
            {
                return (false, "HIBA: Erre a helyre csak érvényes mozgáskorlátozott igazolvánnyal lehet foglalni!", null);
            }

            if (spot.SpotType == "EV_CHARGING")
            {
                if (!isElectricVehicle)
                    return (false, "HIBA: Az elektromos töltőhelyeket csak zöld rendszámmal lehet használni!", null);

                if ((end - start).TotalHours > 3)
                    return (false, "HIBA: Elektromos töltőhelyet maximum 3 órára lehet lefoglalni!", null);
            }

            var reservation = new Reservation
            {
                Id = Guid.NewGuid().ToString(),
                ParkingSpotId = spotId,
                RequesterEmail = email,
                StartTime = start,
                EndTime = end,
                IsActive = true,
            };
            bool isSuccess = await _repository.CreateReservationAsync(reservation);
            if (!isSuccess)
            {
                return (false, "A választott parkolóhely ebben az idősávban már foglalt!", null);
            }

            return (true, "A foglalás sikeresen rögzítve!", reservation);
        }

        public async Task<bool> CancelReservationAsync(string reservationId)
        {
            return await _repository.CancelReservationAsync(reservationId);
        }

        public async Task<ParkingSpot?> GetSpotByIdAsync(int spotId)
        {
            return await _repository.GetSpotByIdAsync(spotId);
        }
    }
}