open System
open System.IO

type dirInfo = { name: string; count: int }

let rec visitor dir filter= 
    seq { yield! Directory.GetFiles(dir, filter)
          for subdir in Directory.GetDirectories(dir) do yield! visitor subdir filter}
          
let rec allFiles dirs =
    if Seq.isEmpty dirs then 
        Seq.empty 
    else
        seq { yield! dirs |> Seq.collect Directory.EnumerateFiles
              yield! dirs |> Seq.collect Directory.EnumerateDirectories |> allFiles } 

let clearEmptyFolderTree folder = 
    seq [ folder ] |> allFiles |> Seq.iter (fun x -> printfn "%s" x)
    

//getAllFiles @"E:\iTunes\iTunes Lib\iTunes Media\Music_b"  |> Seq.iter (printfn "%s")

clearEmptyFolderTree @"E:\iTunes\iTunes Lib\iTunes Media\Music_b"