using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class BlumBot
{
    private static readonly HttpClient client = new HttpClient();
    private const int API_TIMEOUT = 30000; 

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
        Console.ResetColor();  
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

        var token = await GetToken(query, referralToken); 
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
            var tasks = await GetTasks(token);

            var notStartedTasks = tasks.Where(t => t["status"]?.ToString() == "NOT_STARTED").ToList();
            var readyForVerifyTasks = tasks.Where(t => t["status"]?.ToString() == "READY_FOR_VERIFY").ToList();
            var inProgressTasks = tasks.Where(t => t["status"]?.ToString() == "IN_PROGRESS").ToList();

            Log($"Not started tasks: {notStartedTasks.Count}", "INFO");
            Log($"Ready for verify tasks: {readyForVerifyTasks.Count}", "INFO");
            Log($"In progress tasks: {inProgressTasks.Count}", "INFO");

            foreach (var task in notStartedTasks)
            {
                string taskTitle = task["title"]?.ToString();
                string taskId = task["id"]?.ToString();

                if (taskTitle == "Farm" || taskTitle == "Invite")
                {
                    Log($"Skipping task: {taskTitle}", "WARNING");
                    continue;
                }

                var startResult = await StartTask(token, taskId, taskTitle);
                if (startResult != null && startResult["alreadyStarted"]?.ToObject<bool>() == true)
                {
                    Log($"Task \"{taskTitle}\" was already started", "WARNING");
                }
                else
                {
                    Log($"Started task: {taskTitle}", "SUCCESS");
                }

                await Task.Delay(5000);

                if (predefinedAnswers.TryGetValue(taskTitle, out string answer))
                {
                    await SubmitAnswer(token, taskId, answer);
                    Log($"Submitted answer for task: {taskTitle}", "SUCCESS");
                }
            }

            foreach (var task in readyForVerifyTasks)
            {
                string taskTitle = task["title"]?.ToString();
                string taskId = task["id"]?.ToString();

                if (predefinedAnswers.TryGetValue(taskTitle, out string keyword))
                {
                    Log($"Validating task \"{taskTitle}\" with keyword: \"{keyword}\"", "INFO");
                    var verifyResult = await VerifyTask(token, taskId, taskTitle, keyword);
                    if (verifyResult != null)
                    {
                        Log($"Task \"{taskTitle}\" verified successfully.", "SUCCESS");
                    }
                    else
                    {
                        Log($"Task \"{taskTitle}\" did not return a valid result.", "WARNING");
                    }
                }
                else
                {
                    Log($"No keyword found for task: {taskTitle}", "WARNING");
                }

                await Task.Delay(5000);
            }

            foreach (var task in notStartedTasks.Concat(readyForVerifyTasks).Concat(inProgressTasks))
            {
                string taskTitle = task["title"]?.ToString();
                string taskId = task["id"]?.ToString();

                var claimResult = await ClaimTaskReward(token, taskId, taskTitle);
                if (claimResult != null)
                {
                    Log($"Claimed reward for task: {taskTitle}", "SUCCESS");
                }
                else
                {
                    Log($"Unable to claim reward for task: {taskTitle}. It may not be ready yet.", "WARNING");
                }
                await Task.Delay(2000);
            }
        }
        catch (Exception ex)
        {
            Log($"Error completing tasks: {ex.Message}", "ERROR");
        }
    }

    static async Task<List<JObject>> GetTasks(string token)
    {
        try
        {
            Log("Fetching tasks...", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Get, "https://earn-domain.blum.codes/api/v1/tasks");
            request.Headers.Add("Authorization", token);

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            var result = JArray.Parse(content);

            var allTasks = new List<JObject>();

            foreach (var section in result)
            {
                if (section["tasks"] is JArray sectionTasks)
                {
                    allTasks.AddRange(sectionTasks.Cast<JObject>());
                }

                if (section["subSections"] is JArray subSections)
                {
                    foreach (var subSection in subSections)
                    {
                        if (subSection["tasks"] is JArray subSectionTasks)
                        {
                            allTasks.AddRange(subSectionTasks.Cast<JObject>());
                        }
                    }
                }
            }

            foreach (var task in allTasks)
            {
                Log($"Task: {task["title"]}, Status: {task["status"]}, ID: {task["id"]}", "INFO");
            }

            return allTasks;
        }
        catch (Exception ex)
        {
            Log($"Error fetching tasks: {ex.Message}", "ERROR");
            return new List<JObject>();
        }
    }

    static async Task<JObject> StartTask(string token, string taskId, string title)
    {
        try
        {
            Log($"Starting task: {title}", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://earn-domain.blum.codes/api/v1/tasks/{taskId}/start");
            request.Headers.Add("Authorization", token);

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Log($"Task \"{title}\" started successfully.", "SUCCESS");
                return JObject.Parse(content);
            }
            else
            {
                var errorObj = JObject.Parse(content);
                string errorMessage = errorObj["message"]?.ToString();
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && errorMessage?.Contains("already started") == true)
                {
                    Log($"Task \"{title}\" was already started", "WARNING");
                    return new JObject { { "alreadyStarted", true } };
                }
                else
                {
                    Log($"Error starting task \"{title}\": {errorMessage}", "ERROR");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Unexpected error starting task \"{title}\": {ex.Message}", "ERROR");
            return null;
        }
    }

    static async Task<JObject> VerifyTask(string token, string taskId, string title, string keyword)
    {
        try
        {
            Log($"Verifying task: {title} with keyword: {keyword}", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://earn-domain.blum.codes/api/v1/tasks/{taskId}/validate");
            request.Headers.Add("Authorization", token);
            var content = new StringContent(JsonConvert.SerializeObject(new { keyword }), Encoding.UTF8, "application/json");
            request.Content = content;

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Log($"Task \"{title}\" verified successfully with keyword: {keyword}", "SUCCESS");
                return JObject.Parse(responseContent);
            }
            else
            {
                var errorObj = JObject.Parse(responseContent);
                Log($"Error verifying task \"{title}\": {errorObj["message"]}", "ERROR");
                return null;
            }
        }
        catch (Exception ex)
        {
            Log($"Unexpected error verifying task \"{title}\": {ex.Message}", "ERROR");
            return null;
        }
    }

    static async Task<JObject> ClaimTaskReward(string token, string taskId, string title)
    {
        try
        {
            Log($"Claiming reward for task: {title}", "INFO");
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://earn-domain.blum.codes/api/v1/tasks/{taskId}/claim");
            request.Headers.Add("Authorization", token);

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Log($"Reward for task \"{title}\" claimed successfully.", "SUCCESS");
                return JObject.Parse(content);
            }
            else
            {
                var errorObj = JObject.Parse(content);
                string errorMessage = errorObj["message"]?.ToString();
                if (errorMessage == "Task is not done")
                {
                    Log($"Task \"{title}\" is not ready for claim yet.", "WARNING");
                }
                else
                {
                    Log($"Error claiming reward for task \"{title}\": {errorMessage}", "ERROR");
                }
                return null;
            }
        }
        catch (Exception ex)
        {
            Log($"Error claiming reward for task \"{title}\": {ex.Message}", "ERROR");
            return null;
        }
    }

    static async Task SubmitAnswer(string token, string taskId, string answer)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://earn-domain.blum.codes/api/v1/tasks/{taskId}/submit");
            request.Headers.Add("Authorization", token);
            var content = new StringContent(JsonConvert.SerializeObject(new { answer }), Encoding.UTF8, "application/json");
            request.Content = content;

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Log($"Answer submitted successfully for task {taskId}", "SUCCESS");
            }
            else
            {
                var errorObj = JObject.Parse(responseContent);
                Log($"Error submitting answer for task {taskId}: {errorObj["message"]}", "ERROR");
            }
        }
        catch (Exception ex)
        {
            Log($"Unexpected error submitting answer for task {taskId}: {ex.Message}", "ERROR");
        }
    }

    private static Dictionary<string, string> predefinedAnswers = new Dictionary<string, string>
    {
        { "What Are AMMs?", "CRYPTOSMART" },
        { "Say No to Rug Pull!", "SUPERBLUM" },
        { "What are Telegram Mini Apps?", "CRYPTOBLUM" },
        { "Navigating Crypto", "HEYBLUM" },
        { "Secure your Crypto!", "BEST PROJECT EVER" },
        { "Forks Explained", "GO GET" },
        { "How to Analyze Crypto?", "VALUE" }
    };

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
            var playPasses = result["playPasses"]?.ToObject<int>() ?? 0;

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
