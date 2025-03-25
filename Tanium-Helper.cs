using System;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.VisualBasic.FileIO;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_MAXIMIZE = 3;

    private static readonly string taniumApiUrl = "https://fake-tenant-api.cloud.tanium.com/plugin/products/gateway/graphql";
    private static readonly string taniumServer = "https://fake-tenant.cloud.tanium.com";
    private static readonly string apiKey = "token-1234567890MyFakeToken1234567890";
    private static readonly string wd = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
    public static string AttrinAdd = wd + "//Attributes_Add.txt";
    public static int counter = 0;
    string selectedOpt = string.Empty;
    static async Task Main(string[] args)
    {
        // Maximize the console window
        IntPtr consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero)
        {
            ShowWindow(consoleWindow, SW_MAXIMIZE);
        }

        // Handle Ctrl + C exit
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\nCtrl + C detected. Exiting...");
            Environment.Exit(0);
        };


        // Prompt the user number of updates
        Console.WriteLine(@"
                                                                                                                                  
        _/_/_/_/_/    _/_/    _/      _/  _/_/_/  _/    _/  _/      _/              _/    _/            _/                                
           _/      _/    _/  _/_/    _/    _/    _/    _/  _/_/  _/_/              _/    _/    _/_/    _/  _/_/_/      _/_/    _/  _/_/   
          _/      _/_/_/_/  _/  _/  _/    _/    _/    _/  _/  _/  _/  _/_/_/_/_/  _/_/_/_/  _/_/_/_/  _/  _/    _/  _/_/_/_/  _/_/        
         _/      _/    _/  _/    _/_/    _/    _/    _/  _/      _/              _/    _/  _/        _/  _/    _/  _/        _/           
        _/      _/    _/  _/      _/  _/_/_/    _/_/    _/      _/              _/    _/    _/_/_/  _/  _/_/_/      _/_/_/  _/            
                                                                                                       _/                                 
                                                                                                      _/                                  
        ");
        Console.WriteLine("\t\t\t Welcome to the Tanium-Helper Console Application.\r\n\r\nFor now the only feature is setting Asset Attributes.");
        Console.WriteLine("To update attributes one by one enter \"1\". \r\nTo update attributes unsing a csv file enter \"2\".");
        Console.WriteLine("Option 2 (csv file) requires the following format and the first line (header) will be skipped: Application,Owner,Responsable,Vertical,Notes");
        Console.WriteLine("To exit press Ctrl + C or type exit");

        await SelectOption();

        static async Task SelectOption()
        {
            string selectedOpt = string.Empty;
            Console.Write("Option: ");
            while (true)
            {
                string input = Console.ReadLine()?.Trim().ToLower();
                if (input == "exit")
                {
                    Console.WriteLine("Exiting application...");
                    Environment.Exit(0);
                }
                else if (input == "1")
                {
                    if (counter == 0)
                    {
                        Console.WriteLine("\nLet's update asset attributes one by one.");
                        await singleHostUpdate();
                        counter++;  // Increment counter so next time it asks for another update
                    }
                    else
                    {
                        Console.Write("\nWould you like to update asset attributes on another host? (Y/N): ");
                        string inputXX = Console.ReadLine()?.Trim().ToLower();
                        if (inputXX == "n")
                        {
                            Console.WriteLine("Exiting application...");
                            Environment.Exit(0);
                        }
                        else if (inputXX == "y")
                        {
                            await singleHostUpdate();
                            counter++;  // Increment counter for each update
                        }
                        else
                        {
                            Console.WriteLine("Invalid input. Please enter Y or N.");
                        }
                    }
                }
                else if (input == "2")
                {
                    Console.WriteLine("\nLet's update asset attributes with a CSV file.");
                    await csvHostUpdate();
                    Console.WriteLine("Finished updating Attributes in CSV");
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    Console.WriteLine($"You entered: {input}");
                }
            }
        }
    }

    static async Task singleHostUpdate()
    {
        while (true)
        {
            string input = Console.ReadLine()?.Trim().ToLower();
            if (input == "exit")
            {
                Console.WriteLine("Exiting application...");
                Environment.Exit(0);
            }
            else
            {
                List<string> attributes = new List<string>();

                Console.Write("Enter Hostname: ");
                string inputHostName = Console.ReadLine()?.Trim();
                attributes.Add($"Hostname: {inputHostName}");

                Console.Write("Enter Application: ");
                string application = Console.ReadLine();
                attributes.Add($"Application: {application}");

                Console.Write("Enter Owner: ");
                string owner = Console.ReadLine();
                attributes.Add($"Owner: {owner}");

                Console.Write("Enter Responsible: ");
                string responsable = Console.ReadLine();
                attributes.Add($"Responsable: {responsable}");

                Console.Write("Enter Vertical: ");
                string vertical = Console.ReadLine();
                attributes.Add($"Vertical: {vertical}");

                Console.Write("Enter Notes: ");
                string notes = Console.ReadLine();
                attributes.Add($"Notes: {notes}");

                // Confirmation loop to ensure valid input
                while (true)
                {
                    Console.Write("\nReview and confirm values.\n1 - Apply Values\n2 - Start Over\nSelect: ");
                    string okay = Console.ReadLine()?.Trim();

                    if (okay == "1")
                    {


                        Console.WriteLine("Sending Update!");
                        // Extract hostname separately
                        string hostname = attributes[0].Split(": ", 2).LastOrDefault()?.Trim();
                        if (string.IsNullOrEmpty(hostname))
                        {
                            Console.WriteLine("Error: Hostname is required.");
                            return;
                        }
                        // Get Asset ID based on hostname
                        string assetId = await GetAssetIdByHostname(hostname);


                        // Prepare attribute list (skip the first item, which is Hostname)
                        var attributeList = new List<object>();
                        for (int i = 1; i < attributes.Count; i++) // Start at index 1 to skip Hostname
                        {
                            string[] parts = attributes[i].Split(": ", 2);
                            if (parts.Length == 2)
                            {
                                string attributeName = parts[0].Trim(); // "Application"
                                string value = parts[1].Trim();         // "Test"

                                attributeList.Add(new { attributeName, value });
                            }
                        }

                        string result = await UpdateCustomAttributes(attributeList.ToArray(),assetId);
                        return; // Exit the function and return to main menu

                    }
                    else if (okay == "2")
                    {
                        Console.WriteLine("Restarting attribute entry...");
                        break; // Break the inner loop and restart data entry
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter 1 or 2.");
                    }
                }
            }
        }

    }

    static async Task csvHostUpdate()
    {
        Console.Write("Input the csv file path: ");
        string csvPath = Console.ReadLine();
        try
        {
            using (TextFieldParser parser = new TextFieldParser(csvPath))
            {
                parser.SetDelimiters(new string[] { "," });
                parser.HasFieldsEnclosedInQuotes = true; // Ensures values with commas are handled correctly

                int lineNumber = 0;
                while (!parser.EndOfData)
                {
                    string[] columns = parser.ReadFields();
                    lineNumber++;

                    if (lineNumber == 1) continue; // Skip header row

                    if (columns.Length < 6)
                    {
                        Console.WriteLine($"Skipping line {lineNumber}: Not enough columns ({columns.Length} found).");
                        continue;
                    }

                    string hostname = columns[0];
                    string App = columns[1];
                    string Owner = columns[2];
                    string Resp = columns[3];
                    string Vertical = columns[4];
                    string Notes = columns[5];

                    //Console.WriteLine($"DEBUG: Parsed - Hostname: {hostname}, App: {App}, Owner: {Owner}, Resp: {Resp}, Vertical: {Vertical}, Notes: {Notes}");

                    string assetId = await GetAssetIdByHostname(hostname);
                    if (string.IsNullOrEmpty(assetId))
                    {
                        Console.WriteLine($"Error: No Asset ID found for {hostname}. Skipping...");
                        continue;
                    }
                    var attributes = new[]
                    {
                        new { attributeName = "Application", value = App },
                        new { attributeName = "Owner", value = Owner },
                        new { attributeName = "Responsable", value = Resp },
                        new { attributeName = "Vertical", value = Vertical },
                        new { attributeName = "Notes", value = Notes }
                    };

                    await UpdateCustomAttributes(attributes, assetId);
                }
            }
            Console.WriteLine("DEBUG: Completed CSV processing.");
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"ERROR: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }

    static async Task<string> GetAssetIdByHostname(string hostname)
    {
        string assetId=string.Empty;
        Console.WriteLine("Function: GetAssetIdByHostname");
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("session", apiKey);

        // Ensure the hostname ends with ".*"
        string formattedHost = hostname.Trim() + ".*";

        // Updated GraphQL Query for Tanium
        var graphqlQuery = new
        {
            query = @"
            query getTDSEndpointIDs($host: String) {
                endpoints (source: {tds: {excludeErrors: false}}, 
                filter: {op: MATCHES, path: ""name"", value: $host}) {
                    edges {
                        node {
                            id
                            name
                            ipAddress
                        }
                    }
                }
            }",
            variables = new
            {
                host = formattedHost // Ensure the ".*" wildcard is included
            }
        };

        string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(graphqlQuery);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        try
        {
            // Send Request
            HttpResponseMessage response = await client.PostAsync(taniumApiUrl, content);
            string responseString = await response.Content.ReadAsStringAsync();
            
            
            // Log full response for debugging
            Console.WriteLine($"Response Status: {response.StatusCode}");
            Console.WriteLine($"Response Body: {responseString}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: API call failed with status {response.StatusCode}");
                return assetId;
            }

            // Parse response JSON
            JObject jsonResponse = JObject.Parse(responseString);
            assetId = jsonResponse["data"]?["endpoints"]?["edges"]?.First?["node"]?["id"]?.ToString();
            Console.WriteLine($"Parsed Response -> HostName: {hostname}, TaniumId: {assetId}");
            if (string.IsNullOrEmpty(assetId))
            {
                Console.WriteLine($"Warning: No asset ID found for {hostname}");
            }


            return assetId;
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"HTTP Request Error: {httpEx.Message}");
            return assetId;
        }
        catch (JsonReaderException jsonEx)
        {
            Console.WriteLine($"JSON Parsing Error: {jsonEx.Message}");
            return assetId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected Error: {ex.Message}");
            return assetId;
        }
    }
    

    static async Task<string> UpdateCustomAttributes(object[] attributes, string eid)
    {
        Console.WriteLine("Function: UpdateCustomAttributes");
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("session", apiKey);

        var graphqlQuery = new
        {
            query = @"
        mutation UpdateAsset($input: AssetUpdateEndpointsUsingEidInput!) {
            assetUpdateEndpointsUsingEid(input: $input) {
                assets {
                    eid
                    source {
                        namespace
                        id
                        name
                    }
                    status
                    error
                }
            }
        }",
            variables = new
            {
                input = new
                {
                    sourceName = "Tanium",  // Replace with the actual source name
                    entityNames = new[] { "Asset Custom: Test - Server Attributes" }, // Replace with the actual Entity Name
                    assets = new[]
                    {
                        new
                        {
                            eid,  // Use the endpoint ID (EID)
                            entities = new[]
                            {
                                new
                                {
                                    entityName = "Asset Custom: Test - Server Attributes", // Replace with the actual Entity Name
                                    entityRowItems = new[]
                                    {
                                        new { attributes }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(graphqlQuery);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Send Request
        HttpResponseMessage response = await client.PostAsync(taniumApiUrl, content);
        string responseString = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Response:");
        Console.WriteLine(responseString);
        return responseString;
    }
}
