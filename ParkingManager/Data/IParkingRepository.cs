using ParkingManager.Model;

namespace ParkingManager.Data
{
    public interface IParkingRepository
    {
        Task<IEnumerable<ParkingSpot>> GetActiveParkingSpotsAsync();
        Task<ParkingSpot?> GetSpotByIdAsync(int spotId);
        Task<IEnumerable<Reservation>> GetReservationsBySpotAsync(int spotId);
        Task<IEnumerable<Reservation>> GetReservationsByUserSpotAsync(int spotId, string requesterEmail);
        Task<IEnumerable<UserReservationDisplay>> GetActiveReservationsByEmailAsync(string email);
        Task<bool> CreateReservationAsync(Reservation reservation);
        Task<bool> CancelReservationAsync(string reservationId);
    }
}