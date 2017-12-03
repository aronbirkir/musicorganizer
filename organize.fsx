
#r "packages/taglib/lib/taglib-sharp.dll"

open System
open System.IO
open System.Text.RegularExpressions
open TagLib

let workFolder = @"e:\wip"
let masterFolder = @"e:\music"
let mikFolder = Path.Combine(workFolder,"_to_mik")
let tagFolder = Path.Combine(workFolder,"_to_tag")
let convertFolder = Path.Combine(workFolder,"_to_conv");
let inputFolders = [|Path.Combine(workFolder,"new")|]
let extensions = [|".mp3"; ".m4a"|]

let keyPattern = @"^[0-1]?[0-9][AB]"

(*

The goal is to end up with filenames like this:
/<Collection Path>/<Genre>/<Key> <Bpm> <Title> (<Version>) - <Artist>.<ext> 
For example:
/music/Techno/09A 126.0 Habits (Original Mix) - SQL.flac
*)

let filterExtension (file:string) = 
    Seq.exists (fun f -> file.EndsWith(f)) extensions

let parseTag (file:string) =
    TagLib.File.Create(file)

let copy source dest =
    File.Copy(source, dest)

let (|CompiledMatch|_|) pattern input =
    if isNull input then None
    else
        let m = Regex.Match(input, pattern, RegexOptions.Compiled)
        if m.Success then Some [for x in m.Groups -> x]
        else None

let parseKey value =
    match value with
        | CompiledMatch keyPattern result -> Some(result.Head.Value)
        | _ -> None

let decideDestination (file:TagLib.File) =
    // If no title tag or 
    let requiredTags = [| file.Tag.Title; file.Tag.FirstPerformer;file.Tag.FirstGenre|]
    let key = parseKey file.Tag.Comment
    let fileExtension = Path.GetExtension(file.Name)
    if fileExtension <> ".mp3" then
        let convDest = Path.Combine(convertFolder, Path.GetFileName(file.Name))
        file, convDest
    elif Seq.exists (isNull) requiredTags || file.Tag.BeatsPerMinute <= 0u then
        let tagDest = Path.Combine(tagFolder, Path.GetFileName(file.Name))        
        file, tagDest
    // 
    elif key = None then
        let mikDest = Path.Combine(mikFolder, Path.GetFileName(file.Name))        
        file, mikDest
    else        
        let bpm = file.Tag.BeatsPerMinute
        let newFileName = sprintf "%s %u %s - %s%s" (key.Value) (bpm) (file.Tag.Title) (file.Tag.FirstPerformer) fileExtension
        //printfn "%s" newFileName
        let genre = if not (System.String.IsNullOrWhiteSpace( file.Tag.FirstGenre )) then file.Tag.FirstGenre else "Other"         
        let genreFolder = Path.Combine(masterFolder, genre)
        //printfn "%s" genreFolder
        //if( not (Directory.Exists genreFolder)) then
        //    Directory.CreateDirectory genreFolder |> ignore
        let destFile = Path.Combine( genreFolder, newFileName)       
        file, destFile 

let rec visitor dir filter= 
    seq { yield! Directory.GetFiles(dir, filter)
          for subdir in Directory.GetDirectories(dir) do yield! visitor subdir filter} 


   
let processFolder folder count = 
    Directory.GetFiles(folder, "*.*" , SearchOption.AllDirectories)
    |> Seq.truncate count
    |> Seq.filter filterExtension
    |> Seq.map (parseTag >> decideDestination)  
    

let handleFileDestination (file:File) dest dryRun =
    printfn "%s -> %s ((%s) (%s))" file.Name dest (Path.GetDirectoryName(dest)) (Path.GetExtension(dest))
    if not dryRun then
        if not ( Directory.Exists(Path.GetDirectoryName(dest))) then
            Directory.CreateDirectory(Path.GetDirectoryName(dest)) |> ignore
        if not (File.Exists(dest)) then
            File.Move(file.Name, dest)   
        else
            printfn "Destination already exists" 

inputFolders
|> Seq.map (fun f -> processFolder f 100)
|> Seq.iter (fun f -> f |> Seq.iter (fun (file,dest) -> handleFileDestination file dest true ))

