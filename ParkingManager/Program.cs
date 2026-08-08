using ParkingManager.Data;
using ParkingManager.Model;
using Microsoft.Extensions.Configuration;

namespace ParkingManager;

public class Program
{
    private static ParkingService _parkingService = null!;

    public static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

        IConfiguration config = builder.Build();

        var connectionString = config.GetConnectionString("OracleDb");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Nem található az 'OracleDb' connection string az appsettings.json fájlban!");
            return;
        }

        IDbConnectionFactory connectionFactory =
            new OracleConnectionFactory(connectionString);

        IParkingRepository repository =
            new ParkingRepository(connectionFactory);

        _parkingService = new ParkingService(repository);

        while (true)
        {
            ShowMenu();

            string choice = Console.ReadLine()?.Trim() ?? "";

            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await ListParkingSpotsAsync();
                    break;

                case "2":
                    await GetReservationsBySpotAsync();
                    break;

                case "3":
                    await CreateReservationAsync();
                    break;

                case "4":
                    await CancelReservationAsync();
                    break;

                case "5":
                    Console.WriteLine("Kilépés...");
                    return;

                default:
                    Console.WriteLine("Érvénytelen menüpont!");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("Nyomj ENTER-t a folytatáshoz...");
            Console.ReadLine();
            Console.Clear();
        }
    }

    private static void ShowMenu()
    {
        Console.WriteLine("=================================");
        Console.WriteLine("       PARKOLÓHELY FOGLALÁS");
        Console.WriteLine("=================================");
        Console.WriteLine("1. Parkolóhelyek listázása");
        Console.WriteLine("2. Adott hely foglalásainak lekérdezése");
        Console.WriteLine("3. Új foglalás rögzítése");
        Console.WriteLine("4. Foglalás lemondása");
        Console.WriteLine("5. Kilépés");
        Console.WriteLine("=================================");
        Console.Write("Válassz egy menüpontot: ");
    }

    // ============================================================
    // 1. Parkolóhelyek listázása
    // ============================================================

    private static async Task ListParkingSpotsAsync()
    {
        Console.WriteLine("=== PARKOLÓHELYEK ===");

        var spots = await _parkingService.GetActiveParkingSpotsAsync();

        foreach (var spot in spots)
        {
            Console.WriteLine(
                $"ID: {spot.Id} | " +
                $"Kód: {spot.Code} | " +
                $"Típus: {spot.SpotType}");
        }

        if (!spots.Any())
        {
            Console.WriteLine("Nincs elérhető parkolóhely.");
        }
    }

    // ============================================================
    // 2. Adott hely foglalásainak lekérdezése
    // ============================================================

    private static async Task GetReservationsBySpotAsync()
    {
        await ListParkingSpotsAsync();
        Console.WriteLine("=== PARKOLÓHELY FOGLALÁSAI ===");

        int spotId = ReadInt("Add meg a parkolóhely ID-ját: ");

        var reservations =
            await _parkingService.GetReservationsBySpotAsync(spotId);

        if (!reservations.Any())
        {
            Console.WriteLine("Ehhez a parkolóhelyhez nincs foglalás.");
            return;
        }

        foreach (var reservation in reservations)
        {
            Console.WriteLine(
                $"ID: {reservation.Id[..8]}... | " +
                $"Email: {reservation.RequesterEmail} | " +
                $"Kezdés: {reservation.StartTime:yyyy-MM-dd HH:mm} | " +
                $"Vége: {reservation.EndTime:yyyy-MM-dd HH:mm}");
        }
    }

    // ============================================================
    // 3. Új foglalás rögzítése
    // ============================================================

    private static async Task CreateReservationAsync()
    {
        await ListParkingSpotsAsync();
        Console.WriteLine("=== ÚJ FOGLALÁS ===");

        int spotId = ReadInt("Parkolóhely ID: ");

        string email = ReadRequiredString("Email cím: ");

        DateTime start = ReadDateTime(
            "Kezdő időpont (yyyy-MM-dd HH:mm): ");

        DateTime end = ReadDateTime(
            "Záró időpont (yyyy-MM-dd HH:mm): ");

        // Először lekérjük a parkolóhelyet,
        // hogy tudjuk, milyen típusú.
        var spot = await GetSpotForReservationAsync(spotId);

        if (spot == null)
        {
            return;
        }

        bool hasDisabledBadge = false;
        bool isElectricVehicle = false;

        // Mozgáskorlátozott hely esetén
        // megkérdezzük, rendelkezik-e igazolvánnyal.
        if (spot.SpotType == "DISABLED")
        {
            hasDisabledBadge = ReadYesNo(
                "Rendelkezel érvényes mozgáskorlátozott igazolvánnyal?");
        }

        // Elektromos töltőhely esetén
        // megkérdezzük, hogy elektromos-e a jármű.
        if (spot.SpotType == "EV_CHARGING")
        {
            isElectricVehicle = ReadYesNo(
                "Elektromos járművel érkeztél?");
        }

        var result =
            await _parkingService.TryCreateReservationAsync(
                spotId,
                email,
                start,
                end,
                hasDisabledBadge,
                isElectricVehicle);

        Console.WriteLine(result.Message);

        if (result.Success && result.Reservation != null)
        {
            Console.WriteLine();
            Console.WriteLine("Foglalás adatai:");
            Console.WriteLine($"Foglalás ID: {result.Reservation.Id[..8]}...");
            Console.WriteLine($"Parkolóhely: {result.Reservation.ParkingSpotId}");
            Console.WriteLine($"Email: {result.Reservation.RequesterEmail}");
            Console.WriteLine(
                $"Kezdés: {result.Reservation.StartTime:yyyy-MM-dd HH:mm}");
            Console.WriteLine(
                $"Vége: {result.Reservation.EndTime:yyyy-MM-dd HH:mm}");
        }
    }

    // ============================================================
    // 4. Foglalás lemondása
    // ============================================================

    private static async Task CancelReservationAsync()
    {
        Console.WriteLine("=== FOGLALÁS LEMONDÁSA ===");

        string email = ReadRequiredString("Email cím: ");

        var reservations =
            (await _parkingService.GetActiveReservationsByEmailAsync(email))
            .ToList();

        if (!reservations.Any())
        {
            Console.WriteLine(
                "Ehhez az email címhez nem tartozik aktív foglalás.");

            return;
        }

        Console.WriteLine();
        Console.WriteLine("Aktív foglalások:");
        Console.WriteLine();

        for (int i = 0; i < reservations.Count; i++)
        {
            var reservation = reservations[i];

            Console.WriteLine(
                $"{i + 1}. " +
                $"Foglalás ID: {reservation.Id[..8]}... | " +
                $"Parkolóhely: {reservation.SpotCode} | " +
                $"Kezdés: {reservation.StartTime:yyyy-MM-dd HH:mm} | " +
                $"Vége: {reservation.EndTime:yyyy-MM-dd HH:mm}");
        }

        Console.WriteLine();

        int choice = ReadInt(
                $"Válaszd ki a lemondandó foglalást (1-{reservations.Count}), " + "vagy 0 a visszalépéshez: ");
        if (choice == 0)
        {
            Console.WriteLine("A lemondás megszakítva.");
            return;
        }
        while (choice < 1 || choice > reservations.Count)
        {
            Console.WriteLine($"Érvénytelen választás! " + $"1 és {reservations.Count} közötti számot adj meg, " + "vagy 0 a visszalépéshez.");

            choice = ReadInt("Választás: ");
            if (choice == 0)
            {
                Console.WriteLine("A lemondás megszakítva.");
                return;
            }
        }

        var selectedReservation = reservations[choice - 1];

        Console.WriteLine();
        Console.WriteLine("Kiválasztott foglalás:");
        Console.WriteLine(
            $"Parkolóhely: {selectedReservation.SpotCode}");
        Console.WriteLine(
            $"Kezdés: {selectedReservation.StartTime:yyyy-MM-dd HH:mm}");
        Console.WriteLine(
            $"Vége: {selectedReservation.EndTime:yyyy-MM-dd HH:mm}");

        bool confirm = ReadYesNo(
            "Biztosan le szeretnéd mondani ezt a foglalást?");

        if (!confirm)
        {
            Console.WriteLine("A lemondás megszakítva.");
            return;
        }

        bool success =
            await _parkingService.CancelReservationAsync(
                selectedReservation.Id);

        if (success)
        {
            Console.WriteLine("A foglalás sikeresen lemondva.");
        }
        else
        {
            Console.WriteLine(
                "A foglalás lemondása sikertelen.");
        }
    }

    // ============================================================
    // Segédmetódusok
    // ============================================================

    private static int ReadInt(string message)
    {
        while (true)
        {
            Console.Write(message);

            string input = Console.ReadLine()?.Trim() ?? "";

            if (int.TryParse(input, out int result))
            {
                return result;
            }

            Console.WriteLine("Kérlek, egy egész számot adj meg!");
        }
    }

    private static string ReadRequiredString(string message)
    {
        while (true)
        {
            Console.Write(message);

            string input = Console.ReadLine()?.Trim() ?? "";

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            Console.WriteLine("A mező nem lehet üres!");
        }
    }

    private static DateTime ReadDateTime(string message)
    {
        while (true)
        {
            Console.Write(message);

            string input = Console.ReadLine()?.Trim() ?? "";

            if (DateTime.TryParse(input, out DateTime result))
            {
                return result;
            }

            Console.WriteLine(
                "Érvénytelen dátum! " +
                "Használd például: 2026-08-10 14:00");
        }
    }

    private static bool ReadYesNo(string message)
    {
        while (true)
        {
            Console.Write($"{message} (i/n): ");

            string input =
                Console.ReadLine()?.Trim().ToLower() ?? "";

            switch (input)
            {
                case "i":
                case "igen":
                case "y":
                case "yes":
                    return true;

                case "n":
                case "nem":
                case "no":
                    return false;

                default:
                    Console.WriteLine(
                        "Kérlek, 'i' vagy 'n' választ adj!");
                    break;
            }
        }
    }

    private static async Task<ParkingSpot?> GetSpotForReservationAsync(
        int spotId)
    {
        // Ha a ParkingService-ben még nincs ilyen metódus,
        // érdemes hozzáadni:
        //


        var spot =
            await _parkingService.GetSpotByIdAsync(spotId);

        if (spot == null)
        {
            Console.WriteLine(
                "A megadott parkolóhely nem létezik.");
        }

        return spot;
    }
}
