using Dapper;
using ParkingManager.Model;

namespace ParkingManager.Data
{
    public class ParkingRepository : IParkingRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ParkingRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }


        public async Task<IEnumerable<ParkingSpot>> GetParkingSpotsAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            const string sql = @"SELECT Id, Code,SpotType
                             FROM ParkingSpots 
                             ORDER BY Code";
            return await db.QueryAsync<ParkingSpot>(sql);
        }
        public async Task<ParkingSpot?> GetSpotByIdAsync(int spotId)
        {
            using var db = _connectionFactory.CreateConnection();
            const string sql = "SELECT Id, Code, SpotType FROM ParkingSpots WHERE Id = :Id";
            return await db.QueryFirstOrDefaultAsync<ParkingSpot>(sql, new { Id = spotId });
        }

        public async Task<IEnumerable<Reservation>> GetReservationsBySpotAsync(int spotId)
        {
            using var db = _connectionFactory.CreateConnection();
            const string sql = @"SELECT Id, ParkingSpotId, RequesterEmail, StartTime, EndTime, IsActive 
                             FROM Reservations 
                             WHERE ParkingSpotId = :SpotId 
                               AND IsActive = TRUE
                             ORDER BY StartTime";
            return await db.QueryAsync<Reservation>(sql, new { SpotId = spotId });
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByUserSpotAsync(int spotId, string requesterEmail)
        {
            using var db = _connectionFactory.CreateConnection();
            const string sql = @"SELECT Id, ParkingSpotId, RequesterEmail, StartTime, EndTime, IsActive 
                             FROM Reservations 
                             WHERE ParkingSpotId = :SpotId
                               AND RequesterEmail = :Email 
                               AND IsActive = TRUE
                             ORDER BY StartTime";
            return await db.QueryAsync<Reservation>(sql, new { SpotId = spotId, Email = requesterEmail });
        }

        public async Task<IEnumerable<UserReservationDisplay>> GetActiveReservationsByEmailAsync(string email)
        {
            using var db = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT r.Id, p.Code AS SpotCode, p.SpotType,r.StartTime, r.EndTime 
                FROM Reservations r
                JOIN ParkingSpots p ON r.ParkingSpotId = p.Id
                WHERE r.RequesterEmail = :Email AND r.IsActive = TRUE
                ORDER BY r.StartTime";

            return await db.QueryAsync<UserReservationDisplay>(sql, new { Email = email });
        }

        public async Task<bool> CreateReservationAsync(Reservation reservation)
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();

            using var transaction = db.BeginTransaction();

            try
            {
                // only lock parking space that we want to update
                const string lockSql = "SELECT 1 FROM ParkingSpots WHERE Id = :Id FOR UPDATE";
                await db.ExecuteAsync(lockSql, new { Id = reservation.ParkingSpotId }, transaction);

                // collision check
                const string checkSql = @"
                 SELECT COUNT(*) 
                 FROM Reservations 
                 WHERE ParkingSpotId = :SpotId 
                 AND IsActive = TRUE 
                 AND StartTime < :ReqEnd 
                 AND EndTime > :ReqStart";

                int conflictCount = await db.ExecuteScalarAsync<int>(checkSql, new
                {
                    SpotId = reservation.ParkingSpotId,
                    ReqStart = reservation.StartTime,
                    ReqEnd = reservation.EndTime
                }, transaction);

                if (conflictCount > 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // create reservation
                const string insertSql = @"
            INSERT INTO Reservations (Id, ParkingSpotId, RequesterEmail, StartTime, EndTime, IsActive)
            VALUES (:Id, :ParkingSpotId, :RequesterEmail, :StartTime, :EndTime, :IsActive)";

                await db.ExecuteAsync(insertSql, reservation, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> CancelReservationAsync(string reservationId)
        {
            using var db = _connectionFactory.CreateConnection();
            const string sql = @"UPDATE Reservations 
                             SET IsActive = FALSE 
                             WHERE Id = :Id AND IsActive = TRUE";

            var rowsAffected = await db.ExecuteAsync(sql, new { Id = reservationId });
            return rowsAffected > 0;
        }
    }
}