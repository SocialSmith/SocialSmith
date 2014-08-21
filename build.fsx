// include Fake lib
#r @"packages\FAKE\tools\FakeLib.dll"
open Fake
open System
open Fake.AssemblyInfoFile

RestorePackages()

type Project = { name: string;  authors: List<string>; description: string; summary: string; tags: string}
let authors = ["Craig Smitham"]


// The project name should be the same as the project directory
let activityStreams = { 
    name = "SocialSmith.ActivityStreams"; 
    authors = authors; 
    summary = "";
    description = "Activity Streams";
    tags = "" }

let activityStreamsEF = { 
    name = "SocialSmith.ActivityStreams.EntityFramework"; 
    authors = authors; 
    summary = "";
    description = "Entity Framework store for Activity Streams";
    tags = "" }

let blogs = {
    name = "SocialSmith.Blogs";
    authors = authors;
    summary = "";
    description = "SocialSmith Blogs";
    tags = "" }

let blogsEF ={
    name = "SocialSmith.Blogs.EntityFramework";
    authors = authors;
    summary = "";
    description = "Entity Framework for SocialSmith Blogs";
    tags = "" }

let comments = {
    name = "SocialSmith.Comments";
    authors = authors;
    summary = "";
    description = "SocialSmith Comments";
    tags = "" 
}

let commentsEF = {
    name = "SocialSmith.Comments.EntityFramework";
    authors = authors;
    summary = "";
    description = "Entity Framework extension for SocialSmith Comments";
    tags = "" 
}

let groups = {
    name = "SocialSmith.Groups";
    authors = authors;
    summary = "";
    description = "SocialSmith Groups";
    tags = "" 
}

let groupsEF = {
    name = "SocialSmith.Groups.EntityFramework";
    authors = authors;
    summary = "";
    description = "Entity Framework extension for SocialSmith Groups";
    tags = "" 
}

let profiles = {
    name = "SocialSmith.Profiles";
    authors = authors;
    summary = "";
    description = "SocialSmith Profiles";
    tags = "" 
}


let profilesEF = {
    name = "SocialSmith.Profiles.EntityFramework";
    authors = authors;
    summary = "";
    description = "Entity Framework extension for SocialSmith Profiles";
    tags = "" 
}


let projects = [ 
    activityStreams; 
    activityStreamsEF; 
    blogs; 
    blogsEF; 
    comments; 
    commentsEF; 
    groups; 
    groupsEF; 
    profiles; 
    profilesEF ]

let buildMode = getBuildParamOrDefault "buildMode" "Release"
let testResultsDir = "./testresults"
let packagesDir = "./packages/"
let packagingRoot = "./packaging/"
let projectBins =  projects |> List.map(fun p -> "./src/" @@ p.name @@ "/bin")
let projectPackagingDirs =  projects |> List.map(fun p -> packagingRoot @@ p.name)

let buildNumber = environVarOrDefault "APPVEYOR_BUILD_NUMBER" "0"
// APPVEYOR_BUILD_VERSION:  MAJOR.MINOR.PATCH.BUILD_NUMBER
let buildVersion = environVarOrDefault "APPVEYOR_BUILD_VERSION" "0.0.0.0"
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


Target "CreateActivityStreamsPackage" (fun _ -> 
    createNuGetPackage activityStreams useDefaults
)

Target "CreateActivityStreamsEntityFrameworkPackage" (fun _ -> 
    createNuGetPackage activityStreamsEF withEntityFramework    
)

Target "CreateBlogsPackage" (fun _ -> 
    createNuGetPackage blogs useDefaults
)

Target "CreateBlogsEntityFrameworkPackage" (fun _ -> 
    createNuGetPackage blogsEF withEntityFramework
)

Target "CreateCommentsPackage" (fun _ -> 
    createNuGetPackage comments useDefaults
)

Target "CreateCommentsEntityFrameworkPackage" (fun _ -> 
    createNuGetPackage commentsEF withEntityFramework
)

Target "CreateGroupsPackage" (fun _ -> 
    createNuGetPackage groups useDefaults
)

Target "CreateGroupsEntityFrameworkPackage" (fun _ -> 
    createNuGetPackage groupsEF withEntityFramework
)

Target "CreateProfilesPackage" (fun _ -> 
    createNuGetPackage profiles useDefaults
)

Target "CreateProfilesEntityFrameworkPackage" (fun _ -> 
    createNuGetPackage profilesEF withEntityFramework
)


Target "ContinuousIntegration" DoNothing
Target "CreatePackages" DoNothing
Target "Default" DoNothing

"Clean"
    ==> "AssemblyInfo"
        ==> "BuildApp"

"BuildApp" 
    ==>"CreateActivityStreamsPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateActivityStreamsEntityFrameworkPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateBlogsPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateBlogsEntityFrameworkPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateCommentsPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateCommentsEntityFrameworkPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateGroupsPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateGroupsEntityFrameworkPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateProfilesPackage"
        ==> "CreatePackages"

"BuildApp" 
    ==>"CreateProfilesEntityFrameworkPackage"
        ==> "CreatePackages"


"BuildApp" 
    ==>"CreatePackages"
        ==> "ContinuousIntegration" 


// start build
RunTargetOrDefault (environVarOrDefault "target" "Default")