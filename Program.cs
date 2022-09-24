// get console args
using Elements;
using Elements.Geometry;

Console.WriteLine("Do you want to split a model or a catalog? (type \"Model\" or \"Catalog\")");
var input = Console.ReadLine() ?? "";
var isModel = input.ToLower() == "model";
var isCatalog = input.ToLower() == "catalog";
if (!isModel && !isCatalog)
{
    Console.WriteLine($"I don't know what {input} means.");
    return;
}

void ProcessCatalog()
{
    Console.WriteLine("Please provide a path to a catalog.json file");

    var catalogJsonPath = Console.ReadLine();

    if (!File.Exists(catalogJsonPath))
    {
        Console.WriteLine($"File {catalogJsonPath} does not exist.");
        return;
    }

    // prompt for S3 Bucket path
    Console.WriteLine("Please provide an S3 Bucket path, or hit enter to leave the path as is.");
    var s3BucketPath = Console.ReadLine();

    var model = Model.FromJson(File.ReadAllText(catalogJsonPath));
    var contentElements = model.AllElementsOfType<ContentElement>();
    if (s3BucketPath != null && s3BucketPath.Length > 0)
    {
        if (!s3BucketPath.EndsWith("/"))
        {
            s3BucketPath += "/";
        }
        Console.WriteLine("Updating content element GLTF Paths...");
        foreach (var contentElement in contentElements)
        {
            var currentPath = contentElement.GltfLocation;
            contentElement.GltfLocation = s3BucketPath + currentPath;
        }

    }

    Console.WriteLine($"About to split up your model ✂️. \nThis model contains {contentElements.Count()} unique content elements. \nHow many unique content elements should there be per model? 250 seems to work well.");
    if (!int.TryParse(Console.ReadLine(), out var elementsPerModel))
    {
        elementsPerModel = 250;
    }

    for (int i = 0; i < contentElements.Count(); i += elementsPerModel)
    {
        var modelNumber = i / elementsPerModel + 1;
        Console.WriteLine($"Creating model {modelNumber} of {Math.Ceiling((double)contentElements.Count() / elementsPerModel)}");
        var modelToSave = new Model();
        var elementsToCopy = contentElements.Skip(i).Take(elementsPerModel);
        var elementInstances = model.AllElementsOfType<ElementInstance>().Where(e => elementsToCopy.Contains(e.BaseDefinition));
        modelToSave.AddElements(elementInstances);
        var outputPath = Path.Combine(Path.GetDirectoryName(catalogJsonPath) ?? "./", $"catalog-{modelNumber:00}.json");
        var json = modelToSave.ToJson(false);
        File.WriteAllText(outputPath, json);
    }
}

void ProcessModel()
{
    Console.WriteLine("Please provide a path to a model.json file");

    var modelJsonPath = Console.ReadLine();

    if (!File.Exists(modelJsonPath))
    {
        Console.WriteLine($"File {modelJsonPath} does not exist.");
        return;
    }

    var model = Model.FromJson(File.ReadAllText(modelJsonPath));
    var basicElements = model.Elements.Values.Where(e => e is not Profile and not Material).ToList();

    Console.WriteLine($"About to split up your model ✂️. \nThis model contains {basicElements.Count} elements. \nHow many elements should there be per model?");
    if (!int.TryParse(Console.ReadLine(), out var elementsPerModel))
    {
        elementsPerModel = 250;
    }

    for (int i = 0; i < basicElements.Count; i += elementsPerModel)
    {
        var modelNumber = i / elementsPerModel + 1;
        Console.WriteLine($"Creating model {modelNumber} of {Math.Ceiling((double)basicElements.Count / elementsPerModel)}");
        var modelToSave = new Model();
        var elementsToCopy = basicElements.Skip(i).Take(elementsPerModel);
        modelToSave.AddElements(elementsToCopy);
        var outputPath = Path.Combine(Path.GetDirectoryName(modelJsonPath) ?? "./", $"model-{modelNumber:00}.json");
        var json = modelToSave.ToJson(false);
        File.WriteAllText(outputPath, json);
    }
}

if (isModel)
{
    ProcessModel();
}
else if (isCatalog)
{
    ProcessCatalog();
}

Console.WriteLine("Success! 🎉");
