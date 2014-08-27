// include Fake lib
#r @"packages\FAKE\tools\FakeLib.dll"
open Fake
open System
open Fake.AssemblyInfoFile

//RestorePackages()

type Project = { name: string;  authors: List<string>; description: string; summary: string; tags: string}
let authors = ["Craig Smitham"]


// The project name should be the same as the project directory
let socialSmith = { 
    name = "SocialSmith"; 
    authors = authors; 
    summary = "";
    description ="Toolkit for adding social features to your ASP.NET app.";
    tags = "" }

let socialSmithSqlServer = { 
    name = "SocialSmith.SqlServer"; 
    authors = authors; 
    summary = "SQL Server/Entity Framework";
    description = "SQL Server/Entity Framework persistence for SocialSmith";
    tags = "" }

let projects = [ socialSmith; socialSmithSqlServer ]

let buildMode = getBuildParamOrDefault "buildMode" "Release"
let testResultsDir = "./testresults"
let packagesDir = "./packages/"
let packagingRoot = "./packaging/"
let projectBins =  projects |> List.map(fun p -> "./src/" @@ p.name @@ "/bin")
let projectPackagingDirs =  projects |> List.map(fun p -> packagingRoot @@ p.name)

let buildNumber = getBuildParamOrDefault "buildNumber" (environVarOrDefault "APPVEYOR_BUILD_NUMBER" "0")
// APPVEYOR_BUILD_VERSION:  MAJOR.MINOR.PATCH.BUILD_NUMBER
let buildVersion = getBuildParamOrDefault "buildVersion" (environVarOrDefault "APPVEYOR_BUILD_VERSION" "0.0.0.0")
let majorMinorPatch = split '.' buildVersion  |> Seq.take(3) |> Seq.toArray |> (fun versions -> String.Join(".", versions))
let assemblyVersion = majorMinorPatch
let assemblyFileVersion = buildVersion
let preReleaseVersion = getBuildParamOrDefault "prerelease" ("-ci" + buildNumber)
let isMajorRelease = getBuildParam "release" <> ""
let packageVersion = 
        match isMajorRelease with
        | true -> majorMinorPatch
        | false -> majorMinorPatch + preReleaseVersion
    

// Targets
Target "Clean" (fun _ -> 
   List.concat [projectBins; projectPackagingDirs; [testResultsDir; packagingRoot]] |> CleanDirs
)

Target "AssemblyInfo" (fun _ ->
    CreateCSharpAssemblyInfo "./SolutionInfo.cs"
      [ Attribute.Product "SocialSmith" 
        Attribute.Version assemblyVersion
        Attribute.FileVersion assemblyFileVersion
        Attribute.ComVisible false ]
)

Target "BuildApp" (fun _ ->
   MSBuild null "Build" ["Configuration", buildMode] ["./SocialSmith.sln"] |> Log "AppBuild-Output: "
)


let useDefaults = None
let withCustomParams (configuration: NuGetParams -> NuGetParams) = 
    Some(configuration)

let withEntityFramework = withCustomParams(fun p -> 
            {p with 
                Dependencies =
                    ["EntityFramework", GetPackageVersion packagesDir "EntityFramework"] })

let createNuGetPackage (project:Project) (customParams: (NuGetParams -> NuGetParams) option) = 
    let packagingDir = (packagingRoot @@ project.name @@ "/");
    let net45Dir =  (packagingDir @@ "lib/net45")
    let buildDir = ("./src/" @@ project.name @@ "/bin")
    let publishUrl = getBuildParamOrDefault "publishurl" (environVarOrDefault "publishurl" "")
    let apiKey = getBuildParamOrDefault "apikey" (environVarOrDefault "apikey" "")

    CleanDir net45Dir
    CopyFile net45Dir (buildDir @@ "Release/" @@ project.name + ".dll")
    CopyFiles packagingDir ["LICENSE.txt"; "README.md"]

    NuGet((fun p -> 
        {p with 

            Project = project.name
            Authors = project.authors 
            Description = project.description 
            OutputPath = packagingRoot 
            Summary = project.summary 
            WorkingDir = packagingDir
            Version = packageVersion
            Tags = project.tags
            PublishUrl = publishUrl
            AccessKey = apiKey 
            Publish = publishUrl <> "" } 
            |>  match customParams with
                | Some(customParams) -> customParams
                | None -> (fun p -> p))) "./base.nuspec"


Target "CreateCorePackage" (fun _ -> 
    createNuGetPackage socialSmith useDefaults
)

Target "CreateSqlServerPackage" (fun _ -> 
    createNuGetPackage socialSmithSqlServer withEntityFramework    
)


Target "ContinuousIntegration" DoNothing
Target "CreatePackages" DoNothing
Target "Default" DoNothing

"Clean"
    ==> "AssemblyInfo"
        ==> "BuildApp"

"BuildApp" 
    ==>"CreateSqlServerPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateCorePackage"
        ==> "CreatePackages"


"BuildApp" 
    ==>"CreatePackages"
        ==> "ContinuousIntegration" 


// start build
RunTargetOrDefault (environVarOrDefault "target" "Default")