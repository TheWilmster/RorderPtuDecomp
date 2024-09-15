﻿using System.Text.Json;
using System.Text.Json.Nodes;
using UndertaleModLib;

//dotnet publish -r win-x64 -c release -p:PublishSingleFile=true

var exePath = Environment.ProcessPath;
var exeFolder = Path.GetDirectoryName(exePath)!;
Console.WriteLine(exePath);
string fileWithExt(string ext, string directory) => Directory.GetFiles(directory).First(x => Path.GetExtension(x) == ext);

string yypPath, origPath;
try
{
    yypPath = fileWithExt(".yyp", exeFolder);
    origPath = fileWithExt(".win", exeFolder);
}
catch (InvalidOperationException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Missing .yyp or .win file in the same folder as the exe\n" +
        "Please drag the .yyp file and the .win file to the exe folder then try again\n");
    Console.ReadLine();
    return 1; //ERROR
}


Console.WriteLine("Reading yyc file...");
//YYP FILE
using Stream yypFile = File.OpenRead(yypPath);
JsonObject yycJson = JsonSerializer.Deserialize<JsonObject>(yypFile, 
    new JsonSerializerOptions{AllowTrailingCommas=true})!;
JsonArray resources = (yycJson["resources"] as JsonArray)!;

Console.WriteLine("Reading undertale data file... (takes a-while)");
//UNDERTALE-DATA FILE
UndertaleData LoadData(string path) => UndertaleIO.Read(File.OpenRead(path), null);
UndertaleData data = LoadData(origPath);
Console.WriteLine("Done reading undertale data file!\n");

int GetResourceIndex(string resourceName)
{
    for (int i = 0; i < resources.Count; i++)
    {
        JsonObject? resource = (resources[i] as JsonObject);
        if (resource == null) continue;
        if (resource["id"]?["name"]?.GetValue<string>().Equals(resourceName, StringComparison.CurrentCultureIgnoreCase) ?? false)
            return i;
    }
    return -1;
}

bool failed = false;
void Order<T>(IList<T> namedResources) where T : UndertaleNamedResource
{
    for (int i = namedResources.Count - 1; i >= 0; i--)
    {
        var resouce = namedResources[i];
        string resourceName = resouce.Name.Content;
        int yycId = GetResourceIndex(resourceName);
        if (yycId == -1)
        {
            Console.WriteLine("Resource not found: " + resourceName);
            failed = true;
        }
        else
        {
            //Insert to start
            var toAdd = resources[yycId];
            resources.RemoveAt(yycId);
            resources.Insert(0, toAdd);
        }
    }
}

Console.WriteLine("Ordering gameobjects...");
Order(data.GameObjects);
Console.WriteLine("Ordering sprites...");
Order(data.Sprites);
Console.WriteLine("Ordering sounds...");
Order(data.sounds);

if (failed)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Resources not found.\n" +
    "It means the object exists in the original but not the modded version (YYC file)\n" +
    "Please include it in the modded (decomp) then try again");
    Console.ReadLine();
    return 1; //ERROR
}

Console.ForegroundColor = ConsoleColor.Green;
string outputFileName = "PizzaTower_GM2_output.yyp";
var outputJson = Path.Combine(exeFolder, outputFileName);
File.WriteAllText(outputJson, yycJson.ToJsonString());
Console.WriteLine($"Succesfully Done! copy {outputFileName} to your original .yyp file");
Console.ReadLine();
return 0; //Succesful

//HashSet<string> types = new();
//for (int i = 0; i < resources.Count; i++)
//{
//    JsonObject resource = (JsonObject)resources[i]!["id"]!;
//    string name = resource["name"]!.GetValue<string>();
//    string path = resource["path"]!.GetValue<string>();
//    var splits = path.Split("/");
//    string type = splits[0];
//    if (type == "sprites")
//    {
//        Console.WriteLine(name);
//    }
//}

//The position in the list is the ID of the item in-game
//data.Sprites[ID]



enum GamemakerTypes
{
    objects, sprites, scripts, rooms, extensions, shaders, fonts, tilesets
}