using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

class BlumBot
{
    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        string queryFilePath = Path.Combine(AppContext.BaseDirectory, "query.txt");
        Console.WriteLine($"Working Directory: {Directory.GetCurrentDirectory()}");

        string referralToken = "554eWV40LM";  

        var queries = await ReadQueriesFromFile(queryFilePath);

        if (queries.Count > 0)
        {
            foreach (var query in queries)
            {
                await ProcessAccount(query, referralToken);
            }
        }
        else
        {
            Log("No queries found to process.", "WARNING");
        }
    }

    static void DisplayHeader()
    {
        Console.Clear();
        Console.WriteLine("==========================================");
        Console.WriteLine("BLUM BOT - Advanced Multi-Account Airdrop System v2.0");
        Console.WriteLine("🌐 Telegram Channel: t.me/slyntherinnn");
        Console.WriteLine("==========================================\n");
    }

    static void DisplayWatermark()
    {
        Console.WriteLine("\n=====================");
        Console.WriteLine("🌐 Telegram Channel: t.me/slyntherinnn");
        Console.WriteLine("=====================\n");
    }

    static void Log(string message, string status = "INFO")
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        switch (status.ToUpper())
        {
            case "INFO":
                Console.ForegroundColor = ConsoleColor.Cyan;
                break;
            case "SUCCESS":
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case "ERROR":
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case "WARNING":
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            default:
                Console.ResetColor();
                break;
        }

        Console.WriteLine($"[{timestamp}] [{status}] {message}");
        Console.ResetColor();  // Reset warna ke default setelah log
    }

    static async Task<List<string>> ReadQueriesFromFile(string filePath)
    {
        var queries = new List<string>();

        try
        {
            Log($"Reading queries from file: {filePath}", "INFO");

            if (File.Exists(filePath))
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        queries.Add(line);
                    }
                }
                Log($"Successfully read {queries.Count} queries from file.", "SUCCESS");
            }
            else
            {
                Log($"File not found: {filePath}", "ERROR");
            }
        }
        catch (Exception ex)
        {
            Log($"Error reading file: {ex.Message}", "ERROR");
        }

        return queries;
    }

    static async Task<string> GetToken(string query, string referralToken)
    {
        try
        {
            var data = new JObject
            {
                { "query", query },
                { "referralToken", referralToken }
            };

            var response = await client.PostAsync(
                "https://user-domain.blum.codes/api/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP",
                new StringContent(data.ToString(), System.Text.Encoding.UTF8, "application/json")
            );

            if (response.IsSuccessStatusCode)
            {
                var result = JObject.Parse(await response.Content.ReadAsStringAsync());
                var token = result["token"]?["access"]?.ToString();

                if (!string.IsNullOrEmpty(token))
                {
                    Log("Token retrieved successfully", "SUCCESS");
                    return $"Bearer {token}";
                }
            }

            Log("Failed to retrieve a valid token.", "ERROR");
            return null;
        }
        catch (Exception ex)
        {
            Log($"Error fetching token: {ex.Message}", "ERROR");
            return null;
        }
    }

    static async Task ProcessAccount(string query, string referralToken)
    {
        DisplayWatermark();
        Log($"Processing account with query: {query}", "INFO");

        var token = await GetToken(query, referralToken);  // Kirim query langsung
        if (token == null)
        {
            Log("Token is undefined! Skipping this account.", "ERROR");
            return;
        }

        Log("Token retrieved successfully.", "SUCCESS");

        await ClaimFarmReward(token);
        await StartFarmingSession(token);
        await ClaimDailyReward(token);
        await CompleteTasks(token);
        await ClaimGamePoints(token);

        Log($"All processes completed for query: {query}", "SUCCESS");
    }

    static async Task ClaimFarmReward(string token)
    {
        try
        {
            Log("Attempting to claim farm reward...", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Post, "https://game-domain.blum.codes/api/v1/farming/claim");
            request.Headers.Add("Authorization", token);

            var response = await client.SendAsync(request);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());

            if (result["success"]?.ToObject<bool>() == true)
            {
                Log("Farm reward claimed successfully!", "SUCCESS");
            }
            else
            {
                Log("Farm claim failed.", "ERROR");
            }
        }
        catch (Exception ex)
        {
            Log($"Error claiming farm reward: {ex.Message}", "ERROR");
        }
    }

    static async Task StartFarmingSession(string token)
    {
        try
        {
            Log("Attempting to start farming session...", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Post, "https://game-domain.blum.codes/api/v1/farming/start");
            request.Headers.Add("Authorization", token);

            var response = await client.SendAsync(request);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());

            Log($"Farming session started: {result}", "SUCCESS");
        }
        catch (Exception ex)
        {
            Log($"Error starting farming session: {ex.Message}", "ERROR");
        }
    }

    static async Task ClaimDailyReward(string token)
    {
        try
        {
            Log("Attempting to claim daily reward...", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Post, "https://game-domain.blum.codes/api/v1/daily-reward?offset=-420");
            request.Headers.Add("Authorization", token);

            var response = await client.SendAsync(request);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());

            Log($"Daily reward claimed: {result}", "SUCCESS");
        }
        catch (Exception ex)
        {
            Log($"Error claiming daily reward: {ex.Message}", "ERROR");
        }
    }

    static async Task CompleteTasks(string token)
    {
        try
        {
            Log("Fetching tasks and auto completing them...", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Get, "https://earn-domain.blum.codes/api/v1/tasks");
            request.Headers.Add("Authorization", token);

            var response = await client.SendAsync(request);
            var tasks = JArray.Parse(await response.Content.ReadAsStringAsync());

            foreach (var task in tasks)
            {
                string taskStatus = task["status"]?.ToString();
                string taskTitle = task["title"]?.ToString();
                string taskId = task["id"]?.ToString();

                Log($"Task: {taskTitle}, Status: {taskStatus}, ID: {taskId}", "INFO");

                if (taskStatus == "NOT_STARTED")
                {
                    await StartTask(token, taskId, taskTitle);
                    Log($"Started task: {taskTitle}", "SUCCESS");
                }

                if (taskStatus == "READY_FOR_VERIFY")
                {
                    await VerifyTask(token, taskId, taskTitle);
                    Log($"Verified task: {taskTitle}", "SUCCESS");
                }

                await ClaimTaskReward(token, taskId, taskTitle);
            }
        }
        catch (Exception ex)
        {
            Log($"Error completing tasks: {ex.Message}", "ERROR");
        }
    }

    static async Task StartTask(string token, string taskId, string title)
    {
        try
        {
            Log($"Starting task: {title}", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://earn-domain.blum.codes/api/v1/tasks/{taskId}/start");
            request.Headers.Add("Authorization", token);

            await client.SendAsync(request);
            Log($"Task \"{title}\" started successfully.", "SUCCESS");
        }
        catch (Exception ex)
        {
            Log($"Error starting task \"{title}\": {ex.Message}", "ERROR");
        }
    }

    static async Task VerifyTask(string token, string taskId, string title)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://earn-domain.blum.codes/api/v1/tasks/{taskId}/verify");
            request.Headers.Add("Authorization", token);

            await client.SendAsync(request);
            Log($"Task \"{title}\" verified successfully.", "SUCCESS");
        }
        catch (Exception ex)
        {
            Log($"Error verifying task \"{title}\": {ex.Message}", "ERROR");
        }
    }

    static async Task ClaimTaskReward(string token, string taskId, string title)
    {
        try
        {
            Log($"Claiming reward for task: {title}", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://earn-domain.blum.codes/api/v1/tasks/{taskId}/claim");
            request.Headers.Add("Authorization", token);

            await client.SendAsync(request);
            Log($"Reward for task \"{title}\" claimed successfully.", "SUCCESS");
        }
        catch (Exception ex)
        {
            Log($"Error claiming reward for task \"{title}\": {ex.Message}", "ERROR");
        }
    }

    static async Task ClaimGamePoints(string token)
    {
        try
        {
            Log("🎮 Starting game points claiming...", "INFO");

            var gameChances = await GetBalance(token);
            if (gameChances <= 0)
            {
                Log("No game chances available. Skipping game points claiming.", "WARNING");
                return;
            }

            int batchSize = 3;

            Random random = new Random();

            for (int i = 0; i < gameChances; i += batchSize)
            {
                int gamesToPlay = Math.Min(batchSize, gameChances - i);
                Log($"Playing {gamesToPlay} games in batch...", "INFO");

                List<Task> playTasks = new List<Task>();
                List<string> gameIds = new List<string>();

                for (int j = 0; j < gamesToPlay; j++)
                {
                    playTasks.Add(Task.Run(async () =>
                    {
                        var playRequest = new HttpRequestMessage(HttpMethod.Post, "https://game-domain.blum.codes/api/v1/game/play");
                        playRequest.Headers.Add("Authorization", token);

                        var playResponse = await client.SendAsync(playRequest);
                        var playResult = JObject.Parse(await playResponse.Content.ReadAsStringAsync());
                        var gameId = playResult["gameId"]?.ToString();

                        if (!string.IsNullOrEmpty(gameId))
                        {
                            gameIds.Add(gameId);
                            Log($"Game ID: {gameId} started. Waiting for 32 seconds before claiming...", "INFO");
                        }
                        else
                        {
                            Log("Failed to start game. Skipping this game.", "ERROR");
                        }
                    }));
                }

                await Task.WhenAll(playTasks);

                await Task.Delay(32000);

                foreach (var gameId in gameIds)
                {
                    int randomPoints = random.Next(199, 250);

                    Log($"Attempting to claim points for game {gameId} with {randomPoints} points...", "INFO");

                    var claimRequest = new HttpRequestMessage(HttpMethod.Post, "https://game-domain.blum.codes/api/v1/game/claim");
                    claimRequest.Headers.Add("Authorization", token);
                    claimRequest.Content = new StringContent($"{{ \"gameId\": \"{gameId}\", \"points\": {randomPoints} }}", System.Text.Encoding.UTF8, "application/json");

                    Log($"Claim Request: {await claimRequest.Content.ReadAsStringAsync()}", "INFO");
 
                    var claimResponse = await client.SendAsync(claimRequest);
                    var responseContent = await claimResponse.Content.ReadAsStringAsync();

                    Log($"Claim Response: {responseContent}", "INFO");

                    if (responseContent == "OK")
                    {
                        Log($"Successfully claimed {randomPoints} points for game {gameId}. Response: {responseContent}", "SUCCESS");
                    }
                    else if (claimResponse.IsSuccessStatusCode)
                    {
                        try
                        {
                            var claimResult = JObject.Parse(responseContent);
                            Log($"Successfully claimed {randomPoints} points for game {gameId}: {claimResult}", "SUCCESS");
                        }
                        catch (Exception ex)
                        {
                            Log($"Error parsing claim result for game {gameId}: {ex.Message}. Response: {responseContent}", "ERROR");
                        }
                    }
                    else
                    {
                        Log($"Failed to claim points for game {gameId}. Response: {responseContent}", "ERROR");
                    }
                }

                await Task.Delay(1000);
            } 

            Log("🏁 All available game chances have been played and claimed.", "SUCCESS");
        }
        catch (Exception ex)
        {
            Log($"Error claiming game points: {ex.Message}", "ERROR");
        }
    }

    static async Task<int> GetBalance(string token)
    {
        try
        {
            Log("Checking balance for game chances...", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Get, "https://game-domain.blum.codes/api/v1/user/balance");
            request.Headers.Add("Authorization", token);

            var response = await client.SendAsync(request);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var playPasses = result["playPasses"]?.ToObject<int>() ?? 0; // Jumlah tiket (game chances)

            Log($"You have {playPasses} game chances available.", "INFO");
            return playPasses;
        }
        catch (Exception ex)
        {
            Log($"Error fetching balance: {ex.Message}", "ERROR");
            return 0;
        }
    }
}