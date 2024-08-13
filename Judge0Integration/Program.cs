using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        // Step 1: Get user input for language and source code
        Console.WriteLine("Enter the language ID (e.g., 54 for C++): ");
        int languageId = int.Parse(Console.ReadLine());

        Console.WriteLine("Enter the source code (type 'END' on a new line to finish):");
        string sourceCode = ReadSourceCode();

        Console.WriteLine("Enter the input values (stdin): ");
        string stdin = Console.ReadLine();

        // Step 2: Submit Code
        var submissionId = await SubmitCodeAsync(languageId, sourceCode, stdin);

        // Step 3: Poll the status and fetch the result when ready
        if (submissionId != null)
        {
            await WaitForSubmissionResultAsync(submissionId);
        }
    }

    static string ReadSourceCode()
    {
        StringBuilder sourceCodeBuilder = new StringBuilder();
        string line;

        while ((line = Console.ReadLine()) != "END")
        {
            sourceCodeBuilder.AppendLine(line);
        }

        return sourceCodeBuilder.ToString();
    }

    static async Task<string> SubmitCodeAsync(int languageId, string sourceCode, string stdin)
    {
        var requestBody = new
        {
            language_id = languageId,
            source_code = sourceCode,
            stdin = stdin
        };

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://judge0-ce.p.rapidapi.com/submissions?fields=*"),
            Headers =
            {
                { "x-rapidapi-key", "837584a094msh74f33cabdc0fcffp1933fdjsn5a335fb0eddd" },
                { "x-rapidapi-host", "judge0-ce.p.rapidapi.com" },
            },
            Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
        };

        try
        {
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Submission Response: " + body);

                // Extract the submission ID from the response
                var responseJson = JObject.Parse(body);
                var submissionId = responseJson["token"]?.ToString();
                return submissionId;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error submitting code: " + ex.Message);
            return null;
        }
    }

    static async Task WaitForSubmissionResultAsync(string submissionId)
    {
        const int statusIdCompleted = 3; // Status ID for completed submission
        const int statusIdRunning = 1; // Status ID for running
        const int statusIdQueued = 2; // Status ID for queued

        bool isCompleted = false;

        while (!isCompleted)
        {
            // Wait before polling again
            await Task.Delay(2000); // 2 seconds delay between polls

            var result = await FetchSubmissionStatusAsync(submissionId);
            if (result != null)
            {
                var statusId = result["status"]["id"]?.ToObject<int>() ?? 0;
                Console.WriteLine($"Current Status ID: {statusId}");

                if (statusId == statusIdCompleted)
                {
                    isCompleted = true;
                    // Fetch and display the final result
                    await FetchSubmissionResultAsync(submissionId);
                }
                else if (statusId != statusIdRunning && statusId != statusIdQueued)
                {
                    // If the status is neither running nor queued, stop polling and show result
                    Console.WriteLine("Submission did not complete successfully. Status ID: " + statusId);
                    Console.WriteLine("Submission Result: " + result);
                    return;
                }
                // Otherwise, continue polling
            }
        }
    }

    static async Task<JObject> FetchSubmissionStatusAsync(string submissionId)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"https://judge0-ce.p.rapidapi.com/submissions/{submissionId}?base64_encoded=true&fields=*"),
            Headers =
            {
                { "x-rapidapi-key", "837584a094msh74f33cabdc0fcffp1933fdjsn5a335fb0eddd" },
                { "x-rapidapi-host", "judge0-ce.p.rapidapi.com" },
            },
        };

        try
        {
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                return JObject.Parse(body);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error fetching submission status: " + ex.Message);
            return null;
        }
    }

    static async Task FetchSubmissionResultAsync(string submissionId)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"https://judge0-ce.p.rapidapi.com/submissions/{submissionId}?base64_encoded=true&fields=*"),
            Headers =
            {
                { "x-rapidapi-key", "837584a094msh74f33cabdc0fcffp1933fdjsn5a335fb0eddd" },
                { "x-rapidapi-host", "judge0-ce.p.rapidapi.com" },
            },
        };

        try
        {
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Submission Result: " + body);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error fetching submission result: " + ex.Message);
        }
    }
}
